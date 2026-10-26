using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Application.Common;

public static class Roles
{
    public const string Participant = "Participant";
    public const string Organizer = "Organizer";
    public const string Staff = "Staff";
    public const string Admin = "Admin";

    public static readonly string[] All = { Participant, Organizer, Staff, Admin };
}
