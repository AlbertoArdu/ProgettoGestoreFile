using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace ProgettoMalnati
{

    //Si occupa di controllare il limite di file per utente e modella una lista di FileUtente
    class FileUtenteList : DB_Table, IEnumerable
    {
        //Attributi
        private string __nome_utente;
        private System.Collections.Generic.List<int> __list_ids_files;
        private FileUtente[] __file_list;
        static private string sql_get_file_ids_of_user = Properties.SQLquery.sqlGetIds;
        private int __max_file = 0;
        //Proprieta
        public string NomeUtente
        {
            get{ return __nome_utente; }
        }

        public int Length
        {
            get { return __list_ids_files.Count; }
        }

        public FileUtente this[int index] 
        {
            get
            {
                if(__file_list[index] == null)
                {
                    __file_list[index] = new FileUtente(__nome_utente, __list_ids_files[index]);
                }
                return __file_list[index];
            }
           // set { }
        }

        public FileUtente this[string nome_file]
        {
            get
            {
                for(int i = 0; i < this.__list_ids_files.Count; i++)
                {
                    if (this[i].NomeFile == nome_file)
                        return this[i];
                }
                throw new DatabaseException(" Non esiste nessun file con questo nome.", DatabaseErrorCode.FileNonEsistente);
            }
        }

        //Costruttori
        public FileUtenteList(string nome_utente)
            : base()
        {
            this.__nome_utente = nome_utente;
            this.__max_file = Properties.ApplicationSettings.Default.numero_file;
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@nome_utente", nome_utente };
            this.__list_ids_files = new System.Collections.Generic.List<int>();
            this.ExecuteQuery(sql_get_file_ids_of_user, parameters);
            //Get the data
            foreach (int i in this.GetResults())
            {
                this.__list_ids_files.Add(Int32.Parse(this.ResultGetValue("id").ToString()));
            }
            this.__file_list = new FileUtente[this.__list_ids_files.Count];
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
        //Metodi Statici
    }
}
