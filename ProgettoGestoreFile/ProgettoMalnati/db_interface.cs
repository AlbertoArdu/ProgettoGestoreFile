using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Resources;

namespace ProgettoMalnati
{
    class DB_Table
    {
        private SQLiteCommand command;
        private SQLiteDataReader reader;
        //private SQLiteDataAdapter DB;
        private DataSet DS = new DataSet();
        private DataTable DT = new DataTable();
        
        //static private bool flag;
        static String nome_file_db = Properties.DBSettings.Default.nome_db;
        static private int count_ref = 0;
        static private SQLiteConnection sql_con = null;

        private Log l;

        static private string[] db_structure = {
                        //Tabelle utenti
                        Properties.SQLquery.tabellaUtenti,
                        //Tabella fileutente
                        Properties.SQLquery.tabellaFileUtente,
                        //Tabella snapshot
                        Properties.SQLquery.tabellaSnapshot,
                                                     };

        public DB_Table()
        {
            l = Log.getLog();
            if (sql_con == null)
            {
                count_ref = 0;
                String s = "Data Source=";
                s += nome_file_db + ";Versione=3;";
                if (!File.Exists(nome_file_db))
                {
                    l.log("DB non esistente. Devo crearlo");
                    //SQLiteConnection.CreateFile("MyDatabase.sqlite");
                    DB_Table.sql_con = new SQLiteConnection(s);
                    DB_Table.sql_con.Open();
                    Crea_DB();
                    l.log("DB creato");
                }
                else
                {
                    DB_Table.sql_con = new SQLiteConnection(s);
                    DB_Table.sql_con.Open();
                }
            }
            count_ref++;
            this.reader = null;

        }

        ~DB_Table()
        {
            count_ref--;
            if (count_ref == 0) 
            {
                try
                {
                    DB_Table.sql_con.Close();
                }catch(ObjectDisposedException e)
                {
                    l.log("Warning! La connessione è già stata chiusa ma non so come mai -.-. "+e.Message);
                }
                DB_Table.sql_con = null;
            }
        }

        private void Crea_DB()
        {
            command = sql_con.CreateCommand();
            foreach (string sql in db_structure)
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            //Alcuni valori di test:
            Properties.DatiTest.Culture = CultureInfo.CurrentCulture;
            ResourceSet rs = Properties.DatiTest.ResourceManager
                                        .GetResourceSet(CultureInfo.CurrentCulture, true, true);

            foreach (DictionaryEntry sql in rs)
            {
                command.CommandText = sql.Value.ToString();
                command.ExecuteNonQuery();
            }
            
        }

        /// <summary>
        /// Esegue una query sql parametrizzata.
        /// </summary>
        /// <param name="txtQuery">Query sql con o senza parametri</param>
        /// <param name="parameters">
        ///     Parametri da sostituire; 
        ///     Formato: parameters[i] = {"@nome_parametro","valore da sostituire"}
        /// </param>
        public void ExecuteQuery(string txtQuery, string[][] parameters = null)
        {
            command = sql_con.CreateCommand();
            command.CommandText = txtQuery;
            if (parameters != null)
            {
                foreach (string[] param in parameters) 
                {
                    command.Parameters.Add(new SQLiteParameter(param[0],param[1]));
                }
            }
            try
            {
                command.VerifyOnly();
            }
            catch(Exception e)
            {
                l.log("Query errata: " + e.Message);
                throw;
            }
            try
            {
                reader = command.ExecuteReader();
            }catch (SQLiteException e) when (e.ErrorCode == (int)SQLiteErrorCode.Constraint)
            {
                throw new DatabaseException("Errore di Constraint", DatabaseErrorCode.Constraint);
            }
        }

        /// <summary>
        /// Ritorna un iteratore per leggere i risultati di un query
        /// uno alla volta con il costrutto "foreach"
        /// </summary>
        /// <returns>
        /// Iteratore su int
        /// </returns>
        public IEnumerable<int> GetResults()
        {
            int i= 0;
            if (this.reader != null)
            {
                try
                {
                    while (reader.Read())
                    {
                        yield return ++i;
                    }
                }
                finally
                {
                    reader.Close();
                    reader = null;
                }
            }
        }

        public object ResultGetValue(string field_name) 
        {
            return reader[field_name];
        }

        public long getLastInsertedId()
        {
            command = sql_con.CreateCommand();
            string sql = "select last_insert_rowid()";
            command.CommandText = sql;
            return (long)command.ExecuteScalar();
        }

        public bool hasResults()
        {
            return reader.HasRows;
        }
    }
}