using System;

namespace ProgettoMalnati
{
    enum DatabaseErrorCode
    {
        //Low level
        Default,
        Constraint,
        FormatError,
        //Application level
        NomeUtenteNonValido,
        PasswordNonValida,
        PathNonValido,
        UserNonEsistente,
        UserGiaEsistente,
        SnapshotNonValido,
        SnapshotInLettura,
        SnapshotInScrittura,
        SnapshotHashInconsistente,
        //Errore imprevisto
        Unknown
    }

    class DatabaseException : Exception
    {
        DatabaseErrorCode __err_code;
        public DatabaseException() : base("User not found: nome utente o password errati")
        {
        }

        public DatabaseException(string message,DatabaseErrorCode err = DatabaseErrorCode.Default)
            : base(message)
        {
            this.__err_code = err;
        }

        public DatabaseErrorCode ErrorCode
        {
            get { return __err_code; }
        }
    }
}
