using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common;

public class AuthException : Exception
{
    // true = 401 (identifiants ou token invalides), false = 400 (requête incorrecte)
    public bool IsUnauthorized { get; }
    public AuthException(string message, bool isUnauthorized = true) : base(message)
    {
        IsUnauthorized = isUnauthorized;
    }
}
