using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgettoMalnati
{
    
    class Snapshot : db_interface
    {
        //Attributi
        private string __nome_file;
        private string __nome_utente;
        private string __path_relativo;
        private string __sha1_contenuto;
        //Il tipo string qua non va bene: da rivedere!!
        private string __t_inserimento;
        private int __id;
        private int __dim;

        private static string sql_get_snapshot_data 
                            = "SELECT nome_file, path_relativo, dim, t_inserimento, sha1_contenuto"+
                               "FROM snapshots"+
                               "WHERE nome_utente = @nome_utente AND id = @id";
        //Proprieta
        public int Dim 
        {
            get { return __dim; }
        }
        public int Id
        {
            get { return __id; }
        }
        public string NomeUtente
        {
            get { return __nome_utente; }
        }
        //Costruttori
        public Snapshot(string nome_utente,int id) { 
            string[][] parameters = new string[2][];
            parameters[0] = new string[2] {"@nome_utente",nome_utente};
            parameters[1] = new string[2] { "@id", id.ToString() };

            this.ExecuteQuery(sql_get_snapshot_data,parameters);
            //Get the data
            foreach (int i in this.GetResults())
            {
                this.__nome_file = (string)(this.ResultGetValue("nome_file"));
                this.__path_relativo = (string)(this.ResultGetValue("path_relativo"));
                this.__dim = (int)(this.ResultGetValue("dim"));
                //Da rivedere!!
                this.__t_inserimento = (string)(this.ResultGetValue("t_inserimento"));
                this.__sha1_contenuto = (string)(this.ResultGetValue("sha1_contenuto"));

            }
        }
        //Distruttore
        //Metodi
        //Funzioni Statiche
    }
}
