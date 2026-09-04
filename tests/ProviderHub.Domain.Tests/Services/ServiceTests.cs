using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Services;

namespace ProviderHub.Domain.Tests.Services;

public class ServiceTests
{
    [Fact]
    public void A_service_is_created_with_a_name_and_an_hourly_rate()
    {
        var service = Service.Create("  Space content download  ", Money.Usd(120m));

        Assert.Equal("Space content download", service.Name);
        Assert.Equal(Money.Usd(120m), service.HourlyRate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_service_without_a_name_is_rejected(string? name)
    {
        Assert.Throws<DomainException>(() => Service.Create(name, Money.Usd(120m)));
    }

    [Fact]
    public void A_name_longer_than_the_limit_is_rejected()
    {
        var tooLong = new string('a', Service.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => Service.Create(tooLong, Money.Usd(120m)));
    }

    [Fact]
    public void A_service_can_be_renamed()
    {
        var service = Service.Create("Space content download", Money.Usd(120m));

        service.Rename("Orbital content download");

        Assert.Equal("Orbital content download", service.Name);
    }

    [Fact]
    public void The_hourly_rate_can_be_changed()
    {
        var service = Service.Create("Space content download", Money.Usd(120m));

        service.ChangeHourlyRate(Money.Usd(150.75m));

        Assert.Equal(150.75m, service.HourlyRate.Amount);
    }
}
