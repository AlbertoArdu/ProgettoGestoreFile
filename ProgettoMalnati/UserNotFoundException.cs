using System;

namespace ProgettoMalnati
{
    class UserNotFoundException : Exception
    {
        public UserNotFoundException() : base("User not found: nome utente o password errati")
        {
        }

        public UserNotFoundException(string message)
            : base(message)
        {
        }

        public UserNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
