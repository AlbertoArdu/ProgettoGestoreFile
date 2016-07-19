using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgettoMalnati
{
    enum CommandErrorCode
    {
        OK = 0,
        // Errori brutti
        Default,
        Abort,
        DatiIncompleti,
        FormatoDatiErrato,
        MomentoSbagliato,

        // Applicazione
        NomeUtenteInUso,
        UtenteNonLoggato,
        Unknown = -1
    }
}
