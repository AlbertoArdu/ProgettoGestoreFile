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
        static private string sql_insert_user = "INSERT INTO utenti(nome,password,path_monitorato) VALUES (@nome,@password,@path_monitorato);";
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
        //Da valutare se rendere privato -> nuovo user solo attraverso la funzione di Login
        public User(string nome, string password) 
            : base()
        {
            Console.Write("Creo un utente: nome ->"+nome+"; pass->"+password+"\n");            
            string[][] parameters = new string[2][];
            parameters[0] = new string[2] { "@nome", nome };
            parameters[1] = new string[2] { "@password", password };
            int quantita = 0;
            this.ExecuteQuery(sql_get_user_data, parameters);
            //Get the data
            Console.Write("Ora bisogna tirar fuori i dati...\n");
            
            foreach (int i in this.GetResults()) 
            {
                this.__nome = (string)(this.ResultGetValue("nome"));
                this.__password = (string)(this.ResultGetValue("password"));
                this.__path_monitorato = (string)(this.ResultGetValue("path_monitorato"));
                quantita = i;
            }
            Console.Write("Andata... User = "+this.ToString());

            //i contiene il numero di risultati;
            if(quantita < 1){
                throw new UserNotFoundException();
            }
            else if (quantita > 1) {
                throw new UserNotFoundException("Panic! More than one user with same password and name or sql wrong");
            }
            //Create the snapshot list
            __s_list = new SnapshotList(this.__nome);
        }
        //Metodi

        override public string ToString()
        {
            string s = "User\n{\n\tnome = " + this.__nome + "\n\tpassword = " + this.__password + "\n\tpath_monitorato = " + __path_monitorato + "\n\ts_list = ";
            if (this.__s_list != null)
                return s + this.__s_list.ToString() + "\n}\n";
            else
                return s + "null\n}\n";
        }

        //Metodi Statici
        static public User Login(string nome,string password)
        {
            return new User(nome, password);
        }

        /// <summary>
        /// Funzione per registrare un nuovo utente
        /// </summary>
        /// <param name="nome">Nome utente</param>
        /// <param name="password">Password per l'utente (cleartext)</param>
        /// <param name="path_monitorato">Path Locale dell'utente che verra monitorato</param>
        static public void RegistraUtente(string nome,string password, string path_monitorato)
        {
            //provo a mettere il nuovo utente: se ricevo un'eccezione particolare il nome utente e duplicato
            db_interface db = new db_interface();
            string[][] parameters = new string[3][];
            parameters[0] = new string[2] { "@nome", nome };
            parameters[1] = new string[2] { "@password", password };
            parameters[2] = new string[2] { "@path_monitorato", path_monitorato };
            //try{
                db.ExecuteQuery(sql_insert_user, parameters);
            /*}
            //Qua bisogna mettere l'eccezione che viene lanciata quando il nome utente e ripetuto
            catch (SQLiteExeption e)
            {
                //TODO: Notificare il chiamante che il nome utente esiste gia
            }
            */
            return;
        }
    }
}
