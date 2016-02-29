using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finisar.SQLite;
using System.Data;

namespace ProgettoMalnati
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.Write("Starting the server...\n");

            Test.RunTestDB();
            

        }
    }
}
