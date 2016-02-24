using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ProgettoMalnati
{
    class db_interface
    {
        private SQLiteCommand command;
        private SQLiteDataReader reader;
        private SQLiteDataAdapter DB;
        private DataSet DS = new DataSet();
        private DataTable DT = new DataTable();
        
        static private bool flag;
        static String nome_file_db = "MyDatabase.sqlite";
        static private int count_ref = 0;
        static private SQLiteConnection sql_con;

        public db_interface()
        {
            String s = "Data Source=";
            s += nome_file_db + ";Versione=3;";

            if (!File.Exists(nome_file_db))
            {
                //SQLiteConnection.CreateFile("MyDatabase.sqlite");
                db_interface.sql_con = new SQLiteConnection(s);
                db_interface.sql_con.Open();
                Crea_DB();
            }
            else
            {
                db_interface.sql_con = new SQLiteConnection(s);
                db_interface.sql_con.Open();
            }
            this.command = db_interface.sql_con.CreateCommand();
        }

        private void Crea_DB()
        {
            string sql = "create table utenti ("+
                            "name varchar(20), "+
                            "password varchar(100), "+
                            "path_monitorato varchar(250),"+
                            "PRIMARY KEY(nome))";

            command.CommandText = sql;
            command.ExecuteNonQuery();

            sql = "create table snapshots ("+
                        "id int NOT NULL AUTO INCREMENT"+
                        "nome_utente varchar(20), "+
                        "nome_file varchar(50), "+
                        "path_relativo varchar(100), "+
                        "dim int, "+
                        "t_inserimento timestamp NOT NULL DEFAULT CURRENT TIMESTAMP, "+
                        "contenuto blob, "+
                        "sha1_contenuto char(24),"+
                        "PRIMARY KEY (nome_utente,id), "+
                        "FOREIGN KEY (nome_utente) REFERENCES utenti(nome));";
            
            command.CommandText = sql;
            command.ExecuteNonQuery();
         
            /*
            sql = "create table all_info (nome_utente varchar(20), nome_file varchar(50), path_relativo varchar(100), dim int, tempo timestamp, contenuto blob);";
            command = new SQLiteCommand(sql, db_interface.sql_con);
            command.ExecuteNonQuery();
             */
        }
        /// <summary>
        /// Esegue una query sql parametrizzata.
        /// Errori NON GESTITI!!
        /// </summary>
        /// <param name="txtQuery">Query sql con o senza parametri</param>
        /// <param name="parameters">
        ///     Parametri da sostituire; 
        ///     Formato: parameters[i] = {"@nome_parametro","valore da sostituire"}
        /// </param>
        protected void ExecuteQuery(string txtQuery, string[][] parameters = null)
        {
            command.CommandText = txtQuery;
            if (parameters != null)
            {
                foreach (string[] param in parameters) 
                {
                    command.Parameters.Add(new SQLiteParameter(param[0],param[1]));
                }
            }
            reader = command.ExecuteReader();
        }   
    }
}