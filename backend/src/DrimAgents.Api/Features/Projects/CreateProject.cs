using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Common.Services;
using DrimAgents.Api.Database;
using DrimAgents.Api.Domain.Projects;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DrimAgents.Api.Features.Projects;

public static class CreateProject
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/projects", async (
                HttpContext httpContext,
                [FromBody] Body body,
                ISender sender,
                CancellationToken ct) =>
            {
                var userId = httpContext.GetUserId()!.Value;
                var request = new Request(userId, body.Name, body.Description, body.GitHubRepoUrl, body.GitHubPat);
                var response = await sender.Send(request, ct);

                return Results.Created($"/api/projects/{response.Id}", response);
            })
            .RequireAuthorization()
            .WithName("CreateProject")
            .WithTags("Projects")
            .Produces<Response>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);
        }

        private record Body(string Name, string? Description, string GitHubRepoUrl, string GitHubPat);
    }

    public record Request(
        long UserId,
        string Name,
        string? Description,
        string GitHubRepoUrl,
        string GitHubPat) : IRequest<Response>;

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
                .WithMessage("GitHub personal access token is required")
                .WithErrorCode("projects:project:github_pat:required");
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _db;
        private readonly IGitHubService _gitHubService;
        private readonly IDataProtectionEncryption _encryption;
        private readonly IIdFactory _idFactory;

        public RequestHandler(
            AppDbContext db,
            IGitHubService gitHubService,
            IDataProtectionEncryption encryption,
            IIdFactory idFactory)
        {
            _db = db;
            _gitHubService = gitHubService;
            _encryption = encryption;
            _idFactory = idFactory;
        }

        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            await _gitHubService.ValidateRepositoryAccess(request.GitHubRepoUrl, request.GitHubPat, ct);

            var encryptedPat = _encryption.Encrypt(request.GitHubPat);

            var project = new Project
            {
                Id = _idFactory.CreateId(),
                UserId = request.UserId,
                Name = request.Name,
                Description = request.Description,
                GitHubRepoUrl = request.GitHubRepoUrl,
                EncryptedGitHubPat = encryptedPat,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync(ct);

            return new Response(
                Base32Encoder.Encode(project.Id),
                project.Name,
                project.Description,
                project.GitHubRepoUrl,
                MaskPat(request.GitHubPat),
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
