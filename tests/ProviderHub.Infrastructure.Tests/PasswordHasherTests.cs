using ProviderHub.Infrastructure.Authentication;

namespace ProviderHub.Infrastructure.Tests;

/// <summary>No database needed: this is pure cryptography.</summary>
public class PasswordHasherTests
{
    [Fact]
    public void A_password_verifies_against_its_own_hash()
    {
        var hash = PasswordHasher.Hash("Tekus2026!");

        Assert.True(PasswordHasher.Verify("Tekus2026!", hash));
    }

    [Fact]
    public void A_different_password_does_not()
    {
        var hash = PasswordHasher.Hash("Tekus2026!");

        Assert.False(PasswordHasher.Verify("tekus2026!", hash));
        Assert.False(PasswordHasher.Verify("Tekus2026", hash));
    }

    [Fact]
    public void The_same_password_hashes_differently_every_time()
    {
        // A random salt per hash: two people who chose the same password must not produce the
        // same stored value, or one leaked hash gives away every account that shares it.
        var first = PasswordHasher.Hash("Tekus2026!");
        var second = PasswordHasher.Hash("Tekus2026!");

        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("Tekus2026!", first));
        Assert.True(PasswordHasher.Verify("Tekus2026!", second));
    }

    [Fact]
    public void The_stored_hash_never_contains_the_password()
    {
        var hash = PasswordHasher.Hash("Tekus2026!");

        Assert.DoesNotContain("Tekus2026", hash, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_hash_records_the_iteration_count_it_was_made_with()
    {
        // Self-describing, so the cost can be raised later without invalidating older hashes.
        var parts = PasswordHasher.Hash("Tekus2026!").Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal(210_000, int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.def.ghi")]
    [InlineData("210000.notbase64!.alsonot!")]
    public void A_malformed_stored_hash_refuses_rather_than_throws(string? storedHash)
    {
        // Corrupted configuration must fail closed. Throwing here would turn a bad value into a
        // 500, and any behaviour other than "denied" would be worse.
        Assert.False(PasswordHasher.Verify("Tekus2026!", storedHash));
    }

    [Fact]
    public void An_empty_password_is_never_accepted()
    {
        var hash = PasswordHasher.Hash("Tekus2026!");

        Assert.False(PasswordHasher.Verify(string.Empty, hash));
        Assert.False(PasswordHasher.Verify(null, hash));
    }
}
