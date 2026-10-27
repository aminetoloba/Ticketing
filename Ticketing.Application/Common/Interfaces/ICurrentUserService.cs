using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserIdentifier { get; }
    Guid? UserId { get; }
    string? Role { get; }
}
