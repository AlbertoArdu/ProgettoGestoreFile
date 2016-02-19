using Finisar.SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleApplication3
{
    class db_interface
    {
        private SQLiteCommand sql_cmd;
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
        }

        private void Crea_DB()
        { 
            string sql = "create table utente (name varchar(20), password varchar(100), path_monitorato varchar(250))";
            SQLiteCommand command = new SQLiteCommand(sql, db_interface.sql_con);
            command.ExecuteNonQuery();

            sql = "create table snapshot (nome_utente varchar(20), nome_file varchar(50), path_relativo varchar(100), dim int, tempo timestamp, contenuto blob";
            command = new SQLiteCommand(sql, db_interface.sql_con);
            command.ExecuteNonQuery();

            sql = "create table all_info (nome_utente varchar(20), nome_file varchar(50), path_relativo varchar(100), dim int, tempo timestamp, contenuto blob";
            command = new SQLiteCommand(sql, db_interface.sql_con);
            command.ExecuteNonQuery();
        }

        private void SetConnection()
        {
            sql_con = new SQLiteConnection
                ("Data Source="+db_interface.nome_file_db+";Version=3;New=False;Compress=True;");
        }
        /*
        private void ExecuteQuery(string txtQuery)
        {
            SetConnection();
            sql_con.Open();
            sql_cmd = sql_con.CreateCommand();
            sql_cmd.CommandText = txtQuery;
            sql_cmd.ExecuteNonQuery();
            sql_con.Close();
        }
        */
    }
}