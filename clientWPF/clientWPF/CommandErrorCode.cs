using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clientWPF
{
    enum CommandErrorCode
    {
        OK = 0,
        OKIntermedio,
        // Errori brutti
        Default,
        Abort,
        NotImplemented,
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
        FileEsistente,
        //Scaricamento file
        AperturaFile,
        Unknown = -1
    }

}
