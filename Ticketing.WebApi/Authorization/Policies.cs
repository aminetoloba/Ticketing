using Ticketing.Application.Common;

namespace Ticketing.WebApi.Authorization;

public static class Policies
{
    public const string IsEventOwner = "IsEventOwner";
    public const string OrganizerOrAdmin = Roles.Organizer + "," + Roles.Admin;
    public const string Buyers = Roles.Participant + "," + Roles.Organizer;
    public const string TicketHolders = Roles.Participant + "," + Roles.Organizer + "," + Roles.Admin;
}