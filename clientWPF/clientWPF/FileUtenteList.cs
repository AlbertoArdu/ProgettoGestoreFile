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
        public FileUtenteList(): base()
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
}
