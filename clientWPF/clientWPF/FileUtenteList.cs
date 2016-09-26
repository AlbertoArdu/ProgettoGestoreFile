using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace clientWPF
{

    //Modella una lista di FileUtente
    class FileUtenteList : DB_Table, IEnumerable
    {
        //Attributi
        private System.Collections.Generic.List<int> __list_ids_files;
        private System.Collections.Generic.List<int> __list_deleted_ids;

        private FileUtente[] __file_list;
        private FileUtente[] __deleted_list;
        static private string sql_get_file_ids = Properties.SQLquery.sqlGetId;
        string sql_get_deleted_ids = Properties.SQLquery.sqlGetDeletedIds;
        static private string sql_nuovo_file = Properties.SQLquery.sqlNuovoFile;

        static private FileUtenteList _instance = null;
        //Proprieta
        public int Length => __list_ids_files.Count;

        public FileUtente this[int index] 
        {
            get
            {
                if (__file_list[index] == null)
                {
                    __file_list[index] = new FileUtente(__list_ids_files[index]);
                }
                return __file_list[index];
            }
           // set { }
        }

        public bool getValidity(string nome_file, string path_file)
        {
            for (int i = 0; i < this.__list_ids_files.Count; i++)
            {
                if (this[i].Nome == nome_file && this[i].Path == path_file)
                    return true;
            }
            for (int i = 0; i < this.__list_deleted_ids.Count; i++)
            {
                if (this.Deleted[i].Nome == nome_file && this.Deleted[i].Path == path_file)
                    return false;
            }
            throw new DatabaseException(" Non esiste nessun file con questo nome.", DatabaseErrorCode.FileNonEsistente);
        }

        public FileUtente this[string nome_file, string path_file]
        {
            get
            {
                for(int i = 0; i < this.__list_ids_files.Count; i++)
                {
                    if (this[i].Nome == nome_file && this[i].Path == path_file)
                        return this[i];
                }
                throw new DatabaseException(" Non esiste nessun file con questo nome.", DatabaseErrorCode.FileNonEsistente);
            }
        }

        public FileUtente[] Deleted
        {
            get
            {
                int i = 0;
                foreach (int id in __list_deleted_ids)
                {
                    if(__deleted_list[i] == null)
                        __deleted_list[i++] = (new FileUtente(id));
                }
                return __deleted_list;
            }
        }
        
        //Costruttori
        private FileUtenteList(): base()
        {
            //this.__max_file = Properties.Settings.Default.numero_file;
            this.__list_ids_files = new System.Collections.Generic.List<int>();
            this.__list_deleted_ids = new System.Collections.Generic.List<int>();
            this.ExecuteQuery(sql_get_file_ids, null);
            //Get the data
            foreach (int i in this.GetResults())
            {
                this.__list_ids_files.Add(Int32.Parse(this.ResultGetValue("id").ToString()));
            }
            this.ExecuteQuery(sql_get_deleted_ids, null);
            //Get the data
            foreach (int i in this.GetResults())
            {
                this.__list_deleted_ids.Add(Int32.Parse(this.ResultGetValue("id").ToString()));
            }
            this.__file_list = new FileUtente[this.__list_ids_files.Count];
            this.__deleted_list = new FileUtente[this.__list_deleted_ids.Count];
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
                yield return this[index];
            }
        }

        public void Delete(int id)
        {
            int index;
            lock (this)
            {
                if (__list_deleted_ids.Contains(id))
                    return;
                if ((index = __list_ids_files.IndexOf(id)) == -1)
                    throw new ArgumentException("L'id fornito non appartiene ad alcun file");
                this[index].Valido = false;
                __list_deleted_ids.Add(id);
                __list_ids_files.RemoveAt(index);
                __file_list = new FileUtente[__list_ids_files.Count];
                __deleted_list = new FileUtente[__list_deleted_ids.Count];
            }
        }
        public void Ripristina(int id)
        {
            int index;
            lock (this)
            {
                if (__list_ids_files.Contains(id))
                    return;
                if ((index = __list_deleted_ids.IndexOf(id)) == -1)
                    throw new ArgumentException("L'id fornito non appartiene ad alcun file");
                __list_ids_files.Add(id);
                __list_deleted_ids.RemoveAt(index);
                __file_list = new FileUtente[__list_ids_files.Count];
                __deleted_list = new FileUtente[__list_deleted_ids.Count];
            }
        }
        public FileUtente CreaNuovo(string nome_file, string path, DateTime t_creazione, DateTime t_modifica, int dim, string sha_contenuto = null)
        {
            int id = 0;
            DB_Table db = new DB_Table();
            sha_contenuto = sha_contenuto != null ? sha_contenuto : "";
            string[][] parameters = new string[6][];
            lock (this)
            {
                parameters[0] = new string[2] { "@dim", dim.ToString() };
                parameters[1] = new string[2] { "@t_modifica", t_modifica.ToString("u") };
                parameters[2] = new string[2] { "@t_creazione", t_creazione.ToString("u") };
                parameters[3] = new string[2] { "@sha_contenuto", sha_contenuto };
                parameters[4] = new string[2] { "@nome_file", nome_file };
                parameters[5] = new string[2] { "@path", path };

                db.ExecuteQuery(sql_nuovo_file, parameters);
                id = (int)db.getLastInsertedId();
                FileUtente fu = new FileUtente(id);
                fu.AggiungiVersione(t_modifica);
                this.__list_ids_files.Add(id);
                __file_list = new FileUtente[__list_ids_files.Count];
                __deleted_list = new FileUtente[__list_deleted_ids.Count];
                return fu;
            }
        }

        //Static methods
        /// <summary>
        /// Restituisce tutti i file in una cartella, scendendo ricorsivamente nelle sottocartelle
        /// </summary>
        /// <param name="rootFolderPath">Indica il path base, scritto con il separatore alla fine</param>
        /// <returns>
        /// Un array di coppie nome file - percorso, dove il percorso è da intendersi a partire da rootFolderPath
        /// </returns>

        static public List<string[]> exploreFileSystem(string rootFolderPath)
        {
            Queue pending = new Queue();
            pending.Enqueue(rootFolderPath);
            List<string[]> files = new List<string[]>();
            string[] tmp;
            int index = 0;
            string tmp_path;
            while (pending.Count > 0)
            {
                tmp_path = (string)pending.Dequeue();
                tmp = Directory.GetFiles(tmp_path);
                for (int i = 0; i < tmp.Length; i++)
                {
                    string[] f_info = new string[2];
                    f_info[0] = Path.GetFileName(tmp[i]);
                    f_info[1] = Path.GetDirectoryName(tmp[i]);
                    f_info[1] = Path.GetDirectoryName(tmp[i]);
                    //index = f_info[1].IndexOf(rootFolderPath);
                    index = f_info[1].IndexOf(rootFolderPath.Substring(0, rootFolderPath.Length - 1));
                    f_info[1] = (index < 0) ?
                        f_info[1] : f_info[1].Remove(index, rootFolderPath.Length - 1);
                    f_info[1] = (f_info[1].Length == 0) ? "\\." : f_info[1];
                    files.Add(f_info);
                }
                tmp = Directory.GetDirectories(tmp_path);
                for (int i = 0; i < tmp.Length; i++)
                {
                    pending.Enqueue(tmp[i]);
                }
            }
            return files;
        }

        static public FileUtenteList getInstance()
        {
            if(_instance == null)
            {
                _instance = new FileUtenteList();
            }
            _instance.__file_list = new FileUtente[_instance.__list_ids_files.Count];
            _instance.__deleted_list = new FileUtente[_instance.__list_deleted_ids.Count];
            return _instance;
        }
    }
}
