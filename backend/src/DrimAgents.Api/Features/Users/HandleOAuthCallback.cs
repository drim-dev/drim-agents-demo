using DrimAgents.Api.Common.Http;
using DrimAgents.Api.Common.Identity;
using DrimAgents.Api.Database;
using DrimAgents.Api.Domain.Users;
using DrimAgents.Api.Features.Users.Options;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DrimAgents.Api.Features.Users;

public static class HandleOAuthCallback
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/oauth-callback", async (
                [FromBody] Body body,
                ISender sender,
                CancellationToken ct) =>
            {
                var request = new Request(
                    body.Provider,
                    body.ProviderUserId,
                    body.ProviderEmail);

                var response = await sender.Send(request, ct);
                return Results.Ok(response);
            })
            .AllowAnonymous()
            .WithName("HandleOAuthCallback")
            .WithTags("Auth")
            .Produces<Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
        }

        private record Body(string Provider, string ProviderUserId, string ProviderEmail);
    }

    public record Request(
        string Provider,
        string ProviderUserId,
        string ProviderEmail) : IRequest<Response>;

    public record Response(string UserId, string Email, string Role);

    public class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage("Provider is required")
                .WithErrorCode("users:auth:provider:required")
                .Must(p => p is "google" or "github" or "gitlab")
                .WithMessage("Provider must be one of: google, github, gitlab")
                .WithErrorCode("users:auth:provider:invalid");

            RuleFor(x => x.ProviderUserId)
                .NotEmpty()
                .WithMessage("Provider user ID is required")
                .WithErrorCode("users:auth:provider_user_id:required")
                .MaximumLength(255)
                .WithMessage("Provider user ID must not exceed 255 characters")
                .WithErrorCode("users:auth:provider_user_id:too_long");

            RuleFor(x => x.ProviderEmail)
                .NotEmpty()
                .WithMessage("Provider email is required")
                .WithErrorCode("users:auth:provider_email:required")
                .EmailAddress()
                .WithMessage("Provider email must be a valid email address")
                .WithErrorCode("users:auth:provider_email:invalid_format")
                .MaximumLength(255)
                .WithMessage("Provider email must not exceed 255 characters")
                .WithErrorCode("users:auth:provider_email:too_long");
        }
    }

    public class RequestHandler : IRequestHandler<Request, Response>
    {
        private readonly AppDbContext _db;
        private readonly IIdFactory _idFactory;
        private readonly ILogger<RequestHandler> _logger;
        private readonly UsersOptions _usersOptions;

        public RequestHandler(AppDbContext db, IIdFactory idFactory, ILogger<RequestHandler> logger, IOptions<UsersOptions> usersOptions)
        {
            _db = db;
            _idFactory = idFactory;
            _logger = logger;
            _usersOptions = usersOptions.Value;
        }

        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var provider = request.Provider.ToLower();
            var email = request.ProviderEmail.ToLower();

            var existingOAuth = await _db.OAuthAccounts
                .Include(o => o.User)
                .FirstOrDefaultAsync(
                    o => o.Provider == provider && o.ProviderUserId == request.ProviderUserId,
                    ct);

            if (existingOAuth != null)
            {
                _logger.LogInformation(
                    "Existing OAuth account found for {Provider}:{ProviderUserId}, UserId: {UserId}",
                    provider, request.ProviderUserId, existingOAuth.UserId);

                await PromoteToAdminIfConfigured(existingOAuth.User, email, ct);

                return new Response(
                    Base32Encoder.Encode(existingOAuth.User.Id),
                    existingOAuth.User.Email,
                    existingOAuth.User.Role.ToString());
            }

            var existingUser = await _db.Users
                .Include(u => u.OAuthAccounts)
                .FirstOrDefaultAsync(u => u.Email == email, ct);

            if (existingUser != null)
            {
                var newOAuth = new OAuthAccount
                {
                    UserId = existingUser.Id,
                    Provider = provider,
                    ProviderUserId = request.ProviderUserId,
                    ProviderEmail = email,
                    IsPrimary = false,
                    LinkedAt = DateTime.UtcNow
                };

                _db.OAuthAccounts.Add(newOAuth);
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Linked {Provider} OAuth account to existing user {UserId}",
                    provider, existingUser.Id);

                await PromoteToAdminIfConfigured(existingUser, email, ct);

                return new Response(
                    Base32Encoder.Encode(existingUser.Id),
                    existingUser.Email,
                    existingUser.Role.ToString());
            }

            var newUser = new User
            {
                Id = _idFactory.CreateId(),
                Email = email,
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(newUser);

            var primaryOAuth = new OAuthAccount
            {
                User = newUser,
                Provider = provider,
                ProviderUserId = request.ProviderUserId,
                ProviderEmail = email,
                IsPrimary = true,
                LinkedAt = DateTime.UtcNow
            };

            _db.OAuthAccounts.Add(primaryOAuth);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created new user {UserId} with {Provider} OAuth account",
                newUser.Id, provider);

            await PromoteToAdminIfConfigured(newUser, email, ct);

            return new Response(
                Base32Encoder.Encode(newUser.Id),
                newUser.Email,
                newUser.Role.ToString());
        }

        private async Task PromoteToAdminIfConfigured(User user, string email, CancellationToken ct)
        {
            var isAdminEmail = _usersOptions.AdminEmails.Any(adminEmail =>
                string.Equals(adminEmail, email, StringComparison.OrdinalIgnoreCase));

            if (isAdminEmail && user.Role != UserRole.Admin)
            {
                _logger.LogInformation(
                    "Promoting user {UserId} ({Email}) to Admin role based on configuration",
                    user.Id, email);

                user.Role = UserRole.Admin;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
