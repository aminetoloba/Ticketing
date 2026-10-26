using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Common;
using Ticketing.Domain.Exceptions;

namespace Ticketing.WebApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        int status;
        string title;
        object? errors = null;

        switch (ex)
        {
            case ValidationException validation:
                status = StatusCodes.Status400BadRequest;
                title = "Les données envoyées sont invalides.";
                errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case AuthException auth:
                status = auth.IsUnauthorized
                    ? StatusCodes.Status401Unauthorized
                    : StatusCodes.Status400BadRequest;
                title = auth.Message;
                break;

            case DomainException domain:
                status = StatusCodes.Status400BadRequest;
                title = domain.Message;
                break;

            case DbUpdateConcurrencyException:
                status = StatusCodes.Status409Conflict;
                title = "La ressource a été modifiée par une autre opération. Réessayez.";
                break;

            default:
                // Le détail reste dans les logs, jamais dans la réponse
                _logger.LogError(ex, "Erreur non gérée");
                status = StatusCodes.Status500InternalServerError;
                title = "Une erreur interne est survenue.";
                break;
        }

        var problem = new ProblemDetails { Status = status, Title = title };
        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}