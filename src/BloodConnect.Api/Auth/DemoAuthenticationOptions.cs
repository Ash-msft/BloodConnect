using Microsoft.AspNetCore.Authentication;

namespace BloodConnect.Api.Auth;

/// <summary>
/// Options for <see cref="DemoAuthenticationHandler"/>: the fixed allow-list of demo external ids.
/// </summary>
public class DemoAuthenticationOptions : AuthenticationSchemeOptions
{
    public IReadOnlyCollection<string> KnownExternalIds { get; set; } = Array.Empty<string>();
}
