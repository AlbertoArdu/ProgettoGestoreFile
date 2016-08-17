using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoMalnati
{
    class FileUtente: DB_Table
    {
        private int __snapshot_per_file;
        private int id;
        private string __nome_utente = null;
        private string __nome_file_c;
        private string __path_relativo_c;
        private SnapshotList __snapshots;
        private DateTime __t_creazione;
        private bool __valido;
        private Log l;
        static private string sql_get_file_data = Properties.SQLquery.sqlGetFileData;

        public FileUtente(string nome_utente, int id):
            base()
        {
            l = Log.getLog();
            this.id = id;
            //Miglioramenti futuri: rendere questo valore personalizzabile per utente.
            __snapshot_per_file = Properties.ApplicationSettings.Default.snapshot_per_file;
            __snapshots = new SnapshotList(id);
            string[][] parameters = new string[2][];

            parameters[0] = new string[2] { "@id", id.ToString() };
            parameters[1] = new string[2] { "@nome_utente", nome_utente };

            this.ExecuteQuery(sql_get_file_data, parameters);
            if (this.hasResults())
            {
                //Get the data
                foreach (Int32 i in GetResults())
                {
                    this.__nome_file_c = (string)(this.ResultGetValue("nome_file_c"));
                    this.__t_creazione = (DateTime)(this.ResultGetValue("t_creazione"));
                    this.__path_relativo_c = (string)(this.ResultGetValue("path_relativo_c"));
                    this.__valido = (bool)(this.ResultGetValue("valido"));
                    this.__nome_utente = nome_utente;
                }
            }
            else
            {
                throw new DatabaseException("Impossibile ottenere i dati del FileUtente richiesto.", DatabaseErrorCode.NoDati);
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
                string sql = "UPDATE fileutente SET valido = @valido WHERE id = @id AND nome_utente = @nome_utente;";
                string[][] parameters = new string[3][];
                parameters[0] = new string[2] { "@valido", value.ToString() };
                parameters[1] = new string[2] { "@id", this.id.ToString() };
                parameters[2] = new string[2] { "@nome_utente", this.__nome_utente };
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

        /// <summary>
        /// Settare questa proprietà significa notificare il sistema che il file è stato rinominato
        /// da un direttorio ad un altro nel client, ma il contenuto non è cambiato.
        /// </summary>
        public string NomeFile
        {
            get
            {
                return __nome_file_c;
            }
            set
            {
                string[][] parameters = new string[2][];
                parameters[0] = new string[2] { "@id", this.id.ToString() };
                parameters[1] = new string[2] { "@nome_file_c", value };
                this.ExecuteQuery(Properties.SQLquery.sqlCambiaNomeFile, parameters);
                this.__nome_file_c = value;
            }
        }
        /// <summary>
        /// Settare questa proprietà significa notificare il sistema che il file è stato spostato
        /// da un direttorio ad un altro nel client, ma il contenuto non è cambiato.
        /// </summary>
        public string PathRelativo
        {
            get
            {
                return __path_relativo_c;
            }
            
            set
            {
                string[][] parameters = new string[2][];
                parameters[0] = new string[2] { "@id", this.id.ToString() };
                parameters[1] = new string[2] { "@path_monitorato_c", value };
                this.ExecuteQuery(Properties.SQLquery.sqlCambiaPathFile, parameters);
                __path_relativo_c = value;
            }
        }
        public int Id => this.id;

        /// <summary>
        /// Ideata per essere chiamata solo quando si è ecceduto il limite di file creati
        /// </summary>
        public void Distruggi()
        {
            // Cerco i nomi locali degli snapshot e li elimino, poi elimino le entry nella tabella snapshot
            // poi l'entry nella tabella FileUtente
            this.__snapshots.DistruggiTutto();
            string sql = "DELETE FROM fileutente WHERE id_file = @id_file;";
            string[][] parameters = new string[1][];
            parameters[0] = new string[2] { "@id_file", id.ToString() };
            this.ExecuteQuery(sql, parameters);
        }
        
    }
}
