using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace ProgettoMalnati
{
    //Classe che raccoglie i vari snapshot di un file remoto
    class SnapshotList : DB_Table, IEnumerable
    {
        //Attributi
        private int __id_file;
        private System.Collections.Generic.List<int> __list_ids_files;
        private Snapshot[] __snapshots;
        private DateTime[] __timestampList;
        private int snapshotPerFile = Properties.ApplicationSettings.Default.snapshot_per_file;

        static private string sql_get_file_ids_of_user = Properties.SQLquery.sqlGetIdsSnapshots;
        static private string sql_get_versions = Properties.SQLquery.sqlGetVersions;

        //Proprieta
        public int IdFile
        {
            get{ return __id_file; }
        }

        public int Length
        {
            get { return __list_ids_files.Count; }
        }

        public DateTime[] timestampList
        {
            get
            {
                if (__timestampList == null)
                {
                    string[][] parameters = new string[1][];
                    parameters[0] = new string[2] { "@id_file", __id_file.ToString() };

                    System.Collections.Generic.List<DateTime> tmp = new System.Collections.Generic.List<DateTime>();
                    this.ExecuteQuery(sql_get_versions, parameters);
                    //Get the data
                    foreach (int i in this.GetResults())
                    {
                        tmp.Add(DateTime.Parse(this.ResultGetValue("t_modifica").ToString()));
                    }
                    this.__timestampList = tmp.ToArray();
                }

                return __timestampList;
            }
        }

        public Snapshot this[int index] 
        {
            get 
            {
                if (index >= __list_ids_files.Count)
                    throw new IndexOutOfRangeException();

                if (__snapshots == null)
                    this.__snapshots = new Snapshot[snapshotPerFile];
                if (__snapshots[index] == null)
                    __snapshots[index] = new Snapshot(__id_file, __list_ids_files[index]);
                return __snapshots[index];
            }
           // set { }
        }

        public Snapshot this[DateTime timestamp]
        {
            get
            {
                Snapshot s = null;
                for(int i = 0; i < this.Length; i++)
                {
                    if (this[i].InsertTime == timestamp)
                    {
                        s = this[i];
                        break;
                    }
                }

                if(s == null)
                    throw new IndexOutOfRangeException();
                return s;
            }
            // set { }
        }
 
        //Costruttori
        public SnapshotList(int id_file)
            : base()
        {
            //Leggere gli id dei file di questo utente e metterli in __list_ids_files
            this.__id_file = id_file;
            this.__snapshots = new Snapshot[snapshotPerFile];
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@id_file", id_file.ToString() };
            
            this.__list_ids_files = new System.Collections.Generic.List<int>();
            this.ExecuteQuery(sql_get_file_ids_of_user, parameters);
            //Get the data
            foreach (int i in this.GetResults())
            {
                this.__list_ids_files.Add(Int32.Parse(this.ResultGetValue("id").ToString()));
            }
        }

        //Distruttore
        //Metodi
        /// <summary>
        /// Applica la politica di gestione degli snapshot.
        /// </summary>
        /// <param name="timestamp">Nuovo timestamp della modifica</param>
        /// <param name="dim">Nuova dimensione del file</param>
        /// <param name="sha256">Nuovo hash del file in codifica base64</param>
        /// <returns></returns>
        public Snapshot Nuovo(DateTime timestamp, int dim, string sha256)
        {
            Snapshot s;
            __snapshots = null;

            if (__list_ids_files.Count < snapshotPerFile)
            {
                s = Snapshot.creaNuovo(this.__id_file, timestamp, dim, sha256);
                __list_ids_files.Insert(0, s.Id);
            }
            else
            {
                s = new Snapshot(__id_file, __list_ids_files.Last());
                s.cambiaContenuto(dim, timestamp, sha256);
            }
            return s;
        }
        /// <summary>
        ///     Usata per ciclare sui file di un utente.
        ///     Non carica tutti i file in una volta
        /// </summary>
        /// <returns>
        ///     Un iteratore per il costrutto "foreach".
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            int index;
            for (index = 0; index < this.__list_ids_files.Count; index++)
            {
                yield return new Snapshot(this.__id_file, __list_ids_files[index]);
            }
        }

        public void DistruggiTutto()
        {
            Snapshot s;
            for(int i=0; i < this.Length; i++)
            {
                s = this[i];
                s.RimuoviContenuto();
                s = null;
            }
            string sql = "DELETE FROM snapshots WHERE id_file = @id_file;";
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@id_file", __id_file.ToString() };
            this.ExecuteQuery(sql, parameters);
        }

        //Metodi Statici
        /// <summary>
        /// Distrugge gli snapshot di un file.
        /// </summary>
        /// <param name="nome_utente"></param>
        /// <param name="id_file"></param>
        static public void RimuoviSnapshotsDiFile(string nome_utente, int id_file)
        {
            DB_Table db = new DB_Table();
            Log l = Log.getLog();
            string sql = "SELECT nome_locale_s FROM snapshots WHERE id_file = @id_file;";
            string[][] parameters = new string[1][];
            string local_file = "";
            string local_path = Properties.ApplicationSettings.Default.base_path + Path.DirectorySeparatorChar + "users_files"+Path.DirectorySeparatorChar+nome_utente;
            parameters[0] = new string[2]{ "@id_file", id_file.ToString()};
            db.ExecuteQuery(sql, parameters);
            foreach (int i in db.GetResults())
            {
                local_file = (string)db.ResultGetValue("nome_locale_s");
                try
                {
                    File.Delete(local_path + Path.DirectorySeparatorChar + local_file);
                }
                catch(Exception e)
                {
                    l.log("Errore nell'eliminare i file da disco. " + e.Message);
                    throw;
                }
            }
        }
    }
}
