﻿using System;
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
        static protected List<string> users_logged = new List<string>();

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
            User user = null;
            StreamReader reader = new StreamReader(s.GetStream(), Encoding.ASCII);
            StreamWriter writer = new StreamWriter(s.GetStream(), Encoding.ASCII);
            string[] invalid_command = { "999 Invalid Command", "" };
            while (this.__connected && !this.__stop)
            {
                try
                {
                    request = reader.ReadLine();
                    if (request == null) { break; }
                    List<string> data = new List<string>(Properties.ApplicationSettings.Default.max_num_parametri_protocollo);
                    do
                    {
                        data.Add(reader.ReadLine());
                    } while (data.Last() != null && data.Last().Length > 0);
                    l.log(new StringBuilder("Linee lette: ").Append(data.Count).ToString());
                    comando = CommandFactory.creaComando(request);
                    
                    if(comando == null)
                    {
                        //Comando non valido
                        foreach (string response in invalid_command)
                        {
                            writer.WriteLine(response);
                            writer.Flush();
                        }
                    }
                    else
                    {
                        comando.user = user;
                        foreach (string response in comando.esegui(data))
                        {
                            writer.WriteLine(response);
                            writer.Flush();
                        }
                        writer.WriteLine();
                        writer.Flush();
                    }
                    user = comando.user;
                }
                catch(Exception e)
                {
                    l.log(e.Message);
                    user = null;
                    this.__stop = true;
                }

                if (this.__stop)
                {
                    try
                    {
                        writer.WriteLine(CommandErrorCode.Abort + " Abort");
                        s.Close();
                    }
                    catch{;}
                    try { users_logged.Remove(user.Nome); } catch {; }
                }
            }
            return; //Il thread muore qui...
        }

        //Nested classes
        static class CommandFactory
        {
            public static Command creaComando(string nome_comando)
            {
                nome_comando = nome_comando.Trim();
                Log.getLog().log(nome_comando.Length.ToString());
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
                    case "LISTPATHS":
                        c = new ComandoListFolders();
                        break;
                    case "LISTDIR":
                        c = new ComandoListDir();
                        break;
                    case "LISTVERSIONS":
                        c = new ComandoListVersions();
                        break;
                    case "EXIT":
                        c = new ComandoEsci();
                        break;
                    default:
                        c = new ComandoDefault();
                        break;
                }
                return c;
            }
        }

        private abstract class Command
        {
            public User user = null;
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
                if (user != null)
                {
                    yield return sb.Append(CommandErrorCode.MomentoSbagliato.ToString("D")).Append(" Utente già loggato. Impossibile registrarne uno nuovo").ToString();
                    yield break;
                }
                if (dati.Count < 2)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D")).Append(" Dati mancanti per la registrazione").ToString();
                    yield break;
                }
                else
                {
                    try
                    {
                        user = User.RegistraUtente(dati[0], dati[1]);
                        sb.Append(CommandErrorCode.OK.ToString("D")).Append(" OK");
                    } catch (DatabaseException e)
                    {
                        if(e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
                        {
                            sb.Append(CommandErrorCode.NomeUtenteInUso.ToString("D")).Append(" Nome utente già in uso. ");
                            sb.Append(e.Message);
                        }
                        else if (e.ErrorCode == DatabaseErrorCode.FormatError)
                        {
                            sb.Append(CommandErrorCode.FormatoDatiErrato.ToString("D")).Append(" Formato dati errato. ");
                            sb.Append(e.Message);
                        }
                        else
                        {
                            sb.Append(CommandErrorCode.Default.ToString("D")).Append(" Errore nella creazione dell'utente.");
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
                    yield return sb.Append(CommandErrorCode.MomentoSbagliato.ToString("D")).Append(" Utente già loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 2)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D"))
                                    .Append(" Dati mancanti per il login.").ToString();
                    yield break;
                }
                else
                {
                    if(Server.users_logged.Contains(dati[0]))
                    {
                        yield return sb.Append(CommandErrorCode.MomentoSbagliato.ToString("D")).Append(" Utente già loggato.").ToString();
                        yield break;
                    }
                    try
                    {
                        user = User.Login(dati[0], dati[1]);
                        Server.users_logged.Add(dati[0]);
                        sb.Append(CommandErrorCode.OK.ToString("D")).Append(" OK");
                    }
                    catch (DatabaseException e)
                    {
                        l.log(e.Message,Level.ERR);
                        if (e.ErrorCode == DatabaseErrorCode.FormatError)
                        {
                            sb.Append(CommandErrorCode.FormatoDatiErrato.ToString("D")).Append(" Formato dati errato.");
                        }
                        else if(e.ErrorCode == DatabaseErrorCode.UserNonEsistente)
                        {
                            sb.Append(CommandErrorCode.DatiErrati.ToString("D")).Append(" Username o password errati");
                        }else
                        {
                            sb.Append(CommandErrorCode.Default.ToString("D")).Append(" Errore nella creazione dell'utente.");
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
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 7)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D"))
                                    .Append(" I dati inviati non sono sufficienti").ToString();
                    yield break;
                }

                string nome_file = dati[0];
                string path_relativo = dati[1];
                DateTime timestamp = new DateTime();
                DateTime t_modifica = new DateTime();
                int dim = -1;
                sb.Clear();
                try
                {
                    timestamp = new DateTime(Int64.Parse(dati[2]));
                    t_modifica = new DateTime(Int64.Parse(dati[3]));
                    dim = Int32.Parse(dati[5]);
                }catch(Exception e)
                {
                    sb.Append(CommandErrorCode.FormatoDatiErrato.ToString("D")).Append(" Dimensione o timestamp non corretti");
                    l.log("Utente: " + user.Nome + " " + e.Message,Level.INFO);
                }
                // if exception occurred...
                if (dim == -1 || timestamp == DateTime.MinValue || t_modifica == DateTime.MinValue)
                {
                    yield return sb.ToString();
                    yield break;
                }
                sb.Clear();
                string sha256 = dati[4];
                FileUtente nuovo = null;

                try
                {
                    nuovo = user.FileList.nuovoFile(nome_file, path_relativo,timestamp);
                }
                catch (DatabaseException e)
                {
                    switch (e.ErrorCode)
                    {
                        case DatabaseErrorCode.LimiteFileSuperato:
                            sb.Append(CommandErrorCode.LimiteFileSuperato.ToString("D")).Append(" L'utente ha superato il limite di file creabili.");
                            break;
                        case DatabaseErrorCode.FileEsistente:
                            sb.Append(CommandErrorCode.FileEsistente.ToString("D")).Append(" Un file con quel nome esiste gia'.");
                            break;
                        default:
                            l.log("Server Error!! " + e.Message,Level.ERR);
                            sb.Append(CommandErrorCode.Default.ToString("D")).Append(" Errore del server durante la creazione del file.");
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
                    snap = nuovo.Snapshots.Nuovo(t_modifica, dim, sha256);
                }
                catch (Exception e)
                {
                    l.log("Errore... " + e.Message, Level.ERR);
                    sb.Append(CommandErrorCode.Unknown.ToString("D")).Append(" Un errore sconosciuto è accaduto nel server.");
                    snap = null;
                }
                if(snap == null)
                {
                    yield return sb.ToString();
                    yield break;
                }
                string token = CollegamentoDati.getNewToken();
                yield return sb.Append(CommandErrorCode.OKIntermedio.ToString("D")).Append(" Stream dati pronto").ToString();
                yield return token;
                yield return "";
                NetworkStream stream_dati = CollegamentoDati.getCollegamentoDati(token);
                byte[] buffer = new byte[1024];
                int letti = 0;
                try
                {
                    do
                    {
                        letti = stream_dati.Read(buffer, 0, 1024);
                        snap.scriviBytes(buffer, letti);
                    } while (letti != 0);
                    snap.completaScrittura();
                }
                catch (Exception e)
                {
                    l.log("Errore nella scrittura del file" + e.Message, Level.ERR);
                    throw;
                }
                yield return CommandErrorCode.OK.ToString("D") + " Trasferimento completato con successo";
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
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 3)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D"))
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
                }
                if(snap == null)
                {
                    yield return sb.Append(CommandErrorCode.AperturaFile.ToString("D")).Append(" File non esistente o errore strano").ToString();
                    yield break;
                }
                string token = CollegamentoDati.getNewToken();
                yield return sb.Append(CommandErrorCode.OKIntermedio.ToString("D")).Append(" File pronto. Connettiti alla porta corrispondente").ToString();
                yield return token;
                yield return snap.Dim.ToString();
                yield return snap.shaContenuto;
                yield return "";
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
                try
                {
                    //il file era delete e lo sto ripristinando
                    if (!user.FileList[nome_file, path_relativo].Valido)
                    {
                        user.FileList[nome_file, path_relativo].Valido = true;
                        user.FileList[nome_file, path_relativo].Snapshots[timestamp].Valido = true;
                    }
                    //sto scaricando una versione precedente
                    else
                    {
                        //setto a false lo snapshot vecchio
                        user.FileList[nome_file, path_relativo].Snapshots.getValido().Valido = false;
                        //setto a true lo snapshot nuovo
                        user.FileList[nome_file, path_relativo].Snapshots[timestamp].Valido = true;
                    }
                }
                catch (Exception e)
                {
                    l.log("Errore nel settare il flag a valido: " + e.Message);
                    throw;
                }
                yield return sb.Append(CommandErrorCode.OK.ToString("D")).Append(" File scritto correttamente").ToString();
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
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if(dati.Count < 5)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D"))
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
                yield return sb.Append(CommandErrorCode.OKIntermedio.ToString("D")).Append(" Stream dati pronto").ToString();
                yield return token;
                yield return "";
                NetworkStream stream_dati = CollegamentoDati.getCollegamentoDati(token);
                byte[] buffer = new byte[1024];
                int letti = 0;
                try
                {
                    do
                    {
                        letti = stream_dati.Read(buffer, 0, 1024);
                        snap.scriviBytes(buffer, letti);
                    } while (letti != 0);
                    snap.completaScrittura();
                }
                catch( Exception e)
                {
                    l.log("Errore nella scrittura del file: "+e.Message,Level.ERR);
                    throw;
                }

                yield return CommandErrorCode.OK.ToString("D")+" Trasferimento completato con successo";
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
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 3)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D"))
                                    .Append(" I dati inviati non sono sufficienti").ToString();
                    yield break;
                }
                try
                {
                    user.FileList[nome_file: dati[0], path_relativo: dati[1]].Valido = false;
                    for (int i = 0; i < user.FileList[nome_file: dati[0], path_relativo: dati[1]].Snapshots.Length; i++)
                        user.FileList[nome_file: dati[0], path_relativo: dati[1]].Snapshots[i].Valido = false; 
                }
                catch(Exception e)
                {
                    l.log("Errore nel settare il file e i sui snapshot come non validi; " + e.Message,Level.ERR);
                }
                yield return sb.Append(CommandErrorCode.OK.ToString("D")).Append(" File eliminato con successo").ToString();
            }
        }

        private class ComandoListFolders : Command
        {
            /// <summary>
            /// Ritorna uno per riga i paths di un utente. Utile per ricostruire il file system
            /// </summary>
            /// <param name="dati">
            /// null
            /// </param>
            /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>

            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                string[] paths = null;
                try
                {
                    paths = user.FileList.PathNames;
                    sb.Append(CommandErrorCode.OK.ToString("D")).Append(" OK");
                }
                catch
                {
                    sb.Append(CommandErrorCode.Unknown.ToString("D")).Append(" Un errore sconosciuto è accaduto nel server");
                }
                yield return sb.ToString();
                if (paths == null)
                    yield break;
                foreach(string p in paths)
                {
                    yield return p;
                }
            }
        }

        private class ComandoListDir : Command
        {
            /// <summary>
            /// Ritorna uno per riga i nomi dei file nel direttamente nel path specificato
            /// </summary>
            /// <param name="dati">
            /// <list type="string">
            ///    <item index=0 >Path</item>
            /// </list>
            /// </param>
            /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>

            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if(dati.Count < 1)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D")).Append(" Manca il path.").ToString();
                    yield break;
                }

                string[] files = null;
                try
                {
                    files = user.FileList.FileNames(dati[0]);
                    sb.Append(CommandErrorCode.OK.ToString("D")).Append(" OK");
                }
                catch
                {
                    sb.Append(CommandErrorCode.Unknown.ToString("D")).Append(" Un errore sconosciuto è accaduto nel server");
                }
                yield return sb.ToString();
                if (files == null)
                    yield break;
                foreach (string f in files)
                {
                    yield return f;
                }
            }
        }

        private class ComandoListVersions: Command
        {
            /// <summary>
            /// Registra un nuovo utente
            /// </summary>
            /// <param name="dati">
            /// <list type="string">
            ///    <item index=0 >nome file</item>
            ///    <item index=1 >path</item>
            /// </list>
            /// </param>
            /// <returns>OK se terminato con successo, un codice e messaggio di errore altrimenti</returns>
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                if (user == null)
                {
                    yield return sb.Append(CommandErrorCode.UtenteNonLoggato.ToString("D")).Append(" Utente non loggato.").ToString();
                    yield break;
                }
                if (dati.Count < 2)
                {
                    yield return sb.Append(CommandErrorCode.DatiIncompleti.ToString("D")).Append(" Manca il path o il nome del file.").ToString();
                    yield break;
                }

                DateTime[] versioni = null;
                try
                {
                    versioni = user.FileList[dati[0],dati[1]].Snapshots.timestampList;
                    sb.Append(CommandErrorCode.OK.ToString("D")).Append(" OK");
                }
                catch
                {
                    sb.Append(CommandErrorCode.Unknown.ToString("D")).Append(" Un errore sconosciuto è accaduto nel server");
                }
                yield return sb.ToString();
                if (versioni == null)
                    yield break;
                foreach (DateTime t in versioni)
                {
                    yield return t.Ticks.ToString();
                }
            }
        }

        private class ComandoEsci : Command
        {
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                user.Logout();
                yield return sb.Append(CommandErrorCode.OK.ToString("D")).Append(" Connessione terminata con successo").ToString();
            }
        }
        private class ComandoDefault : Command
        {
            public override IEnumerable<string> esegui(List<string> dati)
            {
                StringBuilder sb = new StringBuilder();
                yield return sb.Append(CommandErrorCode.Default.ToString("D")).Append(" Comando non compreso").ToString();
            }
        }
    }
}
