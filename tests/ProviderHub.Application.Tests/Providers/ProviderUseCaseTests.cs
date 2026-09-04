using FluentValidation;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.UseCases;
using ProviderHub.Application.Tests.TestDoubles;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.Events;
using ProviderHub.Domain.Providers.ValueObjects;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Tests.Providers;

public class CreateProviderHandlerTests
{
    private readonly InMemoryProviderRepository _providers = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task A_provider_is_registered_with_no_offerings()
    {
        var result = await Handler().HandleAsync(new CreateProviderCommand(
            "890.903.938-8",
            "Importaciones Tekus S.A.",
            "https://tekus.co",
            "Contact@Tekus.co"));

        Assert.Equal("890903938-8", result.Nit);
        Assert.Equal("contact@tekus.co", result.Email);
        Assert.Empty(result.Offerings);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task A_nit_that_is_already_registered_is_a_conflict()
    {
        _providers.Seed(SampleProvider());

        await Assert.ThrowsAsync<ConflictException>(() => Handler().HandleAsync(new CreateProviderCommand(
            "890903938-8",
            "Another company",
            "https://another.co",
            "hello@another.co")));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData("890903938-1", "Name", "https://tekus.co", "a@tekus.co")]
    [InlineData("890903938-8", "", "https://tekus.co", "a@tekus.co")]
    [InlineData("890903938-8", "Name", "javascript:alert(1)", "a@tekus.co")]
    [InlineData("890903938-8", "Name", "https://tekus.co", "not-an-email")]
    public async Task Invalid_input_is_rejected_before_anything_is_stored(
        string nit,
        string name,
        string website,
        string email)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new CreateProviderCommand(nit, name, website, email)));

        Assert.Empty(_providers.Items);
    }

    [Fact]
    public async Task A_wrong_check_digit_is_reported_with_the_domain_message()
    {
        // The rule is written once, in the value object, and surfaces here as a field error.
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new CreateProviderCommand(
                "890903938-1",
                "Importaciones Tekus S.A.",
                "https://tekus.co",
                "contact@tekus.co")));

        Assert.Contains(
            exception.Errors,
            error => error.ErrorMessage.Contains("check digit", StringComparison.OrdinalIgnoreCase));
    }

    private CreateProviderHandler Handler() =>
        new(_providers, _unitOfWork, new CreateProviderValidator());

    internal static Provider SampleProvider() => Provider.Create(
        Nit.Create("890903938-8"),
        "Importaciones Tekus S.A.",
        WebsiteUrl.Create("https://tekus.co"),
        EmailAddress.Create("contact@tekus.co"));
}

public class OfferServiceHandlerTests
{
    private readonly InMemoryProviderRepository _providers = new();
    private readonly InMemoryServiceRepository _services = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task A_service_is_enabled_for_a_provider_in_the_given_countries()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        var result = await Handler().HandleAsync(
            new OfferServiceCommand(provider.Id, service.Id, ["CO", "mx"]));

        var offering = Assert.Single(result.Offerings);
        Assert.Equal("Space content download", offering.ServiceName);
        Assert.Equal(120m, offering.HourlyRate);
        Assert.Equal(["CO", "MX"], offering.Countries.Select(country => country.Code));
        Assert.Equal("Colombia", offering.Countries[0].Name);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Enabling_a_service_records_the_fact_that_triggers_the_notification()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        await Handler().HandleAsync(new OfferServiceCommand(provider.Id, service.Id, ["CO"]));

        Assert.Contains(provider.DomainEvents, domainEvent => domainEvent is ServiceOfferedDomainEvent);
    }

    [Fact]
    public async Task An_unknown_provider_is_not_found()
    {
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().HandleAsync(new OfferServiceCommand(404, service.Id, ["CO"])));
    }

    [Fact]
    public async Task An_unknown_service_is_not_found()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Handler().HandleAsync(new OfferServiceCommand(provider.Id, 404, ["CO"])));
    }

    [Fact]
    public async Task Offering_the_same_service_twice_is_a_conflict()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));
        await Handler().HandleAsync(new OfferServiceCommand(provider.Id, service.Id, ["CO"]));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Handler().HandleAsync(new OfferServiceCommand(provider.Id, service.Id, ["MX"])));
    }

    [Theory]
    [InlineData("ZZ")]
    [InlineData("COL")]
    [InlineData("")]
    public async Task An_invalid_country_is_rejected(string country)
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new OfferServiceCommand(provider.Id, service.Id, [country])));
    }

    [Fact]
    public async Task A_service_must_be_offered_somewhere()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));

        await Assert.ThrowsAsync<ValidationException>(() =>
            Handler().HandleAsync(new OfferServiceCommand(provider.Id, service.Id, [])));
    }

    private OfferServiceHandler Handler() =>
        new(_providers, _services, _unitOfWork, new OfferServiceValidator());
}

public class ProviderQueryTests
{
    private readonly InMemoryProviderRepository _providers = new();
    private readonly InMemoryServiceRepository _services = new();

    [Fact]
    public async Task Provider_details_resolve_the_name_and_rate_of_every_offered_service()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var download = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));
        var disappearance = _services.Seed(Service.Create("Forced byte disappearance", Money.Usd(80m)));
        provider.OfferService(download.Id, [CountryCode.Create("CO")]);
        provider.OfferService(disappearance.Id, [CountryCode.Create("PE")]);

        var result = await new GetProviderByIdHandler(_providers, _services)
            .HandleAsync(new GetProviderByIdQuery(provider.Id));

        Assert.Equal(2, result.Offerings.Count);
        Assert.Equal("Forced byte disappearance", result.Offerings[0].ServiceName);
        Assert.Equal("Space content download", result.Offerings[1].ServiceName);
    }

    [Fact]
    public async Task An_unknown_provider_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetProviderByIdHandler(_providers, _services).HandleAsync(new GetProviderByIdQuery(404)));
    }

    [Fact]
    public async Task A_listed_provider_reports_how_many_services_it_offers_and_where()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var download = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));
        var disappearance = _services.Seed(Service.Create("Forced byte disappearance", Money.Usd(80m)));
        provider.OfferService(download.Id, [CountryCode.Create("CO"), CountryCode.Create("MX")]);
        provider.OfferService(disappearance.Id, [CountryCode.Create("CO")]);

        var result = await new GetProvidersHandler(_providers, new GetProvidersValidator())
            .HandleAsync(new GetProvidersQuery(new PageRequest()));

        var listed = Assert.Single(result.Items);
        Assert.Equal(2, listed.OfferedServiceCount);
        Assert.Equal(["CO", "MX"], listed.Countries.Select(country => country.Code));
    }
}

public class WithdrawServiceHandlerTests
{
    private readonly InMemoryProviderRepository _providers = new();
    private readonly InMemoryServiceRepository _services = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task A_withdrawn_service_stops_being_offered_but_stays_in_the_catalogue()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());
        var service = _services.Seed(Service.Create("Space content download", Money.Usd(120m)));
        provider.OfferService(service.Id, [CountryCode.Create("CO")]);

        await new WithdrawServiceHandler(_providers, _unitOfWork, new WithdrawServiceValidator())
            .HandleAsync(new WithdrawServiceCommand(provider.Id, service.Id));

        Assert.Empty(provider.Offerings);
        Assert.Single(_services.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Withdrawing_something_that_is_not_offered_is_not_found()
    {
        var provider = _providers.Seed(CreateProviderHandlerTests.SampleProvider());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new WithdrawServiceHandler(_providers, _unitOfWork, new WithdrawServiceValidator())
                .HandleAsync(new WithdrawServiceCommand(provider.Id, 404)));
    }
}
