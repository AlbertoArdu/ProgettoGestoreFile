using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;

namespace ProgettoMalnati
{
    class User : DB_Table
    {
        //Attributi
        private string __nome;
        private string __password;
        private string __path_monitorato;
        private SnapshotList __s_list;
        static private string sql_get_user_data = Properties.SQLquery.sqlCheckUtente;
        static private string sql_insert_user = Properties.SQLquery.sqlGetInfoUtente;
        //static private string sql_update_password = Properties.SQLquery.sqlUpdatePass;

        //Proprieta
        public string Nome
        {
            get { return __nome; }
            set { __nome = value; }
        }

        /// <summary>
        /// Not possible to get this property;
        /// Set hashes (SHA521) the password before storing it in the Database
        /// </summary>
        public string Passwd
        {
            set
            {
                SHA512 alg = SHA512.Create();
                byte[] result = alg.ComputeHash(Encoding.UTF8.GetBytes(value));
                __password = System.Convert.ToBase64String(result);
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
            foreach (int i in this.GetResults()) 
            {
                this.__nome = (string)(this.ResultGetValue("nome"));
                this.__password = (string)(this.ResultGetValue("password"));
                this.__path_monitorato = (string)(this.ResultGetValue("path_monitorato"));
                quantita = i;
            }
            //i contiene il numero di risultati;
            if(quantita < 1){
                throw new DatabaseException("Utente non trovato",DatabaseErrorCode.UserNonEsistente);
            }
            else if (quantita > 1) {
                throw new DatabaseException("Panic! More than one user with same password and name or sql wrong", DatabaseErrorCode.UserNonEsistente);
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
        static public bool NomeUtenteValido(string nome_utente)
        {

            Regex r = new Regex("^[a-zA-Z0-9_]+$");
            if (nome_utente.Length < Properties.ApplicationSettings.Default.min_username || 
                nome_utente.Length > Properties.ApplicationSettings.Default.max_username ||
                !r.IsMatch(nome_utente))
            {
                return false;
            }
            return true;
        }

        static public bool PasswordValida(string pass)
        {
            if (pass.Length < Properties.ApplicationSettings.Default.min_password ||
                pass.Length > Properties.ApplicationSettings.Default.max_password)
            {
                return false;
            }
            return true;
        }

        static public bool PathValido(string path)
        {
            if(path.Length > Properties.ApplicationSettings.Default.max_path)
            {
                return false;
            }
            try {
                if (Path.IsPathRooted(path))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        static public User Login(string nome,string password)
        {
            return new User(nome, password);
        }

        /// <summary>
        /// Funzione per registrare un nuovo utente.
        /// </summary>
        /// <param name="nome">Nome utente</param>
        /// <param name="password">Password per l'utente (cleartext)</param>
        /// <param name="path_monitorato">Path Locale dell'utente che verra monitorato</param>
        static public User RegistraUtente(string nome,string password, string path_monitorato)
        {
            if (!NomeUtenteValido(nome))
            {
                throw new DatabaseException("nome_utente non va bene.", DatabaseErrorCode.FormatError);
            }
            if (!PasswordValida(password))
            {
                throw new DatabaseException("password non va bene.", DatabaseErrorCode.FormatError);
            }
            //provo a mettere il nuovo utente: se ricevo un'eccezione particolare il nome utente e duplicato
            DB_Table db = new DB_Table();
            string[][] parameters = new string[3][];
            parameters[0] = new string[2] { "@nome", nome };
            parameters[1] = new string[2] { "@password", password };
            parameters[2] = new string[2] { "@path_monitorato", path_monitorato };
            try{
                db.ExecuteQuery(sql_insert_user, parameters);
            }
            catch (DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.Constraint)
            {
                throw new DatabaseException("L'utente che si cerca di inserire esiste già.",DatabaseErrorCode.UserGiaEsistente);
            }
            
            return new User(nome, password);
        }
    }
}
