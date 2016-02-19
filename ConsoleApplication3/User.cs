using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ConsoleApplication3
{
    class User : db_interface
    {
        private string __nome;
        private string __passwd;
        private string __path_moitorato;

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
                    
            }
        }
  
        public string PathMonitorato{
            get { return __path_moitorato; }
            set { __path_moitorato = value; }
        }

        public User(string nome = null, string passwd = null)
        {

        }

        public ~User();

    }
}
