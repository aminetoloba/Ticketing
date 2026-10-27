using Microsoft.AspNetCore.Authorization;

namespace Ticketing.WebApi.Authorization;

// Simple marqueur : la logique est dans le handler
public class EventOwnerRequirement : IAuthorizationRequirement
{
}