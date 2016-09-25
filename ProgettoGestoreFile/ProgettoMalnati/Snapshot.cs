using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

namespace ProgettoMalnati
{

    class Snapshot : DB_Table
    {
        //Attributi
        private string __nome_locale;
        private string __sha_contenuto;
        private DateTime __t_modifica;
        private int __id;
        private int __id_file;
        private int __dim;
        private bool __valido;
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
        public int IdFile
        {
            get { return __id_file; }
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
        public bool Valido
        {
            get { return __valido; }
            set
            {
                string sql = "UPDATE snapshots SET valido = @valido WHERE id = @id AND id_file = @id_file;";
                string[][] parameters = new string[3][];
                parameters[0] = new string[2] { "@valido", value ? "TRUE" : "FALSE"};
                parameters[1] = new string[2] { "@id", this.__id.ToString() };
                parameters[2] = new string[2] { "@id_file", this.__id_file.ToString() };
                try
                {
                    this.ExecuteQuery(sql, parameters);
                }
                catch (Exception e)
                {
                    l.log("Errore nel settare il file come invalido nel database: " + e.Message, Level.ERR);
                    throw;
                }
                __valido = value;
            }
        }
        //Costruttori
        public Snapshot(int id_file, int id)
            : base()
        {
            CultureInfo it = new CultureInfo("it-IT");
            Thread.CurrentThread.CurrentCulture = it;

            l = Log.getLog();
            if (base_path == null)
                base_path = Properties.ApplicationSettings.Default.base_path + Path.DirectorySeparatorChar + "users_files";
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@id", id.ToString() };
            this.ExecuteQuery(sql_get_snapshot_data, parameters);
            if (this.hasResults())
            {
                //Get the data
                foreach (Int32 i in GetResults())
                {
                    this.__dim = (int)(this.ResultGetValue("dim"));
                    DateTime utctMod = (DateTime)(this.ResultGetValue("t_modifica"));
                    this.__t_modifica = utctMod.ToUniversalTime();
                    this.__sha_contenuto = (string)(this.ResultGetValue("sha_contenuto"));
                    this.__nome_locale = (string)(this.ResultGetValue("nome_locale_s"));
                    this.__valido = (bool)(this.ResultGetValue("valido"));
                }
                this.__id_file = id_file;
                this.__id = id;
            }
            else
            {
                throw new DatabaseException("Impossibile ricavare i dati su uno snapshot.", DatabaseErrorCode.NoDati);
            }
        }

        //Distruttore
        ~Snapshot()
        {
            if (this.cambioContenutoIniziato)
            {
                try
                {
                    l.log("Il contenuto del file non è stato completamente scritto. Il sistema verrà riportato allo stato prececedente.");
                    this.__scrittura_contenuto.Close();
                    string tmp_nome_da_sostituire = base_path + Path.DirectorySeparatorChar + this.__nome_locale;
                    File.Delete(tmp_nome_da_sostituire);
                    File.Move(tmp_nome_da_sostituire + ".tmp", tmp_nome_da_sostituire);
                }
                catch (Exception e)
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
                throw new DatabaseException("Impossibile leggere il contenuto di uno snapshot mentre è in corso una modifica.", DatabaseErrorCode.SnapshotInScrittura);
            if (this.__lettura_contenuto == null)
            {
                try
                {
                    l.log("Path locale - " + base_path);
                    this.__lettura_contenuto = new FileStream(base_path + Path.DirectorySeparatorChar + this.__nome_locale, FileMode.Open, FileAccess.Read);
                }
                catch (Exception e)
                {
                    l.log("Errore nel nome del file sul server o nel path specificato. " + e.ToString());
                    throw;
                }
            }
            int q = 0;
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
                if (this.__lettura_contenuto != null)
                    this.__lettura_contenuto.Close();
                this.__lettura_contenuto = null;
                this.__t_modifica = timestamp;
                this.__dim = newDim;
                string tmp_nome = base_path + Path.DirectorySeparatorChar + this.__nome_locale + ".tmp";
                string tmp_nome_da_sostituire = base_path + Path.DirectorySeparatorChar + this.__nome_locale;

                File.Move(tmp_nome_da_sostituire, tmp_nome);
                this.__scrittura_contenuto = new FileStream(tmp_nome_da_sostituire, FileMode.Create, FileAccess.ReadWrite);
                this.posizione_scrittura = 0;
                this.__sha_contenuto = sha_contenuto;
            }
            else
            {
                throw new DatabaseException("Cambio contenuto già inizializzato. Non richiamare questa funzione finché tutto il contenuto precedente sia stato scritto.", DatabaseErrorCode.SnapshotInScrittura);
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
                throw new DatabaseException("Dimensione fornita non corretta.", DatabaseErrorCode.DimensioneFileEccessiva);
            if (size == 0)
            {
                return;
            }
            this.__scrittura_contenuto.Write(b, 0, size);
            posizione_scrittura += size;
        }

        /// <summary>
        /// Salva il nuovo contenuto, calcola l'hash del file memorizzato e lo confronta con quello fornito
        /// dall'utente (se fornito).
        /// </summary>
        public void completaScrittura()
        {
            //Controllo se è la prima scrittura: allora lancio eccezione
            if (!cambioContenutoIniziato)
            {
                throw new DatabaseException("Non è stata chiamata la funzione cambiaContenuto();", DatabaseErrorCode.SnapshotInLettura);
            }
            SHA256 sha_obj = SHA256Managed.Create();
            byte[] hash_val;
            this.__scrittura_contenuto.Position = 0;
            hash_val = sha_obj.ComputeHash(this.__scrittura_contenuto);
            this.__scrittura_contenuto.Close();
            string tmp_nome = base_path + Path.DirectorySeparatorChar + this.__nome_locale + ".tmp";
            //Elimino il vecchio contenuto
            File.Delete(tmp_nome);
            //Controllo hash
            StringBuilder hex = new StringBuilder(hash_val.Length * 2);
            foreach (byte b in hash_val)
                hex.AppendFormat("{0:x2}", b);
            string sha_reale = hex.ToString();

            if (this.__sha_contenuto != "")
            {
                if (this.__sha_contenuto != sha_reale)
                {
                    throw new DatabaseException("L'hash fornito è diverso dall'hash calcolato.", DatabaseErrorCode.HashInconsistente);
                }
            }
            this.__sha_contenuto = sha_reale;
            CaricaDatiNelDB();
            this.__lettura_contenuto = new FileStream(base_path + Path.DirectorySeparatorChar + this.__nome_locale, FileMode.Open, FileAccess.Read);
            this.cambioContenutoIniziato = false;
        }

        public void CaricaDatiNelDB()
        {
            string[][] parameters = new string[5][];
            parameters[0] = new string[2] { "@dim", __dim.ToString() };
            parameters[1] = new string[2] { "@t_modifica", __t_modifica.ToString("u") };
            parameters[2] = new string[2] { "@sha_contenuto", __sha_contenuto };
            parameters[3] = new string[2] { "@nome_locale_s", __nome_locale };
            parameters[4] = new string[2] { "@id", __id.ToString() };
            
            this.ExecuteQuery(sql_store_data, parameters);
        }

        public void RimuoviContenuto()
        {
            if (this.__lettura_contenuto != null)
            {
                this.__lettura_contenuto.Close();
                this.__lettura_contenuto = null;
            }
            if (this.__scrittura_contenuto != null)
            {
                this.__scrittura_contenuto.Close();
                this.__scrittura_contenuto = null;
            }
            try
            {
                File.Delete(base_path + Path.DirectorySeparatorChar + this.__nome_locale);
            }
            catch (Exception e)
            {
                l.log("Errore nell'eliminazione del file locale." + e.Message, Level.ERR);
                throw;
            }
        }

        //Funzioni Statiche

        public static Snapshot creaNuovo(int id_file,
                                            DateTime timestamp = new DateTime(),
                                            int dim = 0,
                                            string sha_contenuto = "")
        {
            //Controllo che gli argomenti non abbiano caratteri strani
            Snapshot s = null;
            if (base_path == null)
                base_path = Properties.ApplicationSettings.Default.base_path + Path.DirectorySeparatorChar + "users_files";
            // Genero un nome casuale per il file in locale e controllo che non esista
            string nome_locale;
            do
            {
                nome_locale = Path.GetRandomFileName();
            } while (File.Exists(base_path + Path.DirectorySeparatorChar + nome_locale));
            // Creo il file (vuoto)
            FileStream f = File.Create(base_path + Path.DirectorySeparatorChar + nome_locale);
            // Memorizzo i file nel db, ottengo l'id e ritorno il nuovo snapshot
            string[][] parameters = new string[5][];
            parameters[0] = new string[2] { "@dim", dim.ToString() };
            parameters[1] = new string[2] { "@t_modifica", timestamp.ToString("u") };
            parameters[2] = new string[2] { "@sha_contenuto", sha_contenuto };
            parameters[3] = new string[2] { "@nome_locale_s", nome_locale };
            parameters[4] = new string[2] { "@id_file", id_file.ToString() };

            DB_Table db = new DB_Table();
            Log l = Log.getLog();
            db.ExecuteQuery(sql_insert_data, parameters);
            long id = db.getLastInsertedId();
            l.log("Id inserito: " + id);
            /*          Per qualche motivo questa cosa lancia una nullReferenceException, come se s non venisse creato*/
            s = new Snapshot(id_file, (int)id);
            s.cambioContenutoIniziato = true;
            s.__scrittura_contenuto = f;
            s.posizione_scrittura = 0;
            s.__sha_contenuto = sha_contenuto;
            //return new Snapshot(nome_utente,(int)id);
            return s;
        }

    }
}
