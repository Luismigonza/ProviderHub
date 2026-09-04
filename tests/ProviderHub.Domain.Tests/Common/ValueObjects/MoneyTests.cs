using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Domain.Tests.Common.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Usd_builds_an_amount_in_dollars()
    {
        var rate = Money.Usd(42.50m);

        Assert.Equal(42.50m, rate.Amount);
        Assert.Equal("USD", rate.Currency);
    }

    [Fact]
    public void Zero_is_accepted_because_a_free_service_is_a_valid_offer()
    {
        var rate = Money.Usd(0m);

        Assert.Equal(0m, rate.Amount);
    }

    [Fact]
    public void A_negative_amount_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(() => Money.Usd(-0.01m));

        Assert.Contains("negative", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void More_than_two_decimals_is_rejected()
    {
        Assert.Throws<DomainException>(() => Money.Usd(10.005m));
    }

    [Fact]
    public void Trailing_zeros_do_not_count_as_extra_decimals()
    {
        var rate = Money.Usd(10.500m);

        Assert.Equal(10.50m, rate.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("US1")]
    public void An_invalid_currency_code_is_rejected(string currency)
    {
        Assert.Throws<DomainException>(() => Money.Create(10m, currency));
    }

    [Fact]
    public void Two_amounts_with_the_same_value_are_equal()
    {
        // Value objects have no identity: what they are worth is what they are.
        Assert.Equal(Money.Usd(15m), Money.Usd(15m));
        Assert.NotEqual(Money.Usd(15m), Money.Create(15m, "COP"));
    }

    [Fact]
    public void ToString_is_culture_independent()
    {
        Assert.Equal("1500.00 USD", Money.Usd(1500m).ToString());
    }
}
