using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace ProgettoMalnati
{
    /// <summary>
    /// Classe che gestisce il collegamento di un client, i comandi, e lo scambio dei file.
    /// Contiene il riferimento al thread in cui verranno eseguiti le sue funzioni.
    /// </summary>
    /// <example>
    /// Formato messaggi:
    /// comando\r\n
    /// dati\r\n
    /// ...
    /// \r\n (linea vuota)
    /// </example>
    class Server
    {
        Log l;
        private Thread thread = null;
        private TcpClient s;
        private User user;
        private bool __connected = false;
        private volatile bool __stop = false;

        public Server(TcpClient s)
        {
            l = Log.getLog();
            thread = new Thread(this.servi);
            if (s.Connected)
            {
                this.__connected = true;
            }
            else
            {
                throw new Exception("Socket non connesso.");
            }
            this.s = s;
            
            thread.Start();
        }
        //Proprietà
        /// <summary>
        /// Se falso, l'oggetto più essere distrutto
        /// </summary>
        public bool Connected
        {
            get { return this.__connected; }
        }

        //Metodi
        /// <summary>
        /// Richiede al client di chiudere la connessione e terminare il thread. 
        /// Per eventi eccezionali di errore.
        /// </summary>
        public void RichiediStop()
        {
            __stop = true;
        }
        /// <summary>
        /// Funzione di partenza del thread. Legge messaggi, genera il comando, 
        /// esegue il comando, gestisce eventuali errori.
        /// </summary>
        public void servi()
        {
            string request = "";
            string response;
            Command comando;
            StreamReader reader = new StreamReader(s.GetStream(), Encoding.ASCII);
            StreamWriter writer = new StreamWriter(s.GetStream(), Encoding.ASCII);

            while (this.__connected && !this.__stop)
            {
                try
                {
                    request = reader.ReadLine();
                    List<string> data = new List<string>(Properties.ApplicationSettings.Default.max_num_parametri_protocollo);
                    do
                    {
                        data.Add(reader.ReadLine());
                    } while (data.Last() == null || data.Last().Length > 0);
                    comando = CommandFactory.creaComando(request);
                    response = comando.esegui(this, data);
                    writer.WriteLine(response);
                }
                catch(Exception e)
                {
                    l.log(e.Message);
                }
            }

            if (this.__stop)
            {
                writer.WriteLine(CommandErrorCode.Abort+" Abort");
                s.Close();
            }

            return; //Il thread muore qui...
        }

        //Nested classes
        static class CommandFactory
        {
            public static Command creaComando(string nome_comando)
            {
                Command c = null;
                switch (nome_comando)
                {
                    case "REGISTER":
                        c = new ComandoRegistra();
                        break;
                    case "LOGIN":
                        c = new ComandoLogin();
                        break;
                    case "LISTDIR":
                        c = new ComandoListDir();
                        break;
                }

                return c;
            }
        }

        private abstract class Command
        {
            /// <summary>
            /// Metodo astratto di esecuzione di un comando.
            /// </summary>
            /// <param name="s">L'istanza dell'oggetto server</param>
            /// <param name="dati"></param>
            /// <returns>Il messaggio da restituire al client</returns>
            abstract public string esegui(Server s, List<string> dati);
        }

        private class ComandoRegistra : Command
        {
            /// <summary>
            /// Registra un nuovo utente
            /// </summary>
            /// <param name="s">L'istanza dell'oggetto server</param>
            /// <param name="dati">
            /// <list type="string">
            ///    <item index=0 >Nome utente</item>
            ///    <item index=1 >Password</item>
            ///    <item index=2 >Path monitorato</item>
            /// </list>
            /// </param>
            /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>
            public override string esegui(Server s,List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (s.user == null)
                {
                    return sb.Append(CommandErrorCode.MomentoSbagliato).Append(" Utente già loggato. Impossibile registrarne uno nuovo").ToString();
                }
                if (dati.Count < 3)
                {
                    sb.Append((int)CommandErrorCode.DatiIncompleti).Append(" Dati mancanti per la registrazione");
                }
                else
                {
                    try
                    {
                        s.user = User.RegistraUtente(dati[0], dati[1], dati[2]);
                        sb.Append(CommandErrorCode.OK + " OK");
                    } catch (DatabaseException e)
                    {
                        if(e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
                        {
                            sb.Append(CommandErrorCode.NomeUtenteInUso).Append(" Nome utente già in uso.");
                        }
                        else if (e.ErrorCode == DatabaseErrorCode.FormatError)
                        {
                            sb.Append(CommandErrorCode.FormatoDatiErrato).Append(" Formato dati errato. ");
                        }
                        else
                        {
                            sb.Append(CommandErrorCode.Default).Append(" Errore nella creazione dell'utente.");
                        }
                    }
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// Login utente
        /// </summary>
        /// <param name="s">L'istanza dell'oggetto server</param>
        /// <param name="dati">
        /// <list type="string">
        ///    <item index=0 >Nome utente</item>
        ///    <item index=1 >Password</item>
        /// </list>
        /// </param>
        /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>
        private class ComandoLogin : Command
        {
            public override string esegui(Server s,List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (s.user == null)
                {
                    return sb.Append(CommandErrorCode.MomentoSbagliato).Append(" Utente già loggato.").ToString();
                }
                if (dati.Count < 2)
                {
                    sb.Append((int)CommandErrorCode.DatiIncompleti).Append(" Dati mancanti per il login.");
                }
                else
                {
                    try
                    {
                        s.user = User.Login(dati[0], dati[1]);
                        sb.Append(CommandErrorCode.OK + " OK");
                    }
                    catch (DatabaseException e)
                    {
                        s.l.log(e.Message,Level.ERR);
                        if (e.ErrorCode == DatabaseErrorCode.FormatError)
                        {
                            sb.Append(CommandErrorCode.FormatoDatiErrato).Append(" Formato dati errato.");
                        }
                        else
                        {
                            sb.Append(CommandErrorCode.Default).Append(" Errore nella creazione dell'utente.");
                        }
                    }
                }
                return sb.ToString();
            }
        }
        private class ComandoAggiornaContenutoFile : Command
        {
            public override string esegui(Server s, List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if(s.user == null)
                {
                    return sb.Append(CommandErrorCode.UtenteNonLoggato).Append(" Utente non loggato.").ToString();
                }
                if(dati.Count < )
                throw new NotImplementedException();
            }
        }

        private class ComandoListDir : Command
        {
            public override string esegui(Server s, List<string> dati)
            {
                StringBuilder sb = new StringBuilder();

                return sb.ToString();
            }
        }
    }
}
