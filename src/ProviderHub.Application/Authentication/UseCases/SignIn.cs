using FluentValidation;
using ProviderHub.Application.Abstractions.Authentication;
using ProviderHub.Application.Common;

namespace ProviderHub.Application.Authentication.UseCases;

/// <summary>Exchanges a username and password for an access token.</summary>
public sealed record SignInCommand(string UserName, string Password);

/// <summary>What a successful sign-in returns.</summary>
/// <param name="AccessToken">The token to send as <c>Authorization: Bearer &lt;token&gt;</c>.</param>
/// <param name="ExpiresAt">When the token stops being accepted.</param>
/// <param name="TokenType">Always <c>Bearer</c>, so a client does not have to assume it.</param>
public sealed record AccessTokenDto(string AccessToken, DateTimeOffset ExpiresAt, string TokenType = "Bearer");

public sealed class SignInValidator : AbstractValidator<SignInCommand>
{
    public SignInValidator()
    {
        // Only presence is checked. Telling a caller that a password is "too short to be ours"
        // is telling them something about the password they were not supposed to learn.
        RuleFor(command => command.UserName).NotEmpty();
        RuleFor(command => command.Password).NotEmpty();
    }
}

public sealed class SignInHandler(
    ICredentialVerifier credentials,
    IAccessTokenIssuer tokens,
    IValidator<SignInCommand> validator)
{
    public async Task<AccessTokenDto> HandleAsync(
        SignInCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        if (!credentials.Verify(command.UserName, command.Password))
        {
            // One message for both a wrong username and a wrong password. Distinguishing them
            // would turn the login form into a way of discovering which accounts exist.
            throw new InvalidCredentialsException();
        }

        var token = tokens.Issue(command.UserName);

        return new AccessTokenDto(token.Value, token.ExpiresAt);
    }
}
