using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Tests.Common.ValueObjects;

public class CountryCodeTests
{
    [Fact]
    public void A_code_is_normalized_to_upper_case()
    {
        Assert.Equal("CO", CountryCode.Create(" co ").Value);
    }

    [Fact]
    public void A_code_resolves_its_country_name()
    {
        Assert.Equal("Colombia", CountryCode.Create("CO").DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C")]
    [InlineData("COL")]
    [InlineData("C1")]
    [InlineData("ZZ")]
    public void An_invalid_or_unknown_code_is_rejected(string? value)
    {
        Assert.Throws<DomainException>(() => CountryCode.Create(value));
    }

    [Fact]
    public void The_same_country_written_differently_is_the_same_country()
    {
        // This is what keeps the country indicators of the summary endpoint trustworthy.
        Assert.Equal(CountryCode.Create("co"), CountryCode.Create("CO"));
    }
}
