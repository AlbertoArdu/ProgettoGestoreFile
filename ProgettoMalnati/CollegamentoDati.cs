using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    class CollegamentoDati
    {

        static private Thread t;
        static private TcpListener acceptor;
        static private int port;
        static private Dictionary<byte[], TcpClient> socket_dati_in_sospeso;
        static public int token_lenght = 20;
        static private Random rand_gen;
        static private object lockDictionary;
        static private Dictionary<byte[],AutoResetEvent> waitHandles;
        static private int timeout = Properties.ApplicationSettings.Default.timeout_dati;

        static public void Inizializza()
        {
            port = Properties.ApplicationSettings.Default.tcp_port_dati;
            if(lockDictionary != null)
            {
                throw new Exception("Vietato chiamare due volte questa funzione");
            }
            rand_gen = new Random((int)DateTime.Now.Ticks);
            socket_dati_in_sospeso = new Dictionary<byte[], TcpClient>();
            lockDictionary = new object();
            waitHandles = new Dictionary<byte[], AutoResetEvent>;
            t = new Thread(CollegamentoDati.gestisciConnessioneDati);
            acceptor = TcpListener.Create(port);
            t.Start();
        }

        static private void gestisciConnessioneDati()
        {
            TcpClient c;
            byte[] token = new byte[token_lenght];

            while (true)
            {
                c = acceptor.AcceptTcpClient();
                NetworkStream stream = c.GetStream();
                stream.Read(token, 0,token_lenght);
                lock (lockDictionary)
                {
                    socket_dati_in_sospeso.Add(token, c);
                    waitHandles[token].Set();
                }
            }
        }

        /// <summary>
        /// Restituisce un token garantito univoco.
        /// </summary>
        /// <returns>Il token</returns>
        static public byte[] getNewToken()
        {
            byte[] token = new byte[CollegamentoDati.token_lenght];
            lock (lockDictionary)
            {
                do
                {
                    rand_gen.NextBytes(token);
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
        static public TcpClient getCollegamentoDati(byte[] token)
        {
            TcpClient c = null;

            waitHandles[token].WaitOne(timeout);
            lock (lockDictionary)
            {
                if (!socket_dati_in_sospeso.ContainsKey(token))
                {
                    throw new Exception("Token non valido");
                }
                c = socket_dati_in_sospeso[token];
                socket_dati_in_sospeso.Remove(token);
            }
                    
            lock (lockDictionary)
            {
                waitHandles.Remove(token);
            }
            return c;
        }

    }
}
