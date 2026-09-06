using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProviderHub.Application.Abstractions.Authentication;

namespace ProviderHub.Infrastructure.Authentication;

/// <summary>Issues signed JSON Web Tokens.</summary>
internal sealed class JwtAccessTokenIssuer(IOptions<JwtAuthenticationOptions> options) : IAccessTokenIssuer
{
    private readonly JwtAuthenticationOptions _options = options.Value;

    public AccessToken Issue(string userName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt.UtcDateTime,

            // A JWT is signed, not encrypted: anyone holding it can read these claims. Nothing
            // secret goes in here, only who the caller is.
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.Name, userName),

                // A unique token id, so a token could be revoked individually if this ever grows
                // a deny list.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),

            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}

/// <summary>
/// Checks credentials against the single user defined in configuration, which is what the test
/// asks for: an authentication mechanism, without any user administration.
/// </summary>
internal sealed class ConfiguredCredentialVerifier(IOptions<JwtAuthenticationOptions> options) : ICredentialVerifier
{
    private readonly JwtAuthenticationOptions _options = options.Value;

    public bool Verify(string? userName, string? password)
    {
        // The password is verified even when the username is already wrong, so that a rejection
        // takes the same time either way. Returning early here would let an attacker tell valid
        // usernames from invalid ones with a stopwatch.
        var userMatches = string.Equals(userName, _options.UserName, StringComparison.OrdinalIgnoreCase);
        var passwordMatches = PasswordHasher.Verify(password, _options.PasswordHash);

        return userMatches && passwordMatches;
    }
}
