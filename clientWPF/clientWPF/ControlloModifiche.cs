using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace clientWPF
{

    static class ControlloModifiche
    {
        static public System.Timers.Timer checker;
        static string user, pwd;
        static string base_path;
        static private bool init = false;
        static private ConnectionSettings connectionSetting;
        static private int interval;
        static private readonly object syncLock = new object();
        /// <summary>
        /// Inizializza il controllo periodico del contenuto della cartella.
        /// Controlla anche le credenziali con il server testando il login.
        /// </summary>
        /// <exception cref="ServerException"></exception>
        /// <exception cref="ClientException"></exception>
        static public void Inizializza()
        {
            connectionSetting = new ConnectionSettings();
            user = connectionSetting.readSetting("account", "username");
            pwd = connectionSetting.readSetting("account", "password");
            base_path = connectionSetting.readSetting("account", "directory");
            interval = Int32.Parse(connectionSetting.readSetting("connection", "syncTime"));

            if (!base_path.EndsWith("\\"))
            {
                base_path += "\\";
            }

            Properties.Settings.Default.user = user;
            Properties.Settings.Default.pwd = pwd;
            Properties.Settings.Default.base_path = base_path;
            Properties.Settings.Default.intervallo = interval;
            Properties.Settings.Default.Save();

            if (!init)
            {
                if (user == null || pwd == null)
                    throw new ClientException("Mancano le credenziali dell'utente. Fare il login.", ClientErrorCode.CredenzialiUtenteMancanti);
                if (base_path == null)
                    throw new ClientException("Il percorso di controllo non è specificato", ClientErrorCode.PercorsoNonSpecificato);
                //Controllo se nome utente e password sono giusti
                if (!Command.Logged)
                {
                    try
                    {
                        ComandoLogin login = new ComandoLogin(user, pwd);
                        login.esegui();
                        ComandoEsci esci = new ComandoEsci();
                        esci.esegui();
                    }
                    catch (ServerException e) //Se il server non è disponibile l'eccezione non viene catturata...
                    {
                        switch (e.ErrorCode)
                        {
                            case ServerErrorCode.DatiErrati:
                                throw new ClientException("Le credenziali dell'utente sono errate. Rifare il login o registrarsi.", ClientErrorCode.CredenzialiUtenteErrate);
                            default:
                                throw;
                        }
                    }
                }
				//Controlla se il DB è ok
                
                //if (!DB_Table.DBEsiste)
                    //throw new ClientException("Il database non esiste", ClientErrorCode.DatabaseNonPresente);
                /*
                string sha_db = DB_Table.HashDB;
                ComandoScaricaHashDB c = new ComandoScaricaHashDB();
                c.esegui();
                if(sha_db != c.hash)
                {
                    throw new ClientException("Il database è corrotto o non esiste");
                }
                */
                //Imposta il timer per il controllo periodico
                checker = new System.Timers.Timer(interval * 1000);
                checker.AutoReset = true;
                checker.Elapsed += Checker_Elapsed;
                init = true;
                if (!Directory.Exists(base_path))
                    throw new ClientException(Properties.Messaggi.CartellaNonEsistente, ClientErrorCode.CartellaNonEsistente);
            }
            checker.Enabled = true;
        }

        /// <summary>
        /// In ordine questa funzione:
        ///  - Cancella il contenuto della cartella base_path
        ///  - Cancella il database e lo ricrea con le tabelle vuote
        ///  - Scarica la lista dei path relativi
        ///  - Per ogni path, ricrea le cartelle e scarica i file con le loro versioni
        /// </summary>
        public static void RestoreAsLastStatusOnServer()
        {
            bool tmp = checker.Enabled;
            checker.Enabled = false;
            Log l = Log.getLog();

			lock (syncLock)
            {
                //Puliamo la cartella...
                if(Directory.Exists(base_path))
                    Directory.Delete(base_path, recursive: true);
                try
				{
                    DB_Table.RebuildDB();
                    Directory.CreateDirectory(base_path);

                    ComandoListFolders c = new ComandoListFolders();
					c.esegui();
                    FileUtenteList flist = FileUtenteList.getInstance();
					foreach (string path_rel in c.Paths)
					{
                        string[] directories = path_rel.Split('\\');
                        string tmp_path = base_path;
                        foreach(string dir in directories)
                        {
                            tmp_path += dir + "\\";
                            if(!Directory.Exists(tmp_path))
                            {
                                Directory.CreateDirectory(tmp_path);
                            }
                        }
						ComandoListDir c2 = new ComandoListDir(path_rel);
						c2.esegui();
						foreach (string nome_file in c2.FileNames)
						{
							ComandoListVersions c_vers = new ComandoListVersions(nome_file, path_rel);
							c_vers.esegui();
							DateTime[] versions = c_vers.Versions;
							DateTime last_vers = versions.Max();
							ComandoScaricaFile c_scarica = new ComandoScaricaFile(nome_file, path_rel, last_vers);
							c_scarica.esegui();
							FileUtente fu = flist.CreaNuovo(nome_file,path_rel, last_vers, last_vers, c_scarica.Dim, c_scarica.SHAContenuto);
							foreach (DateTime dt in versions)
							{
								if(dt != last_vers)
									fu.AggiungiVersione(dt);
							}
						}
					}
				}
				catch (ServerException e)
				{
					l.log(e.Message);
					throw;
				}
				checker.Enabled = tmp;
			}
        }
		
        private static void Checker_Elapsed(object sender, ElapsedEventArgs e)
        {
            Check();
        }

        public static void Check()
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");

            if (!Directory.Exists(base_path))
                return;
            if(Monitor.TryEnter(syncLock))
            {
                List<string[]> files = FileUtenteList.exploreFileSystem(base_path);
                List<FileUtente> toBeDeleted = new List<FileUtente>();
                FileUtenteList list = FileUtenteList.getInstance();
                string[] entry = new string[2];
                string path_completo;
                Command c;

                CultureInfo it = new CultureInfo("it-IT");
                Thread.CurrentThread.CurrentCulture = it;

                if (!init)
                {
                    throw new ClientException("La classe per il controllo delle modifiche non è inizializzata correttamente.", ClientErrorCode.ControlloNonInizializzato);
                }
                if (!Command.Logged)
                {
                    ComandoLogin login = new ComandoLogin(user, pwd);
                    login.esegui();
                }
                FileInfo finfo;
                foreach (FileUtente fu in list)
                {
                    //Check if still exists, and if its modified
                    entry[0] = fu.Nome;
                    entry[1] = fu.Path;
                    int index;
                    if ((index = files.FindIndex(fTest => (fTest[0] == entry[0] && fTest[1] == entry[1]))) >= 0)
                    {
                        files.RemoveAt(index);
                        //Il file selezionato esiste ancora...
                        path_completo = base_path + entry[1] + Path.DirectorySeparatorChar + entry[0];
                        finfo = new FileInfo(path_completo);

                        DateTime dt = TimeZoneInfo.ConvertTime(finfo.LastWriteTime, TimeZoneInfo.Local, tz);
                        DateTime tMod = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        tMod = tMod.AddTicks(-(tMod.Ticks % TimeSpan.TicksPerSecond));

                        if (DateTime.Compare(tMod, fu.TempoModifica) != 0)
                        {
                            FileStream fs = File.Open(path_completo, FileMode.Open);
                            string new_sha = FileUtente.CalcolaSHA256(fs);
                            if (new_sha != fu.SHA256Contenuto)
                            {
                                fu.aggiornaDati((int)finfo.Length, finfo.LastWriteTime);
                                c = new ComandoAggiornaContenutoFile(entry[0], entry[1], (int)finfo.Length,
                                    finfo.LastWriteTime, fu.SHA256Contenuto);
                                c.esegui();
                            }
                        }
                    }
                    else
                    {
                        toBeDeleted.Add(fu);
                    }
                }
                //Cancelliamo
                foreach(FileUtente _fu in toBeDeleted)
                {
                    list.Delete(_fu.Id);
                    c = new ComandoEliminaFile(_fu.Nome, _fu.Path);
                    c.esegui();
                }

                //file nuovi
                FileUtente fu2;
                foreach (string[] n_file in files)
                {
                    string file_path_completo = base_path + n_file[1] + Path.DirectorySeparatorChar + n_file[0];
                    finfo = new FileInfo(file_path_completo);
                    DateTime dt = TimeZoneInfo.ConvertTime(finfo.LastWriteTime, TimeZoneInfo.Local, tz);
                    DateTime tMod = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    tMod = tMod.AddTicks(-(tMod.Ticks % TimeSpan.TicksPerSecond));
                    dt = TimeZoneInfo.ConvertTime(finfo.CreationTime, TimeZoneInfo.Local, tz);
                    DateTime tCre = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    tCre = tCre.AddTicks(-(tCre.Ticks % TimeSpan.TicksPerSecond));

                    FileStream fs = File.Open(file_path_completo, FileMode.Open);
                    string new_sha = FileUtente.CalcolaSHA256(fs);
                    fu2 = list.CreaNuovo(n_file[0], n_file[1], tCre, tMod, (int)finfo.Length, new_sha);
                    c = new ComandoNuovoFile(n_file[0], n_file[1], (int)finfo.Length, tCre, tMod, new_sha);
                    c.esegui();
                }
            }
        }

        public static void StopTimer()
        {
            if (checker != null)
            {
                checker.Enabled = false;
            }
        }
    }
}
