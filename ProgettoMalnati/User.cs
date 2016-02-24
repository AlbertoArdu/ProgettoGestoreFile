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
        private string __passwd;
        private string __path_monitorato;
        private SnapshotList __s_list;
        //Proprieta
        public string Nome
        {
            get { return __nome; }
            set { __nome = value; }
        }
        public string Passwd
        {
            get { return __passwd; }
            set
            { 
              //Trovare metodo sicuro per memorizzare la password; (hash)      
            }
        }
        public string PathMonitorato
        {
            get { return __path_monitorato; }
            set { __path_monitorato = value; }
        }
        //Costruttori
        public User(string nome = null, string passwd = null)
        {

        }
        //Distruttore
        public ~User();
        //Metodi
    }
}
