using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BloodConnect.Api.Auth;

/// <summary>
/// Local development authentication scheme. The caller selects a demo identity in the frontend UI,
/// which sends it as the "X-Demo-User" header (an external id such as "demo-priya"). This handler
/// validates that value against the fixed, known set of demo external ids — the browser cannot submit
/// an arbitrary identity and have it treated as authoritative, because only ids present in
/// <see cref="DemoAuthenticationOptions.KnownExternalIds"/> (sourced from the same seed data used to
/// create demo users) are accepted, and the resulting claim is only ever a stable external id, never a
/// database primary key or profile data supplied by the client.
///
/// LIMITATION: this scheme provides no password, token expiry, or cryptographic verification of caller
/// identity — it is only appropriate for local development/demo scenarios on a trusted machine. In any
/// shared or production environment it must be replaced by real Entra ID (Azure AD) authentication
/// (JWT bearer tokens validated against your tenant) — see README "Entra ID / Azure configuration".
/// </summary>
public class DemoAuthenticationHandler : AuthenticationHandler<DemoAuthenticationOptions>
{
    public const string SchemeName = "DemoAuth";
    private const string HeaderName = "X-Demo-User";

    public DemoAuthenticationHandler(
        IOptionsMonitor<DemoAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing required '{HeaderName}' header."));
        }

        var externalId = headerValues.ToString();

        if (string.IsNullOrWhiteSpace(externalId) ||
            !Options.KnownExternalIds.Contains(externalId, StringComparer.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"'{externalId}' is not a recognized demo identity."));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, externalId) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
