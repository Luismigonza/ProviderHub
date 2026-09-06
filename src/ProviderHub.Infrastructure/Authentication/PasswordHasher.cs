using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ProviderHub.Infrastructure.Authentication;

/// <summary>
/// Hashes and verifies passwords with PBKDF2-HMAC-SHA256.
/// <para>
/// The password is never stored, not even in a configuration file that only ever runs locally:
/// what is stored is a hash that cannot be turned back into it. Three properties make that hash
/// worth having, and all three are easy to get wrong:
/// </para>
/// <list type="bullet">
/// <item>A random salt per password, so two people who chose the same password do not produce
/// the same hash, and a precomputed table is useless.</item>
/// <item>A high iteration count, so guessing is slow. Fast hashes like SHA-256 on its own are
/// the wrong tool here precisely because they are fast.</item>
/// <item>A constant-time comparison, so the time a rejection takes reveals nothing about how
/// many leading bytes were correct.</item>
/// </list>
/// </summary>
public static class PasswordHasher
{
    /// <summary>OWASP's recommended minimum for PBKDF2-HMAC-SHA256.</summary>
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>
    /// Produces a self-describing hash, <c>iterations.salt.hash</c>, so the iteration count can
    /// be raised later without invalidating hashes that were created under the old one.
    /// </summary>
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}");
    }

    /// <summary>Checks a candidate password against a stored hash.</summary>
    public static bool Verify(string? password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.');

        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(password, salt, iterations, expected.Length);

        // Not SequenceEqual: that returns as soon as two bytes differ, and the time it took
        // says how far the guess got.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int size = HashSize) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            size);
}
