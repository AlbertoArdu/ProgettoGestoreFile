using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace clientWPF
{
    class FileUtente: DB_Table
    {
        private List<DateTime> fileVersions = null;
        private int id;
        private string __nome_file_c;
        private string __path_relativo_c;
        private string __path_completo;
        private DateTime __t_creazione;
        private DateTime __t_modifica;
        private bool __valido;
        private int __dim;
        private string sha_contenuto;
        private Log l;
        static private string sql_get_file_data = Properties.SQLquery.sqlGetFileData;
        static private string sql_get_version_data = Properties.SQLquery.sqlGetVersionData;
        static private string sql_set_file_data = Properties.SQLquery.sqlSetFileData;
        static private string sql_nuovo_file = Properties.SQLquery.sqlNuovoFile;
        static private string sql_add_version = Properties.SQLquery.sqlAddVersion;

        public List<DateTime> Items => fileVersions;

        //Proprietà
        public int Id => id;

        public string Nome
        {
            get{ return __nome_file_c; }
            set
            {
                __nome_file_c = value;
            }
        }

        public string Path
        {
            get { return __path_relativo_c; }
            set { __path_relativo_c = value; }
        }

        public DateTime TempoCreazione => __t_creazione;
        public DateTime TempoModifica
        {
            get
            {
                return __t_modifica;
            }
            set
            {
                string sql = "UPDATE file SET t_modifica = @t_modifica WHERE id = @id;";
                string[][] parameters = new string[2][];
                parameters[0] = new string[2] { "@t_modifica", value.ToString("u") };
                parameters[1] = new string[2] { "@id", this.id.ToString() };
                try
                {
                    this.ExecuteQuery(sql, parameters);
                }
                catch (Exception e)
                {
                    l.log("Errore nell'aggiornare il timestamp nel database: " + e.Message, Level.ERR);
                    throw;
                }
                __t_modifica = value;
            }
        }
        //Modificabili solo dalla funzione di aggiornamento
        public int Dimensione => __dim;
        public string SHA256Contenuto
        {
            get
            {
                if (sha_contenuto == null)
                    sha_contenuto = FileUtente.CalcolaSHA256(File.Open(this.__path_completo, FileMode.Open));
                return sha_contenuto;
            }
        }

        public FileUtente(int id): base()
        {

            l = Log.getLog();
            string[][] parameters = new string[1][];

            parameters[0] = new string[2] { "@id_file", id.ToString() };
            this.ExecuteQuery(sql_get_file_data, parameters);
            //Get the data
            object o;

            CultureInfo en = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = en;

            foreach (Int32 i in GetResults())
            {
                o = this.ResultGetValue("nome_file_c");
                this.__nome_file_c = (string)(o);

                o = this.ResultGetValue("path_relativo_c");
                this.__path_relativo_c = (string)(o);

                this.__dim = (Int32)(this.ResultGetValue("dim"));
                o = this.ResultGetValue("sha_contenuto");
                this.sha_contenuto = (string)(o);

                o = this.ResultGetValue("t_modifica");
                DateTime utctMod = (DateTime)(o);
                this.__t_modifica = utctMod.ToUniversalTime();

                o = this.ResultGetValue("t_creazione");
                DateTime utctCreaz = (DateTime)(o);
                this.__t_creazione = utctCreaz.ToUniversalTime();

                o = this.ResultGetValue("valido");
                this.__valido = (bool)(o);
            }
            this.id = id;
            this.__path_completo = Properties.Settings.Default.base_path + System.IO.Path.DirectorySeparatorChar + this.Path +
                                    System.IO.Path.DirectorySeparatorChar + this.Nome;
            
            fileVersions = new List<DateTime>();

            this.ExecuteQuery(sql_get_version_data, parameters);

            //Get the data
            foreach (Int32 i in GetResults())
            {
                DateTime utctdt = (DateTime)(this.ResultGetValue("timestamp_vers"));
                fileVersions.Add(utctdt.ToUniversalTime());
            }
        }
        
        static public string CalcolaSHA256(FileStream f)
        {
            SHA256 sha_obj = SHA256Managed.Create();
            byte[] hash_val;
            f.Position = 0;
            hash_val = sha_obj.ComputeHash(f);
            f.Close();
            StringBuilder hex = new StringBuilder(hash_val.Length * 2);
            foreach (byte b in hash_val)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public void aggiornaDati(int newDim, DateTime newModTime, string newHash = null)
        {
            this.__dim = newDim;
            this.__t_modifica = newModTime;
            if(newHash == null)
            {
                newHash = FileUtente.CalcolaSHA256(File.Open(this.__path_completo, FileMode.Open));
            }
            this.sha_contenuto = newHash;
            try
            {
                this.BeginTransaction();
                string[][] parameters = new string[2][];
                parameters[0] = new string[2] { "@id_file", this.id.ToString() };
                parameters[1] = new string[2] { "@timestamp_vers", this.__t_modifica.ToString("u") };
                this.ExecuteQuery(sql_add_version, parameters);

                parameters = new string[5][];
                parameters[0] = new string[2] { "@dim", __dim.ToString() };
                parameters[1] = new string[2] { "@t_modifica", __t_modifica.ToString("u") };
                parameters[2] = new string[2] { "@sha_contenuto", sha_contenuto };
                parameters[3] = new string[2] { "@id", id.ToString() };
                parameters[4] = new string[2] { "@valido", __valido ? "TRUE" : "FALSE" };

                this.ExecuteQuery(sql_set_file_data, parameters);
                this.CommitTransaction();
            }
            catch
            {
                this.RollbackTransaction();
                throw;
            }
        }

        public bool Valido
        {
            get
            {
                return __valido;
            }
            set
            {
                string sql = "UPDATE file SET valido = @valido WHERE id = @id;";
                string[][] parameters = new string[2][];
                parameters[0] = new string[2] { "@valido", value ? "TRUE" : "FALSE" };
                parameters[1] = new string[2] { "@id", this.id.ToString() };
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

        static public FileUtente CreaNuovo(string nome_file, string path, DateTime t_creazione, int dim, string sha_contenuto = null)
        {
            int id = 0;
            DB_Table db = new DB_Table();
            sha_contenuto = sha_contenuto != null ? sha_contenuto : "";
            string[][] parameters = new string[6][];

            parameters[0] = new string[2] { "@dim", dim.ToString() };
            parameters[1] = new string[2] { "@t_modifica", t_creazione.ToString("u") };
            parameters[2] = new string[2] { "@t_creazione", t_creazione.ToString("u") };
            parameters[3] = new string[2] { "@sha_contenuto", sha_contenuto };
            parameters[4] = new string[2] { "@nome_file", nome_file };
            parameters[5] = new string[2] { "@path", path };

            db.ExecuteQuery(sql_nuovo_file, parameters);
            id = (int)db.getLastInsertedId();
            return new FileUtente(id);
        }
    }
}