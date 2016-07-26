using System;
using System.Collections;
using System.IO;
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
        private Log l;

        //Proprieta
        public string NomeUtente
        {
            get{ return __nome_utente; }
        }

        public int Length
        {
            get { return __list_ids_files.Count; }
        }

        /// <summary>
        /// Restituisce un file dell'utente
        /// </summary>
        /// <param name="index">Indica un file tra quelli dell'utente. Non è l'id.</param>
        /// <returns></returns>
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
        }

        /// <summary>
        /// Restituisce un file dell'utente in base al nome e al path.
        /// </summary>
        /// <param name="nome_file">Nome del file.</param>
        /// <param name="path_relativo">Path del file sul client.</param>
        /// <returns></returns>
        public FileUtente this[string nome_file,string path_relativo]
        {
            get
            {
                for(int i = 0; i < this.__list_ids_files.Count; i++)
                {
                    if (this[i].NomeFile == nome_file && this[i].PathRelativo == path_relativo)
                        return this[i];
                }
                throw new DatabaseException(" Non esiste nessun file con questo nome.", DatabaseErrorCode.FileNonEsistente);
            }
        }

        //Costruttori
        public FileUtenteList(string nome_utente)
            : base()
        {
            l = Log.getLog();
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
            this.__file_list = new FileUtente[this.__max_file];
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

        public FileUtente nuovoFile(string nome_file, string path_relativo,DateTime t_creazione= new DateTime())
        {

            //Se non c'è spazio, cerco un capro espiatorio da buttare per far posto a quello nuovo,
            //Altrimenti lancio un'eccezione
            string[][] parameters = new string[1][];
            if (this.__list_ids_files.Count >= this.__max_file)
            {
                int id_da_sacrificare=-1;
                parameters[0] = new string[2] { "@nome_utente", __nome_utente};
                this.ExecuteQuery(Properties.SQLquery.sqlCercaFileDaDistruggere, parameters);
                foreach(int i in this.GetResults())
                {
                    id_da_sacrificare = Int32.Parse(this.ResultGetValue("id").ToString());
                    break;
                }

                if (id_da_sacrificare >= 0)
                {
                    for(int i = 0; i < this.Length; i++)
                    {
                        if(this[i].Id == id_da_sacrificare)
                        {
                            this[i].Distruggi();
                        }
                    }
                }
                else
                {
                    this.l.log("Non c'è più posto per l'utente " + __nome_utente, Level.INFO);
                    throw new DatabaseException("Non è più possibile inserire nuovi file. Limite superato.", DatabaseErrorCode.LimiteFileSuperato);
                }
            }

            if(t_creazione == DateTime.MinValue)
            {
                t_creazione = DateTime.Now;
            }

            parameters = new string[4][];
            parameters[0] = new string[2] { "@t_modifica", t_creazione.ToString("u") };
            parameters[1] = new string[2] { "@path_relativo_c", path_relativo };
            parameters[2] = new string[2] { "@nome_file_c", nome_file };
            parameters[3] = new string[2] { "@nome_utente", this.__nome_utente };
            DB_Table db = new DB_Table();
            Log l = Log.getLog();
            db.ExecuteQuery(Properties.SQLquery.sqlNuovoFile, parameters);
            long id = db.getLastInsertedId();

            FileUtente file = new FileUtente(this.__nome_utente, (int)id);
            this.__list_ids_files.Add(file.Id);
            this.__file_list[this.__list_ids_files.Count - 1] = file;
            return file;
        }

        /// <summary>
        /// Eliminare un file significa settarlo come non Valido. Un file non valido
        /// resta nella lista dei file dell'utente. Viene eliminato definitivamente 
        /// il più vecchio file tra quelli invalidi solo quando l'utente eccede il numero
        /// di file che possiede.
        /// </summary>
        /// <param name="file">Il file da eliminare</param>
        public void eliminaFile(FileUtente file)
        {
            if (file.Valido)
            {
                file.Valido = false;
            }
        }
        //Metodi Statici


    }
}
