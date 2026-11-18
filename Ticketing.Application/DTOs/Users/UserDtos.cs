using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Users;

public record UserDto(Guid Id, string Email, string FullName, string Role);

public record UserQuery(string? Search = null, string? Role = null, int Page = 1, int PageSize = 20);

public record ChangeRoleRequest(string Role);
