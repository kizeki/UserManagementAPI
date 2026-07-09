using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using UserManagementAPI.Models;

namespace UserManagementAPI.Middleware;

public sealed class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly string _expectedToken;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ApiTokenOptions tokenOptions)
        : base(options, logger, encoder, clock)
    {
        _expectedToken = tokenOptions.ExpectedToken;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing authorization header."));
        }

        var value = authorizationHeader.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid authorization scheme."));
        }

        var token = value["Bearer ".Length..].Trim();
        if (!string.Equals(token, _expectedToken, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "authenticated-user"),
            new Claim(ClaimTypes.Name, "api-user")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
