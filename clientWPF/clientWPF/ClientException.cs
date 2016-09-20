using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clientWPF
{
    enum ClientErrorCode
    {
        // Errori brutti
        Unknown = -1,
        Default,
        //Applicazione
        DatabaseCorrotto,
        DatabaseNonPresente,
        ServerNonDisponibile,
        PercorsoNonSpecificato,
        CredenzialiUtenteMancanti,
        CredenzialiUtenteErrate,
        ControlloNonInizializzato,
    }
    class ClientException : Exception
    {
        ClientErrorCode __err_code;

        public ClientException(string message = "Errore sconosciuto", ClientErrorCode err = ClientErrorCode.Default)
            : base(message)
        {
            this.__err_code = err;
        }

        public ClientErrorCode ErrorCode
        {
            get { return __err_code; }
        }
    }
}
