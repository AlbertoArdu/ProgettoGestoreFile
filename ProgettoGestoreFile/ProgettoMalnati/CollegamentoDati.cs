using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ProgettoMalnati
{
    /// <summary>
    /// Classe con un thread in ascolto sulla porta tcp dei dati.
    /// Quando un client si connette:
    ///  - legge il token dal socket
    ///  - aggiunge il socket a un dizionario che ha come indice il token stesso
    /// Un thread Server che necessita di un socket dati invia un token al client;
    /// questo si collega alla porta dati e scrive il token (lunghezza fissa).
    /// Intanto il thread server richiede a questa classe il socket con il token che ha inviato
    /// e viene messo in attesa fintantoché il client non è connesso e ha consegnato il token.
    /// </summary>
    static class CollegamentoDati
    {

        static private Thread t;
        static private TcpListener acceptor;
        static private int port;
        static private Dictionary<string, TcpClient> socket_dati_in_sospeso;
        static public int token_length = 20;
        static private Random rand_gen;
        static private object lockDictionary;
        static private Dictionary<string,AutoResetEvent> waitHandles;
        static private int timeout = Properties.ApplicationSettings.Default.timeout_dati;

        static public void Inizializza()
        {
            port = Properties.ApplicationSettings.Default.tcp_port_dati;
            if(lockDictionary != null)
            {
                throw new Exception("Vietato chiamare due volte questa funzione");
            }
            rand_gen = new Random((int)DateTime.Now.Ticks);
            socket_dati_in_sospeso = new Dictionary<string, TcpClient>();
            lockDictionary = new object();
            waitHandles = new Dictionary<string, AutoResetEvent>();
            t = new Thread(CollegamentoDati.gestisciConnessioneDati);
            acceptor = TcpListener.Create(port);
            acceptor.Start(Properties.ApplicationSettings.Default.max_connessioni_in_sospeso);
            t.Start();
        }

        static private void gestisciConnessioneDati()
        {
            TcpClient c;
            string token;
            Log l = Log.getLog();
            while (true)
            {
                c = acceptor.AcceptTcpClient();
                try
                {
                    NetworkStream stream = c.GetStream();
                    byte[] tmp = new byte[token_length];
                    stream.Read(tmp, 0, token_length);
                    token = System.Convert.ToBase64String(tmp);
                    lock (lockDictionary)
                    {
                        socket_dati_in_sospeso[token] = c;
                        waitHandles[token].Set();
                    }
                }
                catch (IOException e){
                    l.log("Errore nella connessione con un client: "+e.Message,Level.INFO);
                }
            }
        }

        /// <summary>
        /// Restituisce un token garantito univoco.
        /// </summary>
        /// <returns>Il token</returns>
        static public string getNewToken()
        {
            string token;
            lock (lockDictionary)
            {
                byte[] tmp = new byte[CollegamentoDati.token_length];
                do
                {
                    rand_gen.NextBytes(tmp);
                    token = System.Convert.ToBase64String(tmp);
                }
                while (socket_dati_in_sospeso.ContainsKey(token));
                socket_dati_in_sospeso.Add(token, null);
                waitHandles.Add(token, new AutoResetEvent(false));
            }
            return token;
        }

        /// <summary>
        /// Funzione che mette in attesa il chiamante fino all'arrivo di una connessione con
        /// il token corrispondente.
        /// </summary>
        /// <param name="token">Il token che mi aspetto</param>
        /// <returns>La connessione con il client in caso di successo. null se scade il timeout.</returns>
        /// <exception cref="Exception">Lancia un'eccezione se il token non è valido</exception>
        static public NetworkStream getCollegamentoDati(string token)
        {
            TcpClient c = null;

            if(!waitHandles.ContainsKey(token))
                throw new Exception("Token non valido");

            //waitHandles[token].WaitOne(timeout);
            waitHandles[token].WaitOne(-1);
            lock (lockDictionary)
            {
                if (!socket_dati_in_sospeso.ContainsKey(token))
                {
                    throw new Exception("Token non valido");
                }
                c = socket_dati_in_sospeso[token];
                socket_dati_in_sospeso.Remove(token);
                waitHandles.Remove(token);
            }
            return c.GetStream();
        }
    }

}
