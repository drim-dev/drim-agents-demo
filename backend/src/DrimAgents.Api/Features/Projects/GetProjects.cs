using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Common.Services;
using DrimAgents.Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Features.Projects;

public static class GetProjects
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/projects", async (
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
            .WithName("GetProjects")
            .WithTags("Projects")
            .Produces<Response>()
            .Produces(StatusCodes.Status401Unauthorized);
        }
    }

    public record Request(long UserId) : IRequest<Response>;

    public record Response(ProjectItem[] Items);

    public record ProjectItem(
        string Id,
        string Name,
        string? Description,
        string GitHubRepoUrl,
        string MaskedGitHubPat,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _db;
        private readonly IDataProtectionEncryption _encryption;

        public RequestHandler(AppDbContext db, IDataProtectionEncryption encryption)
        {
            _db = db;
            _encryption = encryption;
        }

        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var projects = await _db.Projects
                .AsNoTracking()
                .Where(p => p.UserId == request.UserId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Id, p.Name, p.Description, p.GitHubRepoUrl, p.EncryptedGitHubPat, p.CreatedAt, p.UpdatedAt })
                .ToListAsync(ct);

            var items = projects.Select(p => new ProjectItem(
                Base32Encoder.Encode(p.Id),
                p.Name,
                p.Description,
                p.GitHubRepoUrl,
                MaskPat(_encryption.Decrypt(p.EncryptedGitHubPat)),
                p.CreatedAt,
                p.UpdatedAt
            )).ToArray();

            return new Response(items);
        }

        private static string MaskPat(string decryptedPat)
        {
            var lastFour = decryptedPat.Length >= 4 ? decryptedPat[^4..] : decryptedPat;
            return $"····{lastFour}";
        }
    }
}
