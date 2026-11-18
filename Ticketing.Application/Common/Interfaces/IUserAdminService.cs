using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ticketing.Application.DTOs.Events;
using Ticketing.Application.DTOs.Users;

namespace Ticketing.Application.Common.Interfaces;

public interface IUserAdminService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<UserDto> ChangeRoleAsync(Guid userId, ChangeRoleRequest request, CancellationToken cancellationToken = default);
}
