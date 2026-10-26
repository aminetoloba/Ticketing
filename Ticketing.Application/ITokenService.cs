using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(
        Guid userId, string email, string fullName, string role);

    // Token en clair (pour le client), son hash (pour la base) et sa date d'expiration
    (string Token, string Hash, DateTime ExpiresAt) CreateRefreshToken();

    string HashToken(string token);
}
