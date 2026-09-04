using System.Globalization;
using ProviderHub.Domain.Common;

namespace ProviderHub.Domain.Providers.ValueObjects;

/// <summary>
/// A Colombian tax identifier (<i>Numero de Identificacion Tributaria</i>): a base number
/// followed by a check digit, for example <c>890903938-8</c>.
/// <para>
/// The check digit is verified, not merely stored. A typo in a tax identifier is caught at the
/// door instead of surfacing months later as an invoice issued to the wrong company, and it is
/// exactly the kind of rule that belongs in the domain: it is true about every NIT, everywhere,
/// regardless of this application's screens or database.
/// </para>
/// </summary>
public sealed record Nit
{
    private const int MinBaseLength = 8;
    private const int MaxBaseLength = 15;

    /// <summary>
    /// Prime weights defined by the Colombian tax authority, applied to the digits of the base
    /// number from right to left.
    /// </summary>
    private static readonly int[] CheckDigitWeights =
        [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

    private Nit(string baseNumber, int checkDigit)
    {
        BaseNumber = baseNumber;
        CheckDigit = checkDigit;
    }

    /// <summary>The identifier without its check digit, for example <c>890903938</c>.</summary>
    public string BaseNumber { get; }

    /// <summary>The verification digit, a value between 0 and 10.</summary>
    public int CheckDigit { get; }

    /// <summary>The canonical representation, for example <c>890903938-8</c>.</summary>
    public string Value => string.Create(CultureInfo.InvariantCulture, $"{BaseNumber}-{CheckDigit}");

    /// <summary>
    /// Parses a NIT written in any of the usual formats: <c>890903938-8</c>,
    /// <c>890.903.938-8</c> or <c>890 903 938 - 8</c>.
    /// </summary>
    /// <exception cref="DomainException">The value is malformed or its check digit is wrong.</exception>
    public static Nit Create(string? value)
    {
        var candidate = DomainGuard.RequiredText(value, 32, "NIT")
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var separator = candidate.IndexOf('-', StringComparison.Ordinal);

        if (separator < 0)
        {
            throw new DomainException(
                $"'{candidate}' is missing the check digit, the expected format is 890903938-8.");
        }

        var baseNumber = candidate[..separator];
        var checkDigitText = candidate[(separator + 1)..];

        if (baseNumber.Length is < MinBaseLength or > MaxBaseLength || !baseNumber.All(char.IsAsciiDigit))
        {
            throw new DomainException(
                $"A NIT must start with {MinBaseLength} to {MaxBaseLength} digits, found '{baseNumber}'.");
        }

        if (!int.TryParse(checkDigitText, NumberStyles.None, CultureInfo.InvariantCulture, out var checkDigit)
            || checkDigit > 10)
        {
            throw new DomainException($"'{checkDigitText}' is not a valid check digit.");
        }

        var expected = CalculateCheckDigit(baseNumber);

        return checkDigit != expected
            ? throw new DomainException($"The check digit of NIT '{candidate}' is wrong, expected {expected}.")
            : new Nit(baseNumber, checkDigit);
    }

    /// <summary>
    /// Applies the official algorithm: weight each digit from right to left, add the products,
    /// then derive the digit from that sum modulo eleven.
    /// </summary>
    public static int CalculateCheckDigit(string baseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseNumber);

        var sum = 0;

        for (var position = 0; position < baseNumber.Length; position++)
        {
            var digit = baseNumber[^(position + 1)] - '0';
            sum += digit * CheckDigitWeights[position];
        }

        var remainder = sum % 11;

        return remainder < 2 ? remainder : 11 - remainder;
    }

    public override string ToString() => Value;
}
