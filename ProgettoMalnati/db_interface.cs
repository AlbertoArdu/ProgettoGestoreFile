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
        //private SQLiteDataAdapter DB;
        private DataSet DS = new DataSet();
        private DataTable DT = new DataTable();
        
        //static private bool flag;
        static String nome_file_db = "MyDatabase.sqlite";
        static private int count_ref = 0;
        static private SQLiteConnection sql_con = null;

        static private string[] db_structure = {
                        //Tabelle utenti
                        "create table utenti (nome varchar(20), password varchar(100), "+
                        "path_monitorato varchar(250), PRIMARY KEY(nome))",
                        //Tabella snapshot
                        "create table snapshots ( id int NOT NULL AUTO INCREMENT"+                    
                        "nome_utente varchar(20), nome_file varchar(50), "+
                        "path_relativo varchar(100), dim int, "+
                        "t_inserimento timestamp NOT NULL DEFAULT CURRENT TIMESTAMP, "+
                        "contenuto blob, sha1_contenuto char(24),"+
                        "PRIMARY KEY (nome_utente,id), FOREIGN KEY (nome_utente) REFERENCES utenti(nome));"
                                                     };
        static private string[] utenti_di_test = {
                        "INSERT INTO utenti(nome, password, path_monitorato) VALUES ('tizio', 'abbecedario','C:\\user\\Documents');",
                        "INSERT INTO utenti(nome, password, path_monitorato) VALUES ('caio', 'abbecedario','C:\\user\\Documents');",
                        "INSERT INTO utenti(nome, password, path_monitorato) VALUES ('sempronio', 'abbecedario','C:\\user\\Documents');",
                        "INSERT INTO utenti(nome, password, path_monitorato) VALUES ('cesare', 'abbecedario','C:\\user\\Documents');"
                                                    };
        static private string[] file_di_test = {
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (1, 'tizio', 'cose_importanti.txt', '\\.', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (2, 'tizio', 'cose_importanti.txt', '\\Scuola', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (3, 'tizio', 'robette.exe', '\\Scuola', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (4, 'tizio', 'non_aprire.dll', '\\\\Scuola\\Programmi', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (5, 'caio', 'robette.exe', '', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (6, 'caio', '1234', '\\lkjhgfd', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (7, 'caio', 'liste.lst', '\\mnbvdt\\fgvcdr', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (8, 'caio', 'parcella.doc', '\\Economica', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (9, 'sempronio', 'doc1.doc', '\\.', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (10, 'sempronio', 'robette.exe', '\\Altro', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (11, 'sempronio', 'doc2.doc', '\\Cose', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (12, 'sempronio', 'doc3.doc', '\\Scuola', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (13, 'cesare', 'armate.xls', '\\Utilita', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (14, 'cesare', 'colosseo.jpg', '\\Scuola', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (15, 'cesare', 'pompeo.jpg', '\\.', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                         "INSERT INTO snapshots(id, nome_utente, nome_file, path_relativo, sha1_contenuto) VALUES (16, 'cesare', 'galliche.log', '\\.', '1f8a690b7366a2323e2d5b045120da7e93896f47')",
                                                     };
        public db_interface()
        {
            Console.Write("\tCostruttore db_interface\n");
            if (sql_con == null)
            {
                Console.Write("\tConnetto al db\n");
                String s = "Data Source=";
                s += nome_file_db + ";Versione=3;";
                if (!File.Exists(nome_file_db))
                {
                    Console.Write("\tDB non esistente. Devo crearlo\n");
                    //SQLiteConnection.CreateFile("MyDatabase.sqlite");
                    db_interface.sql_con = new SQLiteConnection(s);
                    db_interface.sql_con.Open();
                    Crea_DB();
                    Console.Write("\tDB creato\n");
                }
                else
                {
                    db_interface.sql_con = new SQLiteConnection(s);
                    db_interface.sql_con.Open();
                }
            }
            count_ref++;
            Console.Write("Connesso. Su questa connessione ci sono " + count_ref.ToString() + " oggetti.\n");
            this.command = db_interface.sql_con.CreateCommand();
            this.reader = null;

        }

        ~db_interface()
        {
            count_ref--;
            if (count_ref == 0) 
            {
                Console.Write("\tChiudo la connessione con il db\n");
                db_interface.sql_con.Close();
                db_interface.sql_con = null;
            }
        }

        private void Crea_DB()
        {
            foreach (string sql in db_structure)
            {
                Console.Write("\t\tCreo la struttura del DB\n");
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            //Alcuni valori di test::
            foreach (string sql in utenti_di_test)
            {
                Console.Write("\t\tInserisco nel DB gli utenti di test\n");
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            foreach (string sql in file_di_test)
            {
                Console.Write("\t\tInserisco nel DB snapshot di test\n");
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

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
            Console.Write("\t\t\tExecuteNonQuery()\n");
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

        /// <summary>
        /// Ritorna un iteratore per leggere i risultati di un query
        /// uno alla volta con il costrutto "foreach"
        /// </summary>
        /// <returns>
        /// Iteratore su stringa (MODIFICARE!!!)
        /// </returns>
        protected IEnumerable<int> GetResults()
        {
            int i= 0;
            if (this.reader != null)
            {
                try
                {
                    while (reader.Read())
                        yield return i++;
                }
                finally
                {
                    reader.Close();
                    reader = null;
                }
            }
        }

        protected object ResultGetValue(string field_name) 
        {
            return reader[field_name];
        }
    }
}