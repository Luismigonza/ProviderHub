using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>Changes the countries where an already offered service is available.</summary>
public sealed record ChangeOfferedCountriesCommand(int ProviderId, int ServiceId, IReadOnlyList<string> Countries);

public sealed class ChangeOfferedCountriesValidator : AbstractValidator<ChangeOfferedCountriesCommand>
{
    public ChangeOfferedCountriesValidator()
    {
        RuleFor(command => command.ProviderId).GreaterThan(0);
        RuleFor(command => command.ServiceId).GreaterThan(0);

        RuleFor(command => command.Countries)
            .NotEmpty()
            .WithMessage("A service must be offered in at least one country.");

        RuleForEach(command => command.Countries)
            .MustBuildDomainValue(country => CountryCode.Create(country));
    }
}

public sealed class ChangeOfferedCountriesHandler(
    IProviderRepository providers,
    IServiceRepository services,
    IUnitOfWork unitOfWork,
    IValidator<ChangeOfferedCountriesCommand> validator)
{
    public async Task<ProviderDetailsDto> HandleAsync(
        ChangeOfferedCountriesCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        var provider = await providers.FindAsync(command.ProviderId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Provider", command.ProviderId);

        if (!provider.Offers(command.ServiceId))
        {
            throw NotFoundException.For("Service offering", command.ServiceId);
        }

        provider.ChangeOfferedCountries(command.ServiceId, [.. command.Countries.Select(CountryCode.Create)]);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var offeredServices = await services
            .GetManyAsync([.. provider.Offerings.Select(offering => offering.ServiceId)], cancellationToken)
            .ConfigureAwait(false);

        return ProviderDetailsDto.From(provider, offeredServices);
    }
}
