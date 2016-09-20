using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace clientWPF
{
    static class ControlloModifiche
    {
        static public Timer checker;
        static string user, pwd;
        static string base_path;
        static private bool init = false;
        static private ConnectionSettings connectionSetting;
        static private readonly object syncLock = new object();
        static private int interval;
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

            Properties.Settings.Default.user = user;
            Properties.Settings.Default.pwd = pwd;
            Properties.Settings.Default.base_path = base_path;
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
                //Imposta il timer per il controllo periodico
                checker = new Timer(interval * 1000);
                checker.AutoReset = true;
                checker.Elapsed += Checker_Elapsed;
                init = true;
            }
            checker.Enabled = true;
        }

        private static void Checker_Elapsed(object sender, ElapsedEventArgs e)
        {
            Check();
        }

        public static void Check()
        {
            if (!Directory.Exists(base_path))
                return;

            lock (syncLock)
            {
                List<string[]> files = FileUtenteList.exploreFileSystem(base_path);
                FileUtenteList list = new FileUtenteList();
                string[] entry = new string[2];
                string path_completo;
                Command c;

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
                        if (finfo.LastWriteTime != fu.TempoModifica)
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
                            else
                            {
                                //Aggiornare il timestamp di modifica
                                fu.TempoModifica = finfo.LastWriteTime;
                            }
                        }
                    }
                    else
                    {
                        list.Delete(fu.Id);
                        c = new ComandoEliminaFile(entry[0], entry[1]);
                        c.esegui();
                    }
                }
                //file nuovi
                FileUtente fu2;
                foreach (string[] n_file in files)
                {
                    string file_path_completo = base_path + n_file[1] + Path.DirectorySeparatorChar + n_file[0];
                    finfo = new FileInfo(file_path_completo);
                    fu2 = FileUtente.CreaNuovo(n_file[0], n_file[1], finfo.CreationTime, (int)finfo.Length);
                    c = new ComandoNuovoFile(n_file[0], n_file[1]);
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
