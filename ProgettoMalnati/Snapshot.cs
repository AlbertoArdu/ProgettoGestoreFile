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
        private int __id;
        private int __dim;

        private static const string sql_get_snapshot_data 
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
        }
        //Distruttore
        //Metodi
        //Funzioni Statiche
    }
}
