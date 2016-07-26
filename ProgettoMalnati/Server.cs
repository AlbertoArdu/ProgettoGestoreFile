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
                    } while (data.Last() != null || data.Last().Length > 0);
                    comando = CommandFactory.creaComando(request);
                    foreach (string response in comando.esegui(this, data))
                    {
                        writer.WriteLine(response);
                    }
                }
                catch(Exception e)
                {
                    l.log(e.Message);
                    throw;
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
                    case "NEWFILE":
                        c = new ComandoNuovoFile();
                        break;
                    case "UPDATE":
                        c = new ComandoAggiornaContenutoFile();
                        break;
                    case "DELETE":
                        c = new ComandoEliminaFile();
                        break;
                    case "RETRIEVE":
                        c = new ComandoScaricaFile();
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
            /// <returns>Un iteratore contenente i messaggi da restituire al client</returns>
            abstract public IEnumerable<string> esegui(Server s, List<string> dati);
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
            public override IEnumerable<string> esegui(Server s,List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (s.user == null)
                {
                    yield return sb.Append(CommandErrorCode.MomentoSbagliato).Append(" Utente già loggato. Impossibile registrarne uno nuovo").ToString();
                    yield break;
                }
                if (dati.Count < 3)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti).Append(" Dati mancanti per la registrazione").ToString();
                    yield break;
                }
                else
                {
                    try
                    {
                        s.user = User.RegistraUtente(dati[0], dati[1], dati[2]);
                        sb.Append(CommandErrorCode.OK).Append(" OK");
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
                yield return sb.ToString();
            }
        }

        private class ComandoLogin : Command
        {
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
            public override IEnumerable<string> esegui(Server s,List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (s.user == null)
                {
                    yield return sb.Append(CommandErrorCode.MomentoSbagliato).Append(" Utente già loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 2)
                {
                    yield return sb.Append((int)CommandErrorCode.DatiIncompleti)
                                    .Append(" Dati mancanti per il login.").ToString();
                    yield break;
                }
                else
                {
                    try
                    {
                        s.user = User.Login(dati[0], dati[1]);
                        sb.Append(CommandErrorCode.OK).Append(" OK");
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
                yield return sb.ToString();
            }
        }

        private class ComandoAggiornaContenutoFile : Command
        {
            /// <summary>
            /// Comando che gestisce l'aggiornamento del contenuto di un file
            /// </summary>
            /// <param name="s"></param>
            /// <param name="dati">
            ///  [0]: nome del file
            ///  [1]: path relativo del file
            ///  [2]: timestamp di modifica (in "ticks")
            ///  [3]: sha256 del file in codifica base64
            ///  [4]: dimensione del file in formato testuale
            /// </param>
            /// <returns>
            /// Ritorna prima una stringa con il codice "OKIntermedio" seguita dal token assegnato al client.
            /// Quando il client si connette alla porta dati viene ricevuto il nuovo contenuto del file e in 
            /// caso di successo viene ancora inviato un messaggio di ok.
            /// </returns>
            public override IEnumerable<string> esegui(Server s, List<string> dati)
            {
                Log l = Log.getLog();
                StringBuilder sb = new StringBuilder();
                if(s.user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if(dati.Count < 5)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti)
                                    .Append(" I dati inviati non sono sufficienti").ToString();
                    yield break;
                }

                FileUtente daModificare;
                Snapshot snap;
                string nome_file = dati[0];
                string path_relativo = dati[1];
                DateTime timestamp = new DateTime(Int64.Parse(dati[2]));
                string sha256 = dati[3];
                int dim = Int32.Parse(dati[4]);
                TcpClient conn_dati;
                try
                {
                    daModificare = s.user.FileList[dati[0], dati[1]];
                    snap = daModificare.Snapshots.Nuovo(timestamp, dim, sha256);
                }
                catch (Exception e)
                {
                    l.log("Errore... "+e.Message,Level.ERR);
                    throw;
                }

                string token = CollegamentoDati.getNewToken();
                yield return sb.Append(CommandErrorCode.OKIntermedio).Append(" Stream dati pronto").ToString();
                yield return token;
                NetworkStream stream_dati = CollegamentoDati.getCollegamentoDati(token);
                byte[] buffer = new byte[1024];
                int letti = 0;
                try
                {
                    do
                    {
                        letti = stream_dati.Read(buffer, 0, 1024);
                        snap.scriviBytes(buffer, letti);
                    } while (letti == 1024);
                    snap.completaScrittura();
                }
                catch( Exception e)
                {
                    l.log("Errore nella scrittura del file",Level.ERR);
                    throw;
                }

                yield return CommandErrorCode.OK.ToString()+" Trasferimento completato con successo";
            }
        }
        private class ComandoNuovoFile : Command
        {
            public override IEnumerable<string> esegui(Server s, List<string> dati)
            {
                StringBuilder sb = new StringBuilder();

                yield return "";
            }
        }
        private class ComandoEsci : Command
        {
            public override IEnumerable<string> esegui(Server s, List<string> dati)
            {
                StringBuilder sb = new StringBuilder();

                throw new NotImplementedException();
                yield return "";
            }
        }
        private class ComandoEliminaFile : Command
        {
            public override IEnumerable<string> esegui(Server s, List<string> dati)
            {

                throw new NotImplementedException();
                yield return "";
            }
        }
        private class ComandoScaricaFile : Command
        {
            public override IEnumerable<string> esegui(Server s, List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                throw new NotImplementedException();
                yield return "";
            }
        }

        private class ComandoListDir : Command
        {
            public override IEnumerable<string> esegui(Server s, List<string> dati)
            {
                StringBuilder sb = new StringBuilder();

                yield return sb.ToString();
            }
        }
    }
}
