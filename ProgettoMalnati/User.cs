using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ProgettoMalnati
{
    class User : db_interface
    {
        //Attributi
        private string __nome;
        private string __password;
        private string __path_monitorato;
        private SnapshotList __s_list;
        static private string sql_get_user_data = "SELECT * FROM users WHERE nome = @nome AND password = @password;";
        //Proprieta
        public string Nome
        {
            get { return __nome; }
            set { __nome = value; }
        }
        public string Passwd
        {
            get { return __password; }
            set
            { 
              //Trovare metodo sicuro per memorizzare la password; (hash)      
            }
        }
        public string PathMonitorato
        {
            get { return __path_monitorato; }
            set
            {
                //Bisogna cambiarlo anche nel db!!!
                __path_monitorato = value; 
            }
        }
        //Costruttori
        public User(string nome, string password)
        {
            string[][] parameters = new string[2][];
            parameters[0] = new string[2] { "@nome", nome };
            parameters[1] = new string[2] { "@password", password };

            this.ExecuteQuery(sql_get_user_data, parameters);
            //Get the data
            foreach (int i in this.GetResults()) 
            {
                this.__nome = (string)(this.ResultGetValue("nome"));
                this.__password = (string)(this.ResultGetValue("password"));
                this.__path_monitorato = (string)(this.ResultGetValue("path_monitorato"));
            }
            //Create the snapshot list
            __s_list = new SnapshotList(this.__nome);
        }
        //Metodi
    }
}
