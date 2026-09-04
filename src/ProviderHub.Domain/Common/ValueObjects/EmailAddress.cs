using System.Net.Mail;

namespace ProviderHub.Domain.Common.ValueObjects;

/// <summary>
/// A valid, normalized e-mail address.
/// <para>
/// Modelling it as a type rather than as a <see cref="string"/> means an address can only exist
/// if it is valid: there is no way to build one that is not. Every method downstream that takes
/// an <see cref="EmailAddress"/> is therefore free of re-validation and of the classic mistake
/// of swapping two string parameters by accident.
/// </para>
/// </summary>
public sealed record EmailAddress
{
    /// <summary>Longest address accepted, matching the practical limit used by most mail systems.</summary>
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    /// <summary>The normalized address, lower-cased so that comparisons behave predictably.</summary>
    public string Value { get; }

    /// <exception cref="DomainException">The address is missing or malformed.</exception>
    public static EmailAddress Create(string? value)
    {
        var candidate = DomainGuard.RequiredText(value, MaxLength, "E-mail address");

        // Delegating to the framework's parser rather than to a hand-written regular
        // expression: RFC 5322 is far harder than it looks and this parser is battle-tested.
        if (!MailAddress.TryCreate(candidate, out var parsed) || parsed.Address != candidate)
        {
            throw new DomainException($"'{candidate}' is not a valid e-mail address.");
        }

        return new EmailAddress(candidate.ToLowerInvariant());
    }

    public override string ToString() => Value;
}
