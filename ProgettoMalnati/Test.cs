using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgettoMalnati
{
    class Test
    {
        static Log l;
        static public void RunTestDB()
        {
            l = Log.getLog();
            //TestUsers();
            //TestSnapshots();
            TestUserRegistration();
            //TestUserAndSnapshot();
        }
        static void TestUsers()
        {
            User u1, u2, u3;
            Console.Write("Testing the db\n");
            Log l = Log.getLog();
            l.log("Starting testing.");
            try
            {
                l.log("Creo l'oggetto User relativo a un utente registrato");
                u1 = new User("tizio", "abbecedario");
                l.log("Successo!");
            }
            catch (DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserNonEsistente)
            {
                l.log("Errore! L'utente esiste ma non è stato trovato. " + e.Message);
                throw new Exception("TestDB fallito\n");
            }
            catch (Exception e)
            {
                l.log("Un errore è accaduto: " + e.Message);
                throw e;
            }

            try
            {
                u2 = new User("oscar", "abbecedario");
                l.log("Attenzione! Utente creato anche se il nome è errato\n");
                throw new Exception("TestDB fallito\n");
            }
            catch (DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserNonEsistente)
            {
                l.log("Bene! La password è errata e la creazione dell'utente ha generato l'errore corrispondente. " + e.Message + "\n");
            }
            catch (Exception e)
            {
                l.log("Un errore è accaduto: " + e.Message + "\n");
                throw e;
            }
            try
            {
                u3 = new User("tizio", "wrongPwd");
                l.log("Attenzione! Utente creato anche se la password è errata\n");
                throw new Exception("TestDB fallito\n");
            }
            catch (DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserNonEsistente)
            {
                l.log("Bene! La password è errata e la creazione dell'utente ha generato l'errore corrispondente. " + e.Message + "\n");
            }
            catch (Exception e)
            {
                l.log("Un errore è accaduto: " + e.Message + "\n");
                throw e;
            }
        }
        static void TestSnapshots()
        {
            try {
                //Test 1: apro uno snapshot e leggo il contenuto
                Snapshot s1 = new Snapshot("cesare", 16);
                l.log("Snapshot creato");
                byte[] b = new byte[s1.Dim + 1];
                s1.leggiBytesDalContenuto(b,s1.Dim);
                l.log(System.Text.Encoding.Default.GetString(b));
                //Ora cambio il contenuto
                s1.cambiaContenuto(25,DateTime.Now);
                s1.scriviBytes(System.Text.Encoding.ASCII.GetBytes("Ciao ciccio, come va?????"),25);
            } catch(Exception e)
            {
                l.log(e.ToString());
                throw;
            }
            try
            {
                string contenuto = "Cantami o diva del pelide Achille...";
                string sha = "3698f94388c67c9fe4adaac8eec03f6ddddbc08abdb8bfebbe865bc92d971e86";
                Snapshot s2 = Snapshot.creaNuovo("cesare", "augusto.imp", ".", DateTime.Now, contenuto.Length,sha);
                l.log("Nuovo snapshot creato; id - "+s2.Id);
                s2.scriviBytes(System.Text.Encoding.ASCII.GetBytes("Cantami o diva del pelide Achille..."), contenuto.Length);
            }
            catch(Exception e)
            {
                l.log(e.ToString());
                throw;
            }
            Console.Read();

        }
        static void TestUserAndSnapshot()
        {
            User u1 = new User("cesare", "abbecedario");
            l.log("");
        }
        static void TestUserRegistration()
        {
            try
            {
                User u = User.RegistraUtente("cesare", "cicco", ".");
                l.log("ERRORE!! L'utente è stato creato ma esiste già!");
            }
            catch(DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
            {
                l.log("L'utente è già esistente e non è stato registrato");
            }

            try
            {
                User u = User.RegistraUtente("ottaviano", "cicco", ".");
                l.log("Utente creato correttamente");
            }
            catch (DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
            {
                l.log("ERRORE!! L'utente non esiste ancora, ma non è stato registrato");
            }
        }
    }
}
