using System;

namespace clientWPF
{
    enum ServerErrorCode
    {
        // Errori brutti
        Unknown = -1,
        Default,
        Abort,
        DatiIncompleti,
        FormatoDatiErrato,
        MomentoSbagliato,
        DatiInconsistenti, //Dimensione o hash non corrispondenti
        // Applicazione
        //Registrazione
        NomeUtenteInUso,
        //Login
        DatiErrati, //Nome utente o password
        UtenteNonLoggato,
        //Creazione file
        LimiteFileSuperato,
        FileEsistente,
        CollegamentoDatiNonDisponibile,
    }
    class ServerException: Exception
    {
        ServerErrorCode __err_code;
        
        public ServerException(string message = "Un errore sconosciuto è accaduto nel server", ServerErrorCode err = ServerErrorCode.Default)
            : base(message)
        {
            this.__err_code = err;
        }

        public ServerErrorCode ErrorCode
        {
            get { return __err_code; }
        }
    }
}
