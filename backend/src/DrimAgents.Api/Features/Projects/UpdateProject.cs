using DrimAgents.Api.Common.Exceptions;
using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Common.Services;
using DrimAgents.Api.Database;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrimAgents.Api.Features.Projects;

public static class UpdateProject
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/api/projects/{id}", async (
                string id,
                HttpContext httpContext,
                [FromBody] Body body,
                ISender sender,
                CancellationToken ct) =>
            {
                if (!Base32Encoder.TryDecode(id, out var projectId))
                    return Results.BadRequest("Invalid project ID format");

                var userId = httpContext.GetUserId()!.Value;
                var request = new Request(projectId, userId, body.Name, body.Description, body.GitHubRepoUrl, body.GitHubPat);
                var response = await sender.Send(request, ct);

                return Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("UpdateProject")
            .WithTags("Projects")
            .Produces<Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
        }

        private record Body(string Name, string? Description, string GitHubRepoUrl, string? GitHubPat);
    }

    public record Request(
        long ProjectId,
        long UserId,
        string Name,
        string? Description,
        string GitHubRepoUrl,
        string? GitHubPat) : IRequest<Response>;

    public record Response(
        string Id,
        string Name,
        string? Description,
        string GitHubRepoUrl,
        string MaskedGitHubPat,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .WithErrorCode("projects:project:name:required")
                .MinimumLength(3)
                .WithMessage("Name must be at least 3 characters")
                .WithErrorCode("projects:project:name:too_short")
                .MaximumLength(200)
                .WithMessage("Name must not exceed 200 characters")
                .WithErrorCode("projects:project:name:too_long");

            RuleFor(x => x.GitHubRepoUrl)
                .NotEmpty()
                .WithMessage("GitHub repository URL is required")
                .WithErrorCode("projects:project:github_repo_url:required")
                .Matches(@"^https?://github\.com/[^/]+/[^/.]+(?:\.git)?/?$")
                .WithMessage("GitHub repository URL must be a valid GitHub repository URL")
                .WithErrorCode("projects:project:github_repo_url:invalid_format");

            RuleFor(x => x.GitHubPat)
                .NotEmpty()
                .When(x => x.GitHubPat != null)
                .WithMessage("GitHub personal access token must not be empty when provided")
                .WithErrorCode("projects:project:github_pat:empty");
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _db;
        private readonly IGitHubService _gitHubService;
        private readonly IDataProtectionEncryption _encryption;

        public RequestHandler(
            AppDbContext db,
            IGitHubService gitHubService,
            IDataProtectionEncryption encryption)
        {
            _db = db;
            _gitHubService = gitHubService;
            _encryption = encryption;
        }

        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var project = await _db.Projects
                .Where(p => p.Id == request.ProjectId && p.UserId == request.UserId)
                .FirstOrDefaultAsync(ct);

            if (project == null)
                throw new NotFoundException("Project", request.ProjectId);

            string decryptedPat;

            if (request.GitHubPat != null)
            {
                await _gitHubService.ValidateRepositoryAccess(request.GitHubRepoUrl, request.GitHubPat, ct);
                project.EncryptedGitHubPat = _encryption.Encrypt(request.GitHubPat);
                decryptedPat = request.GitHubPat;
            }
            else if (request.GitHubRepoUrl != project.GitHubRepoUrl)
            {
                decryptedPat = _encryption.Decrypt(project.EncryptedGitHubPat);
                await _gitHubService.ValidateRepositoryAccess(request.GitHubRepoUrl, decryptedPat, ct);
            }
            else
            {
                decryptedPat = _encryption.Decrypt(project.EncryptedGitHubPat);
            }

            project.Name = request.Name;
            project.Description = request.Description;
            project.GitHubRepoUrl = request.GitHubRepoUrl;
            project.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return new Response(
                Base32Encoder.Encode(project.Id),
                project.Name,
                project.Description,
                project.GitHubRepoUrl,
                MaskPat(decryptedPat),
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
