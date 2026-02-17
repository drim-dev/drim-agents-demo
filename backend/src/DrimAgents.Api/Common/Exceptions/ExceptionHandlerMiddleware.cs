using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DrimAgents.Api.Common.Exceptions;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = notFound.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            },
            ForbiddenException forbidden => CreateForbiddenProblemDetails(forbidden),
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflict.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            },
            BadRequestException badRequest => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = badRequest.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            UnprocessableEntityException unprocessable => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Unprocessable Entity",
                Detail = unprocessable.Message,
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            },
            ServiceUnavailableException serviceUnavailable => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = serviceUnavailable.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4"
            },
            ValidationException validation => new ValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = validation.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            }
        };

        if (exception is not NotFoundException and not ForbiddenException and not ConflictException and not BadRequestException and not UnprocessableEntityException and not ServiceUnavailableException and not ValidationException)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), options));
    }

    private static ProblemDetails CreateForbiddenProblemDetails(ForbiddenException forbidden)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = forbidden.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        };

        if (forbidden.ErrorCode != null)
        {
            problemDetails.Extensions["errorCode"] = forbidden.ErrorCode;
        }

        return problemDetails;
    }
}
