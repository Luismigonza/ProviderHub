using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;
using ProviderHub.Domain.Common.ValueObjects;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>
/// Enables a catalogue service for a provider in a set of countries. This is the operation the
/// test describes as "a provider has enabled a new service", and the one that will trigger the
/// notification e-mail.
/// </summary>
public sealed record OfferServiceCommand(int ProviderId, int ServiceId, IReadOnlyList<string> Countries);

public sealed class OfferServiceValidator : AbstractValidator<OfferServiceCommand>
{
    public OfferServiceValidator()
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

public sealed class OfferServiceHandler(
    IProviderRepository providers,
    IServiceRepository services,
    IUnitOfWork unitOfWork,
    IValidator<OfferServiceCommand> validator)
{
    public async Task<ProviderDetailsDto> HandleAsync(
        OfferServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        var provider = await providers.FindAsync(command.ProviderId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Provider", command.ProviderId);

        if (!await services.ExistsAsync(command.ServiceId, cancellationToken).ConfigureAwait(false))
        {
            throw NotFoundException.For("Service", command.ServiceId);
        }

        // The aggregate would reject this too. Asking first turns what would surface as an
        // unhandled rule violation into a deliberate 409 with a message the caller can act on.
        if (provider.Offers(command.ServiceId))
        {
            throw new ConflictException($"This provider already offers service {command.ServiceId}.");
        }

        provider.OfferService(command.ServiceId, [.. command.Countries.Select(CountryCode.Create)]);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var offeredServices = await services
            .GetManyAsync([.. provider.Offerings.Select(offering => offering.ServiceId)], cancellationToken)
            .ConfigureAwait(false);

        return ProviderDetailsDto.From(provider, offeredServices);
    }
}
