using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ProgettoMalnati
{
    static class Program
    {
        private static List<Server> s_list = null;
        //Ciclo infinito su un socket server e lancia i singoli server (gestendo eventuali errori)
        //Mantiene una lista di 
        static void Main(string[] args)
        {
            Log l = Log.getLog();

            l.log("Starting the server...");
            string base_path = Directory.GetCurrentDirectory();
            Properties.ApplicationSettings.Default.base_path = base_path;
            Properties.ApplicationSettings.Default.Save();

            Test.RunTestDB();

            IPAddress mio_ip = IPAddress.Any;
            int port = Properties.ApplicationSettings.Default.tcp_port;
            TcpListener acceptor = TcpListener.Create(port);
            s_list = new List<Server>();
            acceptor.AllowNatTraversal (true);
            acceptor.Start(Properties.ApplicationSettings.Default.max_connessioni_in_sospeso);
            TcpClient client;
            while (true)
            {
                client = acceptor.AcceptTcpClient();
                s_list.Add(new Server(client));

                foreach(Server s in s_list)
                {
                    if (!s.Connected)
                    {
                        s_list.Remove(s);
                    }
                }
            }
        }

    }
}
