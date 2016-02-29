using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgettoMalnati
{
    class Test
    {
        static public void RunTestDB()
        {
            User u1, u2, u3;
            Console.Write("Testing the db\n");
            try
            {
                u1 = new User("tizio", "abbecedario");
            }
            catch (UserNotFoundException e)
            {
                Console.Write("Errore! L'utente esiste ma non è stato trovato. " + e.Message);
                throw new Exception("TestDB fallito\n");
            }
            catch (Exception e)
            {
                Console.Write("Un errore è accaduto: " + e.Message);
                throw e;
            }

            try
            {
                u2 = new User("oscar", "abbecedario");
                Console.Write("Attenzione! Utente creato anche se il nome è errato\n");
                throw new Exception("TestDB fallito\n");
            }
            catch (UserNotFoundException e)
            {
                Console.Write("Bene! La password è errata e la creazione dell'utente ha generato l'errore corrispondente. " + e.Message + "\n");
            }
            catch (Exception e)
            {
                Console.Write("Un errore è accaduto: " + e.Message + "\n");
                throw e;
            }
            try
            {
                u3 = new User("tizio", "wrongPwd");
                Console.Write("Attenzione! Utente creato anche se la password è errata\n");
                throw new Exception("TestDB fallito\n");
            }
            catch (UserNotFoundException e)
            {
                Console.Write("Bene! La password è errata e la creazione dell'utente ha generato l'errore corrispondente. " + e.Message + "\n");
            }
            catch (Exception e)
            {
                Console.Write("Un errore è accaduto: " + e.Message + "\n");
                throw e;
            }
        }

    }
}
