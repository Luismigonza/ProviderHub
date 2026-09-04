using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>Edits the identifying and contact details of a provider.</summary>
public sealed record UpdateProviderCommand(int Id, string Nit, string Name, string Website, string Email);

public sealed class UpdateProviderValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderValidator()
    {
        RuleFor(command => command.Id).GreaterThan(0);

        RuleFor(command => command.Nit)
            .NotEmpty()
            .MustBuildDomainValue(nit => Nit.Create(nit));

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Provider.NameMaxLength);

        RuleFor(command => command.Website)
            .NotEmpty()
            .MustBuildDomainValue(website => WebsiteUrl.Create(website));

        RuleFor(command => command.Email)
            .NotEmpty()
            .MustBuildDomainValue(email => EmailAddress.Create(email));
    }
}

public sealed class UpdateProviderHandler(
    IProviderRepository providers,
    IServiceRepository services,
    IUnitOfWork unitOfWork,
    IValidator<UpdateProviderCommand> validator)
{
    public async Task<ProviderDetailsDto> HandleAsync(
        UpdateProviderCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        var provider = await providers.FindAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Provider", command.Id);

        var nit = Nit.Create(command.Nit);

        if (await providers.NitExistsAsync(nit, command.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"NIT '{nit.Value}' is already registered.");
        }

        provider.ChangeNit(nit);
        provider.Rename(command.Name);
        provider.ChangeContactDetails(
            WebsiteUrl.Create(command.Website),
            EmailAddress.Create(command.Email));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var offeredServices = await services
            .GetManyAsync([.. provider.Offerings.Select(offering => offering.ServiceId)], cancellationToken)
            .ConfigureAwait(false);

        return ProviderDetailsDto.From(provider, offeredServices);
    }
}
