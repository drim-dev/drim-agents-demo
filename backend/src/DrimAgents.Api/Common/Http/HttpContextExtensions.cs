using System.Security.Claims;

namespace DrimAgents.Api.Common.Http;

public static class HttpContextExtensions
{
    public static long? GetUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub")
                         ?? context.User.FindFirst(ClaimTypes.NameIdentifier);

        var userIdString = userIdClaim?.Value;

        if (userIdString == null)
        {
            userIdString = context.Request.Headers["X-User-Id"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(userIdString))
            return null;

        if (long.TryParse(userIdString, out var userId))
            return userId;

        return null;
    }

    public static string? GetUserEmail(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Email)?.Value
               ?? context.Request.Headers["X-User-Email"].FirstOrDefault();
    }

    public static string? GetUserRole(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Role)?.Value
               ?? context.Request.Headers["X-User-Role"].FirstOrDefault();
    }

    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true;
    }

    public static bool IsInRole(this HttpContext context, string role)
    {
        return context.User.IsInRole(role);
    }

    public static bool IsAdmin(this HttpContext context)
    {
        return context.IsInRole("Admin");
    }

    public static bool IsInstructorOrAdmin(this HttpContext context)
    {
        return context.IsInRole("Instructor") || context.IsInRole("Admin");
    }
}
