using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace ProgettoMalnati
{
    class SnapshotList : db_interface, IEnumerable
    {
        //Attributi
        private string __nome_utente;
        private System.Collections.Generic.List<int> __list_ids_files;
        static private string sql_get_file_ids_of_user = "SELECT id FROM snapshots WHERE nome_utente = @nome_utente;";
        //Proprieta
        public string NomeUtente
        {
            get{ return __nome_utente; }
        }
        public ProgettoMalnati.Snapshot this[int index] 
        {
            get 
            {
                ProgettoMalnati.Snapshot ss = new Snapshot(__nome_utente, __list_ids_files[index]);
                return ss;
            }
            set { }
        }

        //Costruttori
        public SnapshotList(string nome_utente)
            : base()
        {
            //Leggere gli id dei file di questo utente e metterli in __list_ids_files
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@nome_utente", nome_utente };

            this.ExecuteQuery(sql_get_file_ids_of_user, parameters);
            //Get the data
            foreach (int i in this.GetResults())
            {
                this.__list_ids_files.Add((int)(this.ResultGetValue("id")));
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

        override public string ToString() 
        {
            return "Lista di Snapshot\n";
        }
        //Metodi Statici
    }
}
