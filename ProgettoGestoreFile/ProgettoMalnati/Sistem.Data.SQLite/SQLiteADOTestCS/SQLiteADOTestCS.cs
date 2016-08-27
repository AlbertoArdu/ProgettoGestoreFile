//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//namespace subproject1
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            Console.WriteLine("Welcome to C# on Windows Embedded Systems");
//        }
//    }
//}
using System;
using System.Text;
//Note need to add reference to the DLL,System.Data.SQLite.dll,  by browsing to it under C:\WINCE800\3rdParty\CESQLite2013\SQLiteADONET\Resources
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace SQLiteADOTestCS
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to SQLite using ADO.NET, C# on Windows Embedded Compact 2013");

            string databasefile = "\\test.db";
            if (File.Exists(databasefile))
                File.Delete(databasefile);

            //Do exercise twice so can read in db created.
            for (int i = 0; i < 2; i++)
            {
                //Open the db (and create if it doesn't exist

                //Probbaly not a good idea to create the db where the dll are as they could be XIP in ROM.
                //string dbPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase), databasefile);
                string dbPath = databasefile;

                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dbPath));
                conn.Open();

                Console.WriteLine("db openned");

                Console.WriteLine("Create Table ");

                string cmdText;
                SQLiteCommand cmd;
                int res;

                try
                {
                    //Create table
                    cmdText = "CREATE TABLE Persons(Name)";
                    cmd = new SQLiteCommand(cmdText);
                    cmd.Connection = conn;
                    res = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't create table. Table probably already exists. Error message: \n {0} \n", ex.Message);
                }

                Console.WriteLine("Write 3 records into the db");


                //Insert som reecords
                cmdText = "INSERT INTO Persons Values ('David')";
                cmd = new SQLiteCommand(cmdText);
                cmd.Connection = conn;
                res = cmd.ExecuteNonQuery();

                cmdText = "INSERT INTO Persons Values ('Jones')";
                cmd = new SQLiteCommand(cmdText);
                cmd.Connection = conn;
                res = cmd.ExecuteNonQuery();

                cmdText = "INSERT INTO Persons Values ('Hello')";
                cmd = new SQLiteCommand(cmdText);
                cmd.Connection = conn;
                res = cmd.ExecuteNonQuery();

                Console.WriteLine("Query the db");


                //Query the db
                cmdText = "SELECT * FROM Persons";
                cmd = new SQLiteCommand(cmdText);
                cmd.Connection = conn;
                SQLiteDataReader reader = cmd.ExecuteReader();

                Console.WriteLine("Write out all records");

                while (reader.Read())
                {
                    Console.WriteLine(" {0} = {1}", reader.GetName(0), reader[0]);
                }
                reader.Close();

                Console.WriteLine("Close the db");


                //Close the db
                conn.Close();
            }
        }
    }
}
