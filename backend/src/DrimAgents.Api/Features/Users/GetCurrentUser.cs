using DrimAgents.Api.Common.Exceptions;
using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Features.Users;

public static class GetCurrentUser
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/users/me", async (
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
            {
                var userId = httpContext.GetUserId()!.Value;
                var request = new Request(userId);
                var response = await sender.Send(request, ct);

                return Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithTags("Users")
            .Produces<Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
        }
    }

    public record Request(long UserId) : IRequest<Response>;

    public record Response(
        string Id,
        string Email,
        string? DisplayName,
        string? AvatarUrl,
        string Role,
        DateTime CreatedAt);

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<RequestHandler> _logger;

        public RequestHandler(AppDbContext db, ILogger<RequestHandler> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var user = await _db.Users
                .Where(u => u.Id == request.UserId)
                .Select(u => new Response(
                    Base32Encoder.Encode(u.Id),
                    u.Email,
                    u.DisplayName,
                    u.AvatarUrl,
                    u.Role.ToString(),
                    u.CreatedAt))
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                _logger.LogWarning(
                    "User {UserId} authenticated but not found in database",
                    request.UserId);
                throw new NotFoundException("User not found");
            }

            return user;
        }
    }
}
