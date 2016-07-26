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
        private string __nome_utente;
        private System.Collections.Generic.List<int> __list_ids_files;
        static private string sql_get_file_ids_of_user = Properties.SQLquery.sqlGetIds;

        //Proprieta
        public int IdFile
        {
            get{ return __id_file; }
        }

        public int Length
        {
            get { return __list_ids_files.Count; }
        }

        public ProgettoMalnati.Snapshot this[int index] 
        {
            get 
            {
                ProgettoMalnati.Snapshot ss = new Snapshot(__nome_utente, __list_ids_files[index]);
                return ss;
            }
           // set { }
        }

        //Costruttori
        public SnapshotList(int id_file, string nome_utente)
            : base()
        {
            //Leggere gli id dei file di questo utente e metterli in __list_ids_files
            this.__id_file = id_file;
            this.__nome_utente = nome_utente;
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
                yield return new Snapshot(this.__nome_utente, __list_ids_files[index]);
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

        static public void RimuoviSnapshotsDiFile(string nome_utente, int id_file)
        {
            DB_Table db = new DB_Table();

            string sql = "SELECT nome_locale_s FROM snapshots WHERE id_file = @id_file;";
            string[][] parameters = new string[1][];
            string local_file = "";
            string local_path = Properties.ApplicationSettings.Default.base_path + Path.DirectorySeparatorChar + "users_files"+Path.DirectorySeparatorChar+nome_utente;
            parameters[0] = new string[2]{ "@id_file", id_file.ToString()};
            db.ExecuteQuery(sql, parameters);
            foreach (int i in db.GetResults())
            {
                local_file = (string)db.ResultGetValue("nome_locale_s");
                try{
                File.Delete(local_path + Path.DirectorySeparatorChar + local_file);
            }
        }
    }
}
