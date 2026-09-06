namespace ProviderHub.Application.Abstractions.Authentication;

/// <summary>A signed token and the moment it stops being valid.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues access tokens for a signed-in user.
/// <para>
/// The application layer knows that signing in produces a token; it does not know that the token
/// is a JWT, which algorithm signs it or where the key lives. Swapping this for opaque tokens
/// backed by a session table would not change a single use case.
/// </para>
/// </summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(string userName);
}

/// <summary>
/// Checks a username and password against whatever holds the credentials.
/// <para>
/// The test states that no user administration is needed and that a default user may be defined,
/// so the only implementation reads one user from configuration. Behind this interface, replacing
/// it with a users table or an identity provider is an infrastructure change and nothing more.
/// </para>
/// </summary>
public interface ICredentialVerifier
{
    bool Verify(string? userName, string? password);
}
