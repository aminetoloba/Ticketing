using Ticketing.Application.Common;

namespace Ticketing.WebApi.Authorization;

public static class Policies
{
    public const string IsEventOwner = "IsEventOwner";
    public const string OrganizerOrAdmin = Roles.Organizer + "," + Roles.Admin;
}