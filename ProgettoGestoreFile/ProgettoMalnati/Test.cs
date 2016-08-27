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

        static void TestUserRegistration()
        {
            try
            {
                User u = User.RegistraUtente("cesare", "cicco");
                l.log("ERRORE!! L'utente è stato creato ma esiste già!");
            }
            catch(DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
            {
                l.log("L'utente è già esistente e non è stato registrato");
            }

            try
            {
                User u = User.RegistraUtente("ottaviano", "cicco");
                l.log("Utente creato correttamente");
            }
            catch (DatabaseException e) when (e.ErrorCode == DatabaseErrorCode.UserGiaEsistente)
            {
                l.log("ERRORE!! L'utente non esiste ancora, ma non è stato registrato");
            }
        }
    }
}
