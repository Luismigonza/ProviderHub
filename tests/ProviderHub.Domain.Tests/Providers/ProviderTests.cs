using ProviderHub.Domain.Common;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.Events;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Domain.Tests.Providers;

public class ProviderTests
{
    private const int SpaceContentDownload = 1;
    private const int ForcedByteDisappearance = 2;

    [Fact]
    public void A_provider_is_created_with_no_offerings_and_no_identity_yet()
    {
        var provider = CreateProvider();

        Assert.Empty(provider.Offerings);
        Assert.True(provider.IsTransient);
    }

    [Fact]
    public void The_provider_name_is_trimmed()
    {
        var provider = Provider.Create(
            Nit.Create("890903938-8"),
            "  Importaciones Tekus S.A.  ",
            WebsiteUrl.Create("https://tekus.co"),
            EmailAddress.Create("contact@tekus.co"));

        Assert.Equal("Importaciones Tekus S.A.", provider.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_provider_without_a_name_is_rejected(string? name)
    {
        Assert.Throws<DomainException>(() => Provider.Create(
            Nit.Create("890903938-8"),
            name,
            WebsiteUrl.Create("https://tekus.co"),
            EmailAddress.Create("contact@tekus.co")));
    }

    [Fact]
    public void Offering_a_service_records_where_it_is_offered()
    {
        var provider = CreateProvider();

        var offering = provider.OfferService(SpaceContentDownload, Countries("CO", "MX"));

        Assert.Single(provider.Offerings);
        Assert.Equal(SpaceContentDownload, offering.ServiceId);
        Assert.Equal(["CO", "MX"], offering.Countries.Select(country => country.Value));
    }

    [Fact]
    public void Offering_a_service_announces_it_to_whoever_is_listening()
    {
        // The e-mail required by the test is triggered by this event, not by the domain itself.
        var provider = CreateProvider();

        provider.OfferService(SpaceContentDownload, Countries("CO"));

        var announced = Assert.Single(provider.DomainEvents);
        var serviceOffered = Assert.IsType<ServiceOfferedDomainEvent>(announced);
        Assert.Same(provider, serviceOffered.Provider);
        Assert.Equal(SpaceContentDownload, serviceOffered.ServiceId);
    }

    [Fact]
    public void The_same_service_cannot_be_offered_twice()
    {
        var provider = CreateProvider();
        provider.OfferService(SpaceContentDownload, Countries("CO"));

        Assert.Throws<DomainException>(() => provider.OfferService(SpaceContentDownload, Countries("MX")));
    }

    [Fact]
    public void A_service_must_be_offered_in_at_least_one_country()
    {
        var provider = CreateProvider();

        Assert.Throws<DomainException>(() => provider.OfferService(SpaceContentDownload, []));
    }

    [Fact]
    public void A_repeated_country_is_only_counted_once()
    {
        var provider = CreateProvider();

        var offering = provider.OfferService(SpaceContentDownload, Countries("CO", "co", "CO"));

        Assert.Equal(["CO"], offering.Countries.Select(country => country.Value));
    }

    [Fact]
    public void An_offering_must_point_at_a_persisted_service()
    {
        var provider = CreateProvider();

        Assert.Throws<DomainException>(() => provider.OfferService(0, Countries("CO")));
    }

    [Fact]
    public void The_countries_of_an_offering_can_be_replaced()
    {
        var provider = CreateProvider();
        provider.OfferService(SpaceContentDownload, Countries("CO"));

        provider.ChangeOfferedCountries(SpaceContentDownload, Countries("MX", "PE"));

        var offering = Assert.Single(provider.Offerings);
        Assert.Equal(["MX", "PE"], offering.Countries.Select(country => country.Value));
    }

    [Fact]
    public void A_service_can_be_withdrawn()
    {
        var provider = CreateProvider();
        provider.OfferService(SpaceContentDownload, Countries("CO"));
        provider.OfferService(ForcedByteDisappearance, Countries("PE"));

        provider.WithdrawService(SpaceContentDownload);

        Assert.False(provider.Offers(SpaceContentDownload));
        Assert.True(provider.Offers(ForcedByteDisappearance));
    }

    [Fact]
    public void Withdrawing_a_service_that_is_not_offered_is_rejected()
    {
        var provider = CreateProvider();

        Assert.Throws<DomainException>(() => provider.WithdrawService(SpaceContentDownload));
    }

    [Fact]
    public void Contact_details_can_be_updated()
    {
        var provider = CreateProvider();

        provider.ChangeContactDetails(
            WebsiteUrl.Create("https://new.tekus.co"),
            EmailAddress.Create("new@tekus.co"));

        Assert.Equal("new@tekus.co", provider.Email.Value);
        Assert.Equal("https://new.tekus.co/", provider.Website.Value);
    }

    private static Provider CreateProvider() => Provider.Create(
        Nit.Create("890903938-8"),
        "Importaciones Tekus S.A.",
        WebsiteUrl.Create("https://tekus.co"),
        EmailAddress.Create("contact@tekus.co"));

    private static CountryCode[] Countries(params string[] codes) =>
        [.. codes.Select(CountryCode.Create)];
}
