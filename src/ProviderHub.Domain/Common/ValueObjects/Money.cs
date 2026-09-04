using System.Globalization;

namespace ProviderHub.Domain.Common.ValueObjects;

/// <summary>
/// An amount of money together with its currency.
/// <para>
/// The test only ever needs US dollars, yet an amount without a currency is a well known source
/// of expensive bugs: the moment a second currency appears, every bare <c>decimal</c> in the
/// codebase becomes ambiguous. Carrying the currency costs one field today and prevents that.
/// </para>
/// </summary>
public sealed record Money
{
    /// <summary>Currency of the hourly rates handled by this system.</summary>
    public const string UsDollar = "USD";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>ISO 4217 currency code.</summary>
    public string Currency { get; }

    /// <summary>Builds an amount in US dollars.</summary>
    public static Money Usd(decimal amount) => Create(amount, UsDollar);

    /// <exception cref="DomainException">The amount is negative or has more than two decimals.</exception>
    public static Money Create(decimal amount, string? currency)
    {
        var code = DomainGuard.RequiredText(currency, 3, "Currency").ToUpperInvariant();

        if (code.Length != 3 || !code.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException($"'{code}' is not a three-letter currency code.");
        }

        if (amount < 0)
        {
            throw new DomainException("Amount cannot be negative.");
        }

        // A decimal keeps track of its own scale, so 10.500 carries three decimals even though
        // it equals 10.50. Rounding first avoids rejecting a value that is in fact acceptable.
        var normalized = Math.Round(amount, 2, MidpointRounding.ToEven);

        return normalized != amount
            ? throw new DomainException("Amount cannot have more than two decimal places.")
            : new Money(normalized, code);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");
}
