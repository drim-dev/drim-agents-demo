using System.Security.Claims;
using System.Text.Encodings.Web;
using DrimAgents.Api.Common.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DrimAgents.Api.Common.Auth;

/// <summary>
/// SECURITY WARNING: This API must NOT be exposed to the public internet.
/// Only the trusted BFF layer should be able to send requests to this API.
/// Network isolation (private VPC, Kubernetes cluster, etc.) is required.
/// </summary>
public class BffHeaderAuthenticationHandler : AuthenticationHandler<BffHeaderAuthenticationOptions>
{
    public const string SchemeName = "BffHeaderAuth";

    public BffHeaderAuthenticationHandler(
        IOptionsMonitor<BffHeaderAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userIdString = userIdHeader.ToString();

        if (!Base32Encoder.TryDecode(userIdString, out var userId) && !long.TryParse(userIdString, out userId))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"Invalid X-User-Id header: '{userIdString}' is neither a valid Base32 ID nor a long"));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (Request.Headers.TryGetValue("X-User-Email", out var email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
        }

        if (Request.Headers.TryGetValue("X-User-Role", out var roles))
        {
            var roleValues = roles.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            foreach (var role in roleValues)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class BffHeaderAuthenticationOptions : AuthenticationSchemeOptions
{
}
