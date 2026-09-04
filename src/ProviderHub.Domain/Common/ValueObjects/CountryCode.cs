using System.Globalization;

namespace ProviderHub.Domain.Common.ValueObjects;

/// <summary>
/// An ISO 3166-1 alpha-2 country code, such as <c>CO</c> or <c>MX</c>.
/// <para>
/// The test asks for indicators broken down by country but never states where the country
/// lives, so this project models it on the offering: a provider offers a given service in a
/// given set of countries. Storing the standardized code instead of a free-text country name
/// keeps the aggregation reliable, since "Colombia", "colombia" and "COL" would otherwise be
/// three different countries in the summary.
/// </para>
/// </summary>
public sealed record CountryCode
{
    private CountryCode(string value) => Value = value;

    /// <summary>The upper-cased two-letter code.</summary>
    public string Value { get; }

    /// <summary>The country name in English, resolved from the code for display purposes.</summary>
    public string DisplayName => new RegionInfo(Value).EnglishName;

    /// <exception cref="DomainException">The code is missing or is not a known country.</exception>
    public static CountryCode Create(string? value)
    {
        var candidate = DomainGuard.RequiredText(value, 2, "Country code").ToUpperInvariant();

        if (candidate.Length != 2 || !candidate.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException($"'{candidate}' is not a two-letter country code.");
        }

        try
        {
            // RegionInfo is backed by the ICU data shipped with .NET, so the list of valid
            // countries stays up to date without this project maintaining one by hand.
            _ = new RegionInfo(candidate);
        }
        catch (ArgumentException exception)
        {
            throw new DomainException($"'{candidate}' is not a known ISO 3166-1 country code.", exception);
        }

        return new CountryCode(candidate);
    }

    public override string ToString() => Value;
}
