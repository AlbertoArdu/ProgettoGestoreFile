using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
//TODO -> Aggiungere supporto per la condivisione dei file (nel db una tabella relazione tra user e snapshot, magari con un flag per i privilegi)
//                          E definire una politica in caso di modifica da parte di due utenti (Suggerito COPY-ON-MODIFY per semplicita)

namespace ProgettoMalnati
{
    
    class Snapshot : DB_Table
    {
        //Attributi
        private string __nome_utente;
        private string __sha_contenuto;
        private string __path_locale;
        private string __nome_locale;
        private DateTime __t_modifica;
        private int __id;
        private int __dim;
        private FileStream __lettura_contenuto = null;
        private FileStream __scrittura_contenuto = null;
        private Log l;
        private int posizione_lettura = 0;
        private int posizione_scrittura = 0;
        private bool cambioContenutoIniziato = false;

        //Da inizializzare -v-
        static private string base_path;
        private static string sql_get_snapshot_data = Properties.SQLquery.sqlGetSnapshotData;
        private static string sql_store_data = Properties.SQLquery.sqlStoreSnapshotData;
        private static string sql_insert_data = Properties.SQLquery.sqlInsertSnapshotData;

        //Proprieta
        public int Dim 
        {
            get { return __dim; }
        }
        public string NomeUtente
        {
            get { return __nome_utente; }
        }
        public int Id
        {
            get { return __id; }
        }
        public DateTime InsertTime
        {
            get { return __t_modifica; }
        }
        public string shaContenuto
        {
            get { return __sha_contenuto; }
        }

        //Costruttori
        public Snapshot(string nome_utente,int id)
            : base()
        {
            l = Log.getLog();
            if(base_path == null)
                base_path = Properties.ApplicationSettings.Default.base_path + Path.DirectorySeparatorChar + "users_files";
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@id", id.ToString() };
            this.ExecuteQuery(sql_get_snapshot_data,parameters);
            //Get the data
            foreach (Int32 i in GetResults())
            {
                this.__dim = (int)(this.ResultGetValue("dim"));
                //Da rivedere!!
                this.__t_modifica = (DateTime)(this.ResultGetValue("t_modifica"));
                this.__sha_contenuto = (string)(this.ResultGetValue("sha_contenuto"));
                this.__nome_locale = (string)(this.ResultGetValue("nome_locale_s"));

            }
            this.__nome_utente = nome_utente;
            this.__id = id;
            this.__path_locale = base_path + Path.DirectorySeparatorChar + this.__nome_utente + Path.DirectorySeparatorChar;
        }

        //Distruttore
        ~Snapshot()
        {
            if(this.cambioContenutoIniziato)
            {
                try
                {
                    l.log("Il contenuto del file non è stato completamente scritto. Il sistema verrà riportato allo stato prececedente.");
                    this.__scrittura_contenuto.Close();
                    string tmp_nome_da_sostituire = this.__path_locale + Path.DirectorySeparatorChar + this.__nome_locale;
                    File.Delete(tmp_nome_da_sostituire);
                    File.Move(tmp_nome_da_sostituire+".tmp", tmp_nome_da_sostituire);
                }
                catch(Exception e)
                {
                    l.log(e.Message, Level.ERR);
                }
            }
            else
            {
                this.CaricaDatiNelDB();
            }
        }

        //Metodi
        public int leggiBytesDalContenuto(byte[] b, Int32 size)
        {
            if (this.cambioContenutoIniziato)
                throw new DatabaseException("Impossibile leggere il contenuto di uno snapshot mentre è in corso una modifica.",DatabaseErrorCode.SnapshotInScrittura);
            if (this.__lettura_contenuto == null)
            {
                try
                {
                    l.log("Path locale - " + this.__path_locale);
                    this.__lettura_contenuto = new FileStream(this.__path_locale + this.__nome_locale, FileMode.Open, FileAccess.Read);
                }
                catch (Exception e)
                {
                    l.log("Errore nel nome del file sul server o nel path specificato. " + e.ToString());
                    throw;
                }
            }
            int q=0;
            try
            {
                if (posizione_lettura + size > this.__dim)
                    size = this.__dim - posizione_lettura;
                q = this.__lettura_contenuto.Read(b, 0, size);
                posizione_lettura += size;
            }
            catch (NotSupportedException e)
            {
                l.log(e.ToString());
                throw;
            }
            catch (ObjectDisposedException e)
            {
                l.log(e.ToString());
                throw;
            }
            return q;
        }

        /// <summary>
        /// Inizia la modifica del contenuto di uno snapshot. Il vecchio contenuto viene mantenuto
        /// in un file temporaneo finché la scrittura di quello nuovo non è stabile.
        /// </summary>
        /// <param name="newDim">Dimensione del nuovo contenuto</param>
        /// <param name="timestamp">- Opzionale - Timestamp</param>
        /// <param name="sha_contenuto">SHA256 del contenuto. Se viene passato, viene fatto un confronto 
        /// con l'hash reale. Altrimenti viene memorizzato quello calcolato sul contenuto finale.</param>
        public void cambiaContenuto(int newDim, DateTime timestamp = new DateTime(), string sha_contenuto = "")
        {
            if (!this.cambioContenutoIniziato)
            {
                //Non è possibile mettere DateTime.Now come parametro di default
                //e questo è il giro attorno al problema
                if (timestamp == DateTime.MinValue)
                {
                    timestamp = DateTime.Now;
                }
                cambioContenutoIniziato = true;
                if(this.__lettura_contenuto != null)
                    this.__lettura_contenuto.Close();
                this.__lettura_contenuto = null;
                this.__t_modifica = timestamp;
                this.__dim = newDim;
                string tmp_nome = this.__path_locale + Path.DirectorySeparatorChar + this.__nome_locale + ".tmp";
                string tmp_nome_da_sostituire = this.__path_locale + Path.DirectorySeparatorChar + this.__nome_locale;
                
                File.Move(tmp_nome_da_sostituire, tmp_nome);
                this.__scrittura_contenuto = new FileStream(tmp_nome_da_sostituire, FileMode.Create, FileAccess.ReadWrite);
                this.posizione_scrittura = 0;
                this.__sha_contenuto = sha_contenuto;
            }
            else
            {
                throw new DatabaseException("Cambio contenuto già inizializzato. Non richiamare questa funzione finché tutto il contenuto precedente sia stato scritto.",DatabaseErrorCode.SnapshotInScrittura);
            }
        }

        public void scriviBytes(byte[] b, Int32 size)
        {
            //Controllo se è la prima scrittura: allora lancio eccezione
            if (!cambioContenutoIniziato)
            {
                throw new ApplicationException("Prima di scrivere il nuovo contenuto chiamare la funzione cambiaContenuto();");
            }

            if (posizione_scrittura + size > this.__dim)
                size = this.__dim - posizione_scrittura;
            if (size == 0)
            {
                return;
            }
            this.__scrittura_contenuto.Write(b, 0, size);
            posizione_scrittura += size;
            //Da testare con calma
            if(posizione_scrittura == this.__dim)
            {
                completaScrittura();
                CaricaDatiNelDB();
            }
        }

        /// <summary>
        /// Salva il nuovo contenuto, calcola l'hash del file memorizzato e lo confronta con quello fornito
        /// dall'utente (se fornito).
        /// </summary>
        private void completaScrittura() 
        {
            //Controllo se è la prima scrittura: allora lancio eccezione
            if (!cambioContenutoIniziato)
            {
                throw new DatabaseException("Non è stata chiamata la funzione cambiaContenuto();",DatabaseErrorCode.SnapshotInLettura);
            }
            SHA256 sha_obj = SHA256Managed.Create();
            byte[] hash_val;
            this.__scrittura_contenuto.Position = 0;
            hash_val = sha_obj.ComputeHash(this.__scrittura_contenuto);
            this.__scrittura_contenuto.Close();
            string tmp_nome = this.__path_locale + Path.DirectorySeparatorChar + this.__nome_locale + ".tmp";
            //Elimino il vecchio contenuto
            File.Delete(tmp_nome);
            //Controllo hash
            StringBuilder hex = new StringBuilder(hash_val.Length * 2);
            foreach (byte b in hash_val)
                hex.AppendFormat("{0:x2}", b);
            string sha_reale = hex.ToString();
            
            if (this.__sha_contenuto != "")
            {
                if(this.__sha_contenuto != sha_reale)
                {
                    throw new DatabaseException("L'hash fornito è diverso dall'hash calcolato.",DatabaseErrorCode.HashInconsistente);
                }
            }
            this.__sha_contenuto = sha_reale;
            this.__lettura_contenuto = new FileStream(this.__path_locale + this.__nome_locale, FileMode.Open, FileAccess.Read);
        }

        public void CaricaDatiNelDB()
        {
            string[][] parameters = new string[5][];
            parameters[0] = new string[2] { "@dim", __dim.ToString()};
            parameters[1] = new string[2] { "@t_modifica", __t_modifica.ToString("u") };
            parameters[2] = new string[2] { "@sha_contenuto", __sha_contenuto };
            parameters[3] = new string[2] { "@nome_locale_s", __nome_locale };
            parameters[4] = new string[2] { "@id", __id.ToString() };


            this.ExecuteQuery(sql_store_data, parameters);
        }

        //Funzioni Statiche

        //private static Snapshot s = null;

        public static Snapshot creaNuovo(string nome_utente, 
                                            string nome_file,
                                            string path_relativo,
                                            DateTime timestamp = new DateTime(),
                                            int dim = 0,
                                            string sha_contenuto = "")
        {
            //Controllo che gli argomenti non abbiano caratteri strani
            Regex r = new Regex("^[a-zA-Z0-9_]+$");
            Snapshot s = null;

            if (!r.IsMatch(nome_utente))
            {
                throw new DatabaseException("nome_utente ha dei caratteri non permessi.",DatabaseErrorCode.FormatError);
            }
            // Controllo se la cartella relativa all'utente esiste
            if (!Directory.Exists(base_path + Path.DirectorySeparatorChar + nome_utente))
            {
                Directory.CreateDirectory(base_path + Path.DirectorySeparatorChar + nome_utente);
            }
            // Genero un nome casuale per il file in locale e controllo che non esista
            string nome_locale;
            string path_locale = base_path + Path.DirectorySeparatorChar + nome_utente + Path.DirectorySeparatorChar;
            do
            {
                nome_locale = RandomFileName();
            } while (File.Exists(path_locale + nome_locale));
            // Creo il file (vuoto)
            FileStream f = File.Create(path_locale + nome_locale);
            // Memorizzo i file nel db, ottengo l'id e ritorno il nuovo snapshot
            string[][] parameters = new string[7][];
            parameters[0] = new string[2] { "@dim", dim.ToString() };
            parameters[1] = new string[2] { "@t_modifica", timestamp.ToString("u") };
            parameters[2] = new string[2] { "@path_relativo_c", path_relativo };
            parameters[3] = new string[2] { "@sha_contenuto", sha_contenuto };
            parameters[4] = new string[2] { "@nome_file_c", nome_file };
            parameters[5] = new string[2] { "@nome_locale_s", nome_locale };
            parameters[6] = new string[2] { "@nome_utente", nome_utente };
            DB_Table db = new DB_Table();
            Log l = Log.getLog();
            db.ExecuteQuery(sql_insert_data, parameters);
            long id = db.getLastInsertedId();
            l.log("Id inserito: "+ id);
/*          Per qualche motivo questa cosa lancia una nullReferenceException, come se s non venisse creato*/
            s = new Snapshot(nome_utente,(int)id);
            s.cambioContenutoIniziato = true;
            s.__scrittura_contenuto = f;
            s.posizione_scrittura = 0;
            s.__sha_contenuto = sha_contenuto;
            //return new Snapshot(nome_utente,(int)id);
            return s;
        }

        // Può essere migliorata con l'uso di RNGCryptoServiceProvider()
        private static string RandomFileName()
        {
            int size = Properties.ApplicationSettings.Default.lunghezza_nomi_locali;
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }
}
