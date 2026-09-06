using System.ComponentModel.DataAnnotations;

namespace ProviderHub.Infrastructure.Authentication;

/// <summary>
/// Everything the sign-in flow needs, read from configuration and validated at startup.
/// <para>
/// The attributes are not decoration: bound with <c>ValidateOnStart</c>, a deployment that
/// forgets the signing key fails immediately and loudly, instead of starting happily and issuing
/// tokens signed with an empty string.
/// </para>
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required]
    public string Issuer { get; set; } = "ProviderHub";

    [Required]
    public string Audience { get; set; } = "ProviderHub";

    /// <summary>
    /// Secret used to sign tokens. HMAC-SHA256 gives no more security than the key it is given,
    /// so anything shorter than the 256-bit digest weakens the signature; the minimum length is
    /// enforced rather than trusted.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "The signing key must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>The single user this system knows about, as the test allows.</summary>
    [Required]
    public string UserName { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash of that user's password, in the format produced by <see cref="PasswordHasher.Hash"/>.</summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
}
