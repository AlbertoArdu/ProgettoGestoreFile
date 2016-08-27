using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFPageSwitch
{
    enum CommandErrorCode
    {
        OK = 0,
        OKIntermedio,
        // Errori brutti
        Default,
        Abort,
        DatiIncompleti,
        FormatoDatiErrato,
        MomentoSbagliato,

        // Applicazione
            //Registrazione
        NomeUtenteInUso,
            //Login
        DatiErrati, //Nome utente o password
        UtenteNonLoggato,
           //Creazione file
        LimiteFileSuperato,
        Unknown = -1
    }
}
