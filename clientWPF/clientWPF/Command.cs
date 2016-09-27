using System;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading;

namespace clientWPF
{
    /// <summary>
    /// Classe che gestisce il collegamento di un client, i comandi, e lo scambio dei file.
    /// </summary>
    /// <example>
    /// Formato messaggi client:
    /// comando\r\n
    /// dati\r\n
    /// ...
    /// \r\n (linea vuota)
    /// Formato risposte server:
    /// CommandErrorCode Messaggio\r\n
    /// dati\r\n
    /// ...
    /// \r\n
    /// </example>
    abstract class Command
    {
        private IPAddress server_addr;
        private int server_port;
        protected Log l;
        static protected TcpClient s;
        static protected bool __logged = false;
        static protected bool __connected = false;
        static protected string base_path = Properties.Settings.Default.base_path;
        protected StreamReader control_stream_reader = null;
        protected StreamWriter control_stream_writer = null;
        protected NetworkStream data_stream = null;
        protected string error_message;
        protected ServerErrorCode error_code;
        static protected object sharedLock = null;

        public Command()
        {
            l = Log.getLog();
            if (s != null && s.Connected && sharedLock != null){ __connected = true; }
            else
            {
                __logged = false;
                server_addr = IPAddress.Parse(Properties.Settings.Default.ip_address);
                server_port = Properties.Settings.Default.port;
                s = new TcpClient();
                try
                {
                    s.Connect(server_addr, server_port);
                    __connected = true;
                    l.log("Connected!");
                    if (!s.Connected)
                    {
                        l.log("Errore di connessione", Level.ERR);
                        throw new ServerException("Errore di connessione", ServerErrorCode.ServerNonDisponibile);
                    }

                }
                catch (Exception e)
                {
                    l.log(e.Message, Level.ERR);
                    throw new ServerException("Errore di connessione: " + e.Message, ServerErrorCode.ServerNonDisponibile);
                }
                sharedLock = new object();
            }
            Monitor.Enter(sharedLock);
            try
            {
                control_stream_reader = new StreamReader(s.GetStream(), Encoding.ASCII);
                control_stream_writer = new StreamWriter(s.GetStream(), Encoding.ASCII);
            }
            catch(Exception e)
            {
                l.log("Error in taking the streams: "+e.Message);
                throw new ClientException("Errore di connessione: " + e.Message, ClientErrorCode.ServerNonDisponibile);
            }
        }
        ~Command()
        {
            if(Monitor.IsEntered(sharedLock))
                Monitor.Exit(sharedLock);
        }
        //Proprietà
        /// <summary>
        /// Se falso, l'oggetto più essere distrutto
        /// </summary>
        static public bool Connected
        {
            get { return s.Connected; }
            set
            {
                if(value == false)
                {
                    __connected = false;
                    __logged = false;
                    s.Close();
                }
            }
        }
        public string ErrorMessage => error_message;
        public ServerErrorCode ErrorCode => error_code;
        static public bool Logged { get { return __logged; } }
        abstract public bool esegui();

    protected void sendData(string s)
    {
            try
            {
                control_stream_writer.Write(s);
                control_stream_writer.Flush();
            }
            catch (IOException e)
            {
                {
                    l.log("Errore di connessione", Level.ERR);
                    throw new ServerException("Errore di connessione", ServerErrorCode.ServerNonDisponibile);
                }
            }
    }

    protected IEnumerable<string> getResponses()
        {
            bool completata = false;
            if(!Connected)
            {
                error_message = Properties.Messaggi.erroreConnessioneServer;
                error_code = ServerErrorCode.ConnessioneInterrotta;
                yield return null;
                yield break;
            }
            while (!completata) {
                //Prima linea con codice di errore
                string response = control_stream_reader.ReadLine();
                if (response == null)
                {
                    error_message = Properties.Messaggi.erroreConnessioneServer;
                    error_code = ServerErrorCode.ConnessioneInterrotta;
                    control_stream_reader.Close();
                    Connected = false;
                    yield return null;
                    yield break;
                }
                response = response.Trim();
                CommandErrorCode errorCode = (CommandErrorCode)Int32.Parse(response.Split(' ')[0]); //Extract code from response
                switch (errorCode)
                {
                    case CommandErrorCode.OK:
                        completata = true;
                        do
                        {
                            response = control_stream_reader.ReadLine();
                            yield return response;
                        } while (response != null && response.Length > 0);
                        if (response == null)
                        {
                            error_message = Properties.Messaggi.erroreConnessioneServer;
                            error_code = ServerErrorCode.ConnessioneInterrotta;
                            control_stream_reader.Close();
                            Connected = false;
                            yield break;
                        }
                        break;
                    case CommandErrorCode.OKIntermedio:
                        do
                        {
                            response = control_stream_reader.ReadLine();
                            yield return response;
                        } while (response != null && response.Length > 0);
                        if (response == null)
                        {
                            error_message = Properties.Messaggi.erroreConnessioneServer;
                            error_code = ServerErrorCode.ConnessioneInterrotta;
                            control_stream_reader.Close();
                            Connected = false;
                            yield break;
                        }
                        break;
                    case CommandErrorCode.FormatoDatiErrato:
                        error_message = Properties.Messaggi.formatoDatiErrato;
                        error_code = ServerErrorCode.FormatoDatiErrato;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    case CommandErrorCode.UtenteNonLoggato:
                        error_message = Properties.Messaggi.nonLoggato;
                        error_code = ServerErrorCode.UtenteNonLoggato;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    case CommandErrorCode.FileEsistente:
                        error_message = Properties.Messaggi.fileEsistente;
                        error_code = ServerErrorCode.FileEsistente;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    case CommandErrorCode.LimiteFileSuperato:
                        error_message = Properties.Messaggi.limiteFileSuperato;
                        error_code = ServerErrorCode.LimiteFileSuperato;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    case CommandErrorCode.DatiIncompleti:
                        error_message = Properties.Messaggi.datiInconsistenti;
                        error_code = ServerErrorCode.DatiIncompleti;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    case CommandErrorCode.DatiErrati:
                        error_message = Properties.Messaggi.datiErrati;
                        error_code = ServerErrorCode.DatiErrati;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    case CommandErrorCode.MomentoSbagliato:
                        error_message = Properties.Messaggi.momentoSbagliato;
                        error_code = ServerErrorCode.MomentoSbagliato;
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        yield return null;
                        yield break;
                    default:
                        do { response = control_stream_reader.ReadLine(); }
                        while (response != null && response.Length > 0);
                        throw new ServerException(Properties.Messaggi.erroreServer, ServerErrorCode.Default);
                }
            }
        }
    }

    class ComandoRegistra : Command
    {
        string nome_utente, password;
        const string nome_comando = "REGISTER";

        public ComandoRegistra(string nome_utente, string password):
            base()
        {
            this.nome_utente = nome_utente;
            this.password = password;
        }
        /// <summary>
        /// Registra un nuovo utente
        /// </summary>
        /// <exception>CommandExeption con un codice corrispondente all'errore riscontrato</exception>
        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(nome_utente).Append(Environment.NewLine).
                Append(password).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            l.log("Data has been sent");
            string response = null;
            var respEnumerator = this.getResponses().GetEnumerator();
            if (!respEnumerator.MoveNext())
            {
                Monitor.Exit(sharedLock);
                return false;
            }

            response = respEnumerator.Current;
                Monitor.Exit(sharedLock);
            if (response == null)
            { 
                return false;
            }
            __logged = true;
            return true;
        }
    }

    class ComandoLogin : Command
    {
        string nome_utente, password;
        const string nome_comando = "LOGIN";

        public ComandoLogin(string nome_utente, string password):
            base()
        {
            this.nome_utente = nome_utente;
            this.password = password;
        }
        /// <summary>
        /// Identifica un utente con il server. E' necessario che sia il primo comando ogni nuova connessione
        /// </summary>
        /// <exception>CommandExeption con un codice corrispondente all'errore riscontrato</exception>
        public override bool esegui()
        {
            if (Logged)
            {
                Monitor.Exit(sharedLock);
                return true;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(nome_utente).Append(Environment.NewLine).
                Append(password).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            string response = null;
            var respEnumerator = this.getResponses().GetEnumerator();
            if (!respEnumerator.MoveNext())
            {
                Monitor.Exit(sharedLock);
                return false;
            }
                
            response = respEnumerator.Current;
            respEnumerator.MoveNext();
            Monitor.Exit(sharedLock);
            if (response == null)
            {
                if (this.error_code == ServerErrorCode.MomentoSbagliato)
                    return true;
                return false;
            }
            __logged = true;
            return true;
        }
    }

    class ComandoNuovoFile : Command
    {
        string path;
        string nome_file;
        string path_completo;
        int dim=0;
        string sha_contenuto = "";
        DateTime t_creazione = DateTime.MinValue;
        DateTime t_modifica = DateTime.MinValue;
        FileStream file = null;
        const string nome_comando = "NEWFILE";

        public ComandoNuovoFile(string nome_file, string path, int dim = -1, 
                                DateTime t_creazione = new DateTime(), 
                                DateTime t_modifica = new DateTime(),
                                string sha_contenuto = null
                                )
            : base()
        {
            FileInfo finfo = null;
            Log l = Log.getLog();
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            this.path = path;
            this.nome_file = nome_file;
            this.path_completo = new StringBuilder(base_path).Append(Path.DirectorySeparatorChar).Append(path).Append(Path.DirectorySeparatorChar).Append(nome_file).ToString();

            try
            {
                finfo = new FileInfo(path_completo);
                if (!finfo.Exists)
                    throw new Exception("Il file da inviare non esiste.");
            }
            catch(Exception e)
            {
                    throw new Exception("Errore nel leggere i parametri del file. Forse i parametri sono sbagliati. "+e.Message);
            }
            if (dim < 0)
                this.dim = (int)(finfo.Length);
            else
                this.dim = dim;
            if (t_creazione == DateTime.MinValue)
                this.t_creazione = finfo.CreationTime;
            else
                this.t_creazione = t_creazione;
            if (t_modifica == DateTime.MinValue)
                this.t_modifica = finfo.LastWriteTime;
            else
                this.t_modifica = t_modifica;

            file = File.Open(this.path_completo,FileMode.Open);
            if (sha_contenuto == null)
            {
                SHA256 sha_obj = SHA256Managed.Create();
                byte[] hash_val;
                hash_val = sha_obj.ComputeHash(this.file);
                StringBuilder hex = new StringBuilder(hash_val.Length * 2);
                foreach (byte b in hash_val)
                    hex.AppendFormat("{0:x2}", b);
                this.sha_contenuto = hex.ToString();
            }
            else
                this.sha_contenuto = sha_contenuto;

            this.file.Position = 0;
        }
        /// <summary>
        /// Comando per la creazione di un nuovo file sul server. Se l'utente ha troppi file, il più
        /// vecchio tra quelli eliminati viene distrutto. Se non ci sono file eliminati da distruggere
        /// viene generato un errore.
        /// </summary>
        /// <exception>CommandExeption con un codice corrispondente all'errore riscontrato</exception>
        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(nome_file).Append(Environment.NewLine).
                Append(path).Append(Environment.NewLine).
                Append(t_creazione.Ticks).Append(Environment.NewLine).
                Append(t_modifica.Ticks).Append(Environment.NewLine).
                Append(sha_contenuto).Append(Environment.NewLine).
                Append(dim).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            //Get answer
            var respEnumerator = getResponses().GetEnumerator();
            respEnumerator.MoveNext();
            string response = respEnumerator.Current;
            respEnumerator.MoveNext();
            if (response == null)
            {
                Monitor.Exit(sharedLock);
                return false;
            }
            try
            { 
                data_stream = CollegamentoDati.getCollegamentoDati(response);
            }
            catch
           {
                Monitor.Exit(sharedLock);
                error_message = Properties.Messaggi.collegamentoDati;
                error_code = ServerErrorCode.CollegamentoDatiNonDisponibile;
                return false;
            }
            byte[] buffer = new byte[1024];
            int size = 1024;
            try
            {
                while ((size = file.Read(buffer, 0, size)) != 0)
                {
                    data_stream.Write(buffer, 0, size);
                    data_stream.Flush();
                }
                data_stream.Close();
                file.Close();
            }
            catch
            {
                Monitor.Exit(sharedLock);
                Connected = false;
                error_message = Properties.Messaggi.erroreConnessioneServer;
                error_code = ServerErrorCode.ConnessioneInterrotta;
                return false;
            }
            respEnumerator.MoveNext();
            Monitor.Exit(sharedLock);
            if (respEnumerator.Current == null)
            {
                error_message = Properties.Messaggi.erroreConnessioneServer;
                error_code = ServerErrorCode.ConnessioneInterrotta;
                return false;
            }
            return true;
        }
    }

    class ComandoScaricaFile : Command
    {
        string path;
        string nome_file;
        string path_completo;
        string tmp_path;
        int dim;
        string sha_contenuto;
        DateTime t_creazione;
        FileStream tmp_file;
        const string nome_comando = "RETRIEVE";

        public int Dim => dim;
        public string SHAContenuto => sha_contenuto;
        public ComandoScaricaFile(string nome_file, string path, DateTime timestamp): base()
        {
            Log l = Log.getLog();
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            this.path = path;
            this.nome_file = nome_file;
            this.path_completo = new StringBuilder(base_path).Append(Path.DirectorySeparatorChar).Append(path).Append(Path.DirectorySeparatorChar).Append(nome_file).ToString();
            tmp_path = Path.GetTempFileName();
            tmp_file = File.Open(this.tmp_path, FileMode.Open);
            this.t_creazione = timestamp;
        }

        /// <summary>
        /// Carica la versione richiesta del file all'utente che lo richiede.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="dati">
        ///     [0]: nome_file
        ///     [1]: path relativo
        ///     [2]: timestamp versione (in Ticks)
        /// </param>
        /// <returns>
        /// OK intermedio
        /// token
        /// dimensione file
        /// sha contenuto
        /// </returns>
        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(nome_file).Append(Environment.NewLine).
                Append(path).Append(Environment.NewLine).
                Append(t_creazione.Ticks).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            string response = null;
            var respEnumerator = getResponses().GetEnumerator();
            respEnumerator.MoveNext();
            response = respEnumerator.Current;
            if (response == null)
            {
                Monitor.Exit(sharedLock);
                return false;
            }
            try
            { 
                string token = response;
                if(!respEnumerator.MoveNext() || respEnumerator.Current == null)
                {
                    Monitor.Exit(sharedLock);
                    return false;
                }
                this.dim = Int32.Parse(respEnumerator.Current);
                if (!respEnumerator.MoveNext() || respEnumerator.Current == null)
                {
                    Monitor.Exit(sharedLock);
                    return false;
                }
                this.sha_contenuto = respEnumerator.Current;
                data_stream = CollegamentoDati.getCollegamentoDati(token);
                respEnumerator.MoveNext();
            }
            catch
            {
                Monitor.Exit(sharedLock);
                error_message = Properties.Messaggi.collegamentoDati;
                error_code = ServerErrorCode.CollegamentoDatiNonDisponibile;
                Connected = false;
                return false;
            }
            byte[] buffer = new byte[1024];
            int size = 1024;
            int tot_read = 0;
            try
            {
                while ((size = data_stream.Read(buffer, 0, size)) != 0)
                {
                    //Scrivo su un file temporaneo. Se tutto va bene sostituisco quello presente
                    //nella cartella dell'utente
                    tot_read += size;
                    tmp_file.Write(buffer, 0, size);
                    tmp_file.Flush();
                }
            }
            catch
            {
                Monitor.Exit(sharedLock);
                error_message = Properties.Messaggi.erroreConnessioneServer;
                error_code = ServerErrorCode.ConnessioneInterrotta;
                Connected = false;
                return false;
            }
            finally
            {
                tmp_file.Close();
                data_stream.Close();
            }
            
            if (tot_read != this.dim)
            {
                error_message = Properties.Messaggi.datiInconsistenti;
                error_code = ServerErrorCode.DatiInconsistenti;
                Monitor.Exit(sharedLock);
                return false;
            }
            SHA256 sha_obj = SHA256Managed.Create();
            byte[] hash_val;
            tmp_file = File.Open(tmp_path, FileMode.Open);
            hash_val = sha_obj.ComputeHash(tmp_file);
            tmp_file.Close();
            StringBuilder hex = new StringBuilder(hash_val.Length * 2);
            foreach (byte b in hash_val)
                hex.AppendFormat("{0:x2}", b);
            string sha_reale = hex.ToString();
            if(sha_reale != sha_contenuto.Trim())
            {
                error_message = Properties.Messaggi.datiInconsistenti;
                error_code = ServerErrorCode.DatiInconsistenti;
                Monitor.Exit(sharedLock);
                return false;
            }

            if (!respEnumerator.MoveNext() || respEnumerator.Current == null)
            {
                Monitor.Exit(sharedLock);
                return false;
            }

            while (respEnumerator.MoveNext());
            Monitor.Exit(sharedLock);
            FileUtenteList list = FileUtenteList.getInstance();
            //vado a vedere il flag di validità sul db
            //se è TRUE -> è una retrive di un file esistente
            //se è FALSE -> è una retrive di un file cancellato

            if (list.getValidity(this.nome_file, this.path) == true)
                list[this.nome_file, this.path].aggiornaDatiPrec(this.dim, this.t_creazione, this.SHAContenuto);
            else {
                FileUtente[] deleted = list.Deleted;
                foreach(FileUtente fu in deleted)
                {
                    if(fu.Nome == this.nome_file && fu.Path ==this.path)
                    {
                        list.Ripristina(fu.Id);
                        fu.aggiornaDatiPrec(this.dim, this.t_creazione, this.SHAContenuto);
                        break;
                    }
                }
            }
            try
            {
                // Ensure that the target does not exist.
                if (File.Exists(path_completo))
                    File.Delete(path_completo);
                // Move the file.
                File.Move(tmp_path, path_completo);
            }
            catch (Exception e)
            {
                error_message = "Errore nel sostituire la versione precedente. " + e.Message;
                error_code = ServerErrorCode.Unknown;
                return false;
            }
            return true;
        }
    }

    class ComandoAggiornaContenutoFile : Command
    {
        string path;
        string nome_file;
        string path_completo;
        int dim;
        string sha_contenuto;
        DateTime t_modifica;
        FileStream file = null;
        const string nome_comando = "UPDATE";

        public ComandoAggiornaContenutoFile(string nome_file, string path, int dim = -1,
                                DateTime t_modifica = new DateTime(), string sha_contenuto = null
                                )
            : base()
        {
            FileInfo finfo = null;
            Log l = Log.getLog();
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            this.path = path;
            this.nome_file = nome_file;
            this.path_completo = new StringBuilder(base_path).Append(Path.DirectorySeparatorChar).Append(path).Append(Path.DirectorySeparatorChar).Append(nome_file).ToString();

            try
            {
                finfo = new FileInfo(path_completo);
                if (!finfo.Exists)
                    throw new ClientException("Il file da inviare non esiste.");
            }
            catch (Exception e)
            {
                throw new ClientException("Errore nel leggere i parametri del file. Forse i parametri sono sbagliati. " + e.Message);
            }
            if (dim < 0)
                this.dim = (int)(finfo.Length);
            else
                this.dim = dim;
            if (t_modifica == DateTime.MinValue)
                this.t_modifica = finfo.LastWriteTime;
            else
                this.t_modifica = t_modifica;
            file = File.Open(this.path_completo, FileMode.Open);
            if (sha_contenuto == null)
            {
                SHA256 sha_obj = SHA256Managed.Create();
                byte[] hash_val;
                hash_val = sha_obj.ComputeHash(this.file);
                this.sha_contenuto = System.Convert.ToBase64String(hash_val);
            }
            else
            {
                this.sha_contenuto = sha_contenuto;
            }
            this.file.Position = 0;
        }
        /// <summary>
        /// Comando usato per aggiornare il contenuto di un file sul server.
        /// </summary>
        /// <exception>ServerExeption con un codice corrispondente all'errore riscontrato</exception>
        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(nome_file).Append(Environment.NewLine).
                Append(path).Append(Environment.NewLine).
                Append(t_modifica.Ticks).Append(Environment.NewLine).
                Append(sha_contenuto).Append(Environment.NewLine).
                Append(dim).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            string response = null;
            var respEnumerator = getResponses().GetEnumerator();
            if (!respEnumerator.MoveNext() || respEnumerator.Current == null)
            {
                Monitor.Exit(sharedLock);
                return false;
            }
            response = respEnumerator.Current;
            respEnumerator.MoveNext();
            try { 
                data_stream = CollegamentoDati.getCollegamentoDati(response);
            }
            catch
            {
                error_message = Properties.Messaggi.collegamentoDati;
                error_code = ServerErrorCode.CollegamentoDatiNonDisponibile;
                Connected = false;
                Monitor.Exit(sharedLock);
                return false;
            }

            byte[] buffer = new byte[1024];
            int size = 1024;
            try
            {
                while ((size = file.Read(buffer, 0, size)) != 0)
                {
                    data_stream.Write(buffer, 0, size);
                    data_stream.Flush();
                }
                data_stream.Close();
                file.Close();
            }
            catch
            {
                error_message = Properties.Messaggi.erroreConnessioneServer;
                error_code = ServerErrorCode.ConnessioneInterrotta;
                Monitor.Exit(sharedLock);
                return false;
            }
            //Leggo la risposta (se tutto è andato bene o c'è stato un errore)
            Monitor.Exit(sharedLock);
            if (!respEnumerator.MoveNext())
                return false;
            response = respEnumerator.Current;
            if (response == null)
                return false;
            return true;
        }
        ~ComandoAggiornaContenutoFile()
        {
            data_stream.Close();
            file.Close();
        }
    }

    class ComandoEliminaFile : Command
    {
        string path;
        string nome_file;
        string path_completo;
        const string nome_comando = "DELETE";

        public ComandoEliminaFile(string nome_file, string path)
            : base()
        {
            Log l = Log.getLog();
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            this.path = path;
            this.nome_file = nome_file;
            this.path_completo = new StringBuilder(base_path).Append(Path.DirectorySeparatorChar).Append(path).Append(Path.DirectorySeparatorChar).Append(nome_file).ToString();
        }
        /// <summary>
        /// Setta un file come non valido. Esiste ancora.
        /// </summary>
        /// <returns></returns>
        public override bool esegui()
        {
            Log l = Log.getLog();
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(nome_file).Append(Environment.NewLine).
                Append(path).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            control_stream_writer.Write(sb.ToString());
            control_stream_writer.Flush();
            string response = null;
            var respEnumerator = this.getResponses().GetEnumerator();
            Monitor.Exit(sharedLock);
            if (!respEnumerator.MoveNext())
                return false;
            response = respEnumerator.Current;
            if (response == null)
                return false;
            respEnumerator.MoveNext();
            return true;
        }
    }

    class ComandoListFolders : Command
    {
        System.Collections.Generic.List<string> __paths = null;
        const string nome_comando = "LISTPATHS";

        public string[] Paths => __paths.ToArray();
        public ComandoListFolders() : base()
        {
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            __paths = new List<string>();
        }

        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder().Append(nome_comando).Append(Environment.NewLine)
                .Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            string response = null;
            var respEnumerator = this.getResponses().GetEnumerator();
            while (respEnumerator.MoveNext())
            {
                response = respEnumerator.Current;
                if (response == null)
                {
                    Monitor.Exit(sharedLock);
                    return false;
                }
                if(response.Length > 0)
                    __paths.Add(response);
            }
            Monitor.Exit(sharedLock);
            return true;
        }
    }

    class ComandoListDir : Command
    {
        System.Collections.Generic.List<string> __files;
        string path;
        const string nome_comando = "LISTDIR";
        
        public string[] FileNames => this.__files.ToArray();

        public ComandoListDir(string path) : base()
        {
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            this.path = path;
            __files = new System.Collections.Generic.List<string>();
        }
        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder().Append(nome_comando).Append(Environment.NewLine)
                .Append(path).Append(Environment.NewLine)
                .Append(Environment.NewLine);

            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            string response = null;
            var respEnumerator = this.getResponses().GetEnumerator();
            while (respEnumerator.MoveNext())
            {
                response = respEnumerator.Current;
                if (response == null)
                {
                    Monitor.Exit(sharedLock);
                    return false;
                }
                if(response.Length > 0)
                    __files.Add(response);
            }
            Monitor.Exit(sharedLock);
            return true;
        }
    }

    class ComandoListVersions : Command
    {
        System.Collections.Generic.List<DateTime> __versions;
        string path;
        string nome_file;
        const string nome_comando = "LISTVERSIONS";

        public DateTime[] Versions => __versions.ToArray();

        public ComandoListVersions(string nome_file, string path) : base()
        {
            if (!Logged)
                throw new ServerException(Properties.Messaggi.nonLoggato, ServerErrorCode.UtenteNonLoggato);
            this.path = path;
            this.nome_file = nome_file;
            __versions = new System.Collections.Generic.List<DateTime>();
        }

        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder().Append(nome_comando).Append(Environment.NewLine)
                .Append(nome_file).Append(Environment.NewLine)
                .Append(path).Append(Environment.NewLine)
                .Append(Environment.NewLine);

            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            string response = null;
            var respEnumerator = this.getResponses().GetEnumerator();
            while (respEnumerator.MoveNext())
            {
                response = respEnumerator.Current;
                if (response == null)
                {
                    Monitor.Exit(sharedLock);
                    return false;
                }
                if(response.Length > 0)
                    __versions.Add(new DateTime(Int64.Parse(response.Trim())));
            }
            Monitor.Exit(sharedLock);
            return true;
        }
    }

    class ComandoEsci : Command
    {
        const string nome_comando = "EXIT"; 
        public override bool esegui()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(nome_comando).Append(Environment.NewLine).
                Append(Environment.NewLine);
            sendData(sb.ToString());
            //control_stream_writer.Write(sb.ToString());
            //control_stream_writer.Flush();
            this.control_stream_reader.Close();
            this.control_stream_writer.Close();
            Monitor.Exit(sharedLock);
            return true;
        }
    }

    class ComandoCaricaHashDB : Command
    {
        private string hash;
        const string nome_comando = "UPLOAD_HASH";
        public ComandoCaricaHashDB(string aHash)
        {
            hash = aHash;
        }

        public override bool esegui()
        {
            
            return false;
        }
    }

    class ComandoScaricaHashDB : Command
    {
        public string hash = null;
        const string nome_comando = "DOWNLOAD_HASH";
        public override bool esegui()
        {
            return false;
        }
    }
}
