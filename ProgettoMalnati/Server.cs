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
                    foreach (string response in comando.esegui(data))
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
            protected User user = null;
            /// <summary>
            /// Metodo astratto di esecuzione di un comando.
            /// </summary>
            /// <param name="dati"></param>
            /// <returns>Un iteratore contenente i messaggi da restituire al client</returns>
            abstract public IEnumerable<string> esegui(List<string> dati);
        }

        private class ComandoRegistra : Command
        {
            /// <summary>
            /// Registra un nuovo utente
            /// </summary>
            /// <param name="dati">
            /// <list type="string">
            ///    <item index=0 >Nome utente</item>
            ///    <item index=1 >Password</item>
            /// </list>
            /// </param>
            /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.MomentoSbagliato).Append(" Utente già loggato. Impossibile registrarne uno nuovo").ToString();
                    yield break;
                }
                if (dati.Count < 2)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti).Append(" Dati mancanti per la registrazione").ToString();
                    yield break;
                }
                else
                {
                    try
                    {
                        user = User.RegistraUtente(dati[0], dati[1]);
                        sb.Append(CommandErrorCode.OK).Append(" OK");
                    } catch (DatabaseException e)
                    {
                        if(e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
                        {
                            sb.Append(CommandErrorCode.NomeUtenteInUso).Append(" Nome utente già in uso. ");
                            sb.Append(e.Message);
                        }
                        else if (e.ErrorCode == DatabaseErrorCode.FormatError)
                        {
                            sb.Append(CommandErrorCode.FormatoDatiErrato).Append(" Formato dati errato. ");
                            sb.Append(e.Message);
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
            /// <param name="dati">
            /// <list type="string">
            ///    <item index=0 >Nome utente</item>
            ///    <item index=1 >Password</item>
            /// </list>
            /// </param>
            /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                Log l = Log.getLog();
                if (user != null)
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
                        user = User.Login(dati[0], dati[1]);
                        sb.Append(CommandErrorCode.OK).Append(" OK");
                    }
                    catch (DatabaseException e)
                    {
                        l.log(e.Message,Level.ERR);
                        if (e.ErrorCode == DatabaseErrorCode.FormatError)
                        {
                            sb.Append(CommandErrorCode.FormatoDatiErrato).Append(" Formato dati errato.");
                        }
                        else if(e.ErrorCode == DatabaseErrorCode.UserNonEsistente)
                        {
                            sb.Append(CommandErrorCode.DatiErrati).Append(" Username o password errati");
                        }else
                        {
                            sb.Append(CommandErrorCode.Default).Append(" Errore nella creazione dell'utente.");
                        }
                    }
                }
                yield return sb.ToString();
            }
        }

        private class ComandoNuovoFile : Command
        {
            /// <summary>
            /// Comando per la creazione di un nuovo file sul server. Se l'utente ha troppi file, il più
            /// vecchio tra quelli eliminati viene distrutto. Se non ci sono file eliminati da distruggere
            /// viene generato un errore.
            /// </summary>
            /// <param name="dati">
            ///     [0]: nome_file
            ///     [1]: path_relativo
            ///     [2]: timestamp creazione (in ticks)
            ///     [3]: sha256 del contenuto
            ///     [4]: dimensione
            /// </param>
            /// <returns></returns>
            public override IEnumerable<string> esegui(List<string> dati)
            {
                Log l = Log.getLog();
                StringBuilder sb = new StringBuilder();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 5)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti)
                                    .Append(" I dati inviati non sono sufficienti").ToString();
                    yield break;
                }
                
                string nome_file = dati[0];
                string path_relativo = dati[1];
                DateTime timestamp = new DateTime();
                int dim = -1;
                sb.Clear();
                try
                {
                    timestamp = new DateTime(Int64.Parse(dati[2]));
                    dim = Int32.Parse(dati[4]);
                }catch(Exception e)
                {
                    sb.Append(CommandErrorCode.FormatoDatiErrato).Append(" Dimensione o timestamp non corretti");
                    l.log("Utente: " + user.Nome + " " + e.Message,Level.INFO);
                }
                // if exception occurred...
                if (dim == -1 || timestamp == DateTime.MinValue)
                {
                    yield return sb.ToString();
                    yield break;
                }
                sb.Clear();
                string sha256 = dati[3];
                FileUtente nuovo = null;

                try
                {
                    nuovo = user.FileList.nuovoFile(nome_file, path_relativo);
                }
                catch (DatabaseException e)
                {
                    switch (e.ErrorCode)
                    {
                        case DatabaseErrorCode.LimiteFileSuperato:
                            sb.Append(CommandErrorCode.LimiteFileSuperato).Append(" L'utente ha superato il limite di file creabili.");
                            break;
                        default:
                            l.log("Server Error!! " + e.Message,Level.ERR);
                            sb.Append(CommandErrorCode.Default).Append(" Errore del server durante la creazione del file.");
                            break;
                    }
                    l.log("Errore nella creazione del file." + e.Message, Level.ERR);
                    throw;
                }
                if(nuovo == null)
                {
                    yield return sb.ToString();
                    yield break;
                }
                Snapshot snap;
                sb.Clear();
                try
                {
                    snap = nuovo.Snapshots.Nuovo(timestamp, dim, sha256);
                }
                catch (Exception e)
                {
                    l.log("Errore... " + e.Message, Level.ERR);
                    sb.Append(CommandErrorCode.Unknown).Append(" Un errore sconosciuto è accaduto nel server.");
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
                catch (Exception e)
                {
                    l.log("Errore nella scrittura del file" + e.Message, Level.ERR);
                    throw;
                }
                yield return CommandErrorCode.OK.ToString() + " Trasferimento completato con successo";
            }
        }

        private class ComandoScaricaFile : Command
        {
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
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                Log l = Log.getLog();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 3)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti)
                                    .Append(" I dati inviati non sono sufficienti").ToString();
                    yield break;
                }
                DateTime timestamp = new DateTime(Int64.Parse(dati[2]));
                string nome_file = dati[0];
                string path_relativo = dati[1];
                Snapshot snap = null;
                try
                {
                    snap = user.FileList[nome_file, path_relativo].Snapshots[timestamp];
                }
                catch (Exception e)
                {
                    l.log("Errore nel selezionare il file corretto. " + e.Message, Level.ERR);
                    throw;
                }
                string token = CollegamentoDati.getNewToken();
                yield return sb.Append(CommandErrorCode.OKIntermedio).Append(" File pronto. Connettiti alla porta corrispondente").ToString();
                yield return token;
                yield return snap.Dim.ToString();
                yield return snap.shaContenuto;
                //Prepararsi all'invio del file...
                NetworkStream ns = CollegamentoDati.getCollegamentoDati(token);
                int buff_size = 1024;
                int letti = 0;
                byte[] buff = new byte[buff_size];
                do
                {
                    letti = snap.leggiBytesDalContenuto(buff, buff_size);
                    ns.Write(buff, 0, letti);
                } while (letti > 0);
                ns.Close();
                sb.Clear();
                yield return sb.Append(CommandErrorCode.OK).Append(" File scritto correttamente").ToString();
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
            public override IEnumerable<string> esegui(List<string> dati)
            {
                Log l = Log.getLog();
                StringBuilder sb = new StringBuilder();
                if(user == null)
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
                try
                {
                    daModificare = user.FileList[dati[0], dati[1]];
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
                    l.log("Errore nella scrittura del file: "+e.Message,Level.ERR);
                    throw;
                }

                yield return CommandErrorCode.OK.ToString()+" Trasferimento completato con successo";
            }
        }

        private class ComandoEliminaFile : Command
        {
            /// <summary>
            /// Setta un file come non valido. Esiste ancora.
            /// </summary>
            /// <param name="s"></param>
            /// <param name="dati">
            ///     [0]: nome file
            ///     [1]: path relativo
            /// </param>
            /// <returns></returns>
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                Log l = Log.getLog();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 2)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti)
                                    .Append(" I dati inviati non sono sufficienti").ToString();
                    yield break;
                }
                try
                {
                    user.FileList[nome_file: dati[0], path_relativo: dati[1]].Valido = false;
                }catch(Exception e)
                {
                    l.log("Errore nel settare il file come non valido; " + e.Message,Level.ERR);
                }
                yield return sb.Append(CommandErrorCode.OK).Append(" File eliminato con successo").ToString();
            }
        }

        private class ComandoListDir : Command
        {
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();

                yield return sb.ToString();
            }
        }

        private class ComandoEsci : Command
        {
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                user.Logout();
                yield return sb.Append(CommandErrorCode.OK).Append(" Connessione terminata con successo").ToString();
            }
        }
    }
}
