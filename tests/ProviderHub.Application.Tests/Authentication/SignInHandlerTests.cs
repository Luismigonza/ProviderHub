using FluentValidation;
using ProviderHub.Application.Abstractions.Authentication;
using ProviderHub.Application.Authentication.UseCases;
using ProviderHub.Application.Common;

namespace ProviderHub.Application.Tests.Authentication;

public class SignInHandlerTests
{
    private const string KnownUser = "admin";
    private const string KnownPassword = "Tekus2026!";

    [Fact]
    public async Task Correct_credentials_produce_a_token()
    {
        var result = await Handler().HandleAsync(new SignInCommand(KnownUser, KnownPassword));

        Assert.Equal("token-for-admin", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(KnownUser, "wrong-password")]
    [InlineData("someone-else", KnownPassword)]
    public async Task Wrong_credentials_are_refused(string userName, string password)
    {
        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            Handler().HandleAsync(new SignInCommand(userName, password)));
    }

    [Fact]
    public async Task A_wrong_user_and_a_wrong_password_fail_identically()
    {
        // Two different messages would let someone enumerate which usernames exist.
        var unknownUser = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            Handler().HandleAsync(new SignInCommand("someone-else", KnownPassword)));

        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            Handler().HandleAsync(new SignInCommand(KnownUser, "wrong-password")));

        Assert.Equal(unknownUser.Message, wrongPassword.Message);
    }

    [Theory]
    [InlineData("", KnownPassword)]
    [InlineData(KnownUser, "")]
    public async Task Empty_credentials_are_rejected_before_anything_is_checked(string userName, string password)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new SignInCommand(userName, password)));
    }

    private static SignInHandler Handler() =>
        new(new FakeCredentialVerifier(), new FakeTokenIssuer(), new SignInValidator());

    private sealed class FakeCredentialVerifier : ICredentialVerifier
    {
        public bool Verify(string? userName, string? password) =>
            userName == KnownUser && password == KnownPassword;
    }

    private sealed class FakeTokenIssuer : IAccessTokenIssuer
    {
        public AccessToken Issue(string userName) =>
            new($"token-for-{userName}", DateTimeOffset.UtcNow.AddHours(1));
    }
}
