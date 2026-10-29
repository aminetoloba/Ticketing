using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.DTOs.Staff;

public record AssignStaffRequest(string Email);

public record StaffMemberDto(Guid StaffId, string Email, string FullName, DateTime AssignedAt);

// Résultat de la recherche d'un compte Staff par email
public record StaffUserInfo(Guid Id, string Email, string FullName);
