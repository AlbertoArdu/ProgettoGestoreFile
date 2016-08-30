using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WPFPageSwitch
{
    /// <summary>
    /// Classe usata per creare un collegamento dati (dati binari) con il server.
    /// </summary>
    class CollegamentoDati
    {

        static private int port;
        static public int token_length = 20;

        /// <summary>
        /// Funzione che ritorna un NetworkStream pronto per ricevere o spedire dati.
        /// </summary>
        /// <param name="token">Il token inviato dal server</param>
        /// <returns>La connessione con il server in caso di successo.</returns>
        /// <exception cref="Exception">Lancia un'eccezione se il token non è valido o non è possibile connettere il socket</exception>
        static public NetworkStream getCollegamentoDati(string token)
        {
            int token_length = 20;
            TcpClient c = null;
            IPAddress server_addr = IPAddress.Parse(Properties.Settings.Default.ip_address);
            NetworkStream netS = null;
            port = Properties.Settings.Default.data_port;
            byte[] tmp = new byte[token_length = 20];
            tmp = System.Convert.FromBase64String(token);
            c = new TcpClient();
            try
            {
                c.Connect(server_addr, port);
                netS = c.GetStream();
                netS.Write(tmp, 0, token_length);
                netS.Flush();
            }
            catch
            {
                throw new ServerException("Errore nell'istanziare una connessione dati",
                    ServerErrorCode.CollegamentoDatiNonDisponibile);
            }
            return netS;
        }

    }
}
