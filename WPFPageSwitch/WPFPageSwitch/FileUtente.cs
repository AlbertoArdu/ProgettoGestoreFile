using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFPageSwitch
{
    class FileUtente: DB_Table
    {
        private string __nome_file_c;
        private string __path_relativo_c;
        private DateTime __t_creazione;
        private Log l;
        static private string sql_get_file_data = Properties.SQLquery.sqlGetFileData;

        public FileUtente(int id): base()
        {
            l = Log.getLog();
            //Miglioramenti futuri: rendere questo valore personalizzabile per utente.
            __snapshot_per_file = Properties.Settings.Default.snapshot_per_file;
            __snapshots = new SnapshotList(id);
            string[][] parameters = new string[1][];

            parameters[0] = new string[2] { "@id", id.ToString() };
            this.ExecuteQuery(sql_get_file_data, parameters);
            //Get the data
            foreach (Int32 i in GetResults())
            {
                this.__nome_file_c = (string)(this.ResultGetValue("nome_file_c"));
                this.__t_creazione = (DateTime)(this.ResultGetValue("t_creazione"));
                this.__path_relativo_c = (string)(this.ResultGetValue("path_relativo_c"));
                this.__valido = (bool)(this.ResultGetValue("valido"));

            }
        }

        public SnapshotList Snapshots => __snapshots;
        public DateTime IstanteCreazione => __t_creazione;
        public bool Valido
        {
            get
            {
                return __valido;
            }
            set
            {
                __valido = value;
            }
        }
        public string NomeFile
        {
            get
            {
                return __nome_file_c;
            }
            set
            {
                __nome_file_c = value;
            }
        }

    }
}