using DrimAgents.Api.Common.Exceptions;
using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Common.Services;
using DrimAgents.Api.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Features.Projects;

public static class GetProject
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/projects/{id}", async (
                string id,
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
            {
                if (!Base32Encoder.TryDecode(id, out var projectId))
                    return Results.BadRequest("Invalid project ID format");

                var userId = httpContext.GetUserId()!.Value;
                var request = new Request(projectId, userId);
                var response = await sender.Send(request, ct);

                return Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("GetProject")
            .WithTags("Projects")
            .Produces<Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
        }
    }

    public record Request(long ProjectId, long UserId) : IRequest<Response>;

    public record Response(
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
            var project = await _db.Projects
                .AsNoTracking()
                .Where(p => p.Id == request.ProjectId && p.UserId == request.UserId)
                .Select(p => new { p.Id, p.Name, p.Description, p.GitHubRepoUrl, p.EncryptedGitHubPat, p.CreatedAt, p.UpdatedAt })
                .FirstOrDefaultAsync(ct);

            if (project == null)
                throw new NotFoundException("Project", request.ProjectId);

            return new Response(
                Base32Encoder.Encode(project.Id),
                project.Name,
                project.Description,
                project.GitHubRepoUrl,
                MaskPat(_encryption.Decrypt(project.EncryptedGitHubPat)),
                project.CreatedAt,
                project.UpdatedAt);
        }

        private static string MaskPat(string decryptedPat)
        {
            var lastFour = decryptedPat.Length >= 4 ? decryptedPat[^4..] : decryptedPat;
            return $"····{lastFour}";
        }
    }
}
