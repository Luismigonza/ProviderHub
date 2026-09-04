using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.Contracts;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Providers;
using ProviderHub.Domain.Providers.ValueObjects;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>Registers a new provider. Services are attached afterwards, one offering at a time.</summary>
public sealed record CreateProviderCommand(string Nit, string Name, string Website, string Email);

public sealed class CreateProviderValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderValidator()
    {
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

public sealed class CreateProviderHandler(
    IProviderRepository providers,
    IUnitOfWork unitOfWork,
    IValidator<CreateProviderCommand> validator)
{
    public async Task<ProviderDetailsDto> HandleAsync(
        CreateProviderCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        var nit = Nit.Create(command.Nit);

        // Uniqueness is not an invariant the aggregate can enforce: a provider cannot see the
        // other providers. Rules that span aggregates are decided by the use case, backed by a
        // unique index in the database for the race that a check-then-act cannot cover.
        if (await providers.NitExistsAsync(nit, null, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"NIT '{nit.Value}' is already registered.");
        }

        var provider = Provider.Create(
            nit,
            command.Name,
            WebsiteUrl.Create(command.Website),
            EmailAddress.Create(command.Email));

        providers.Add(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ProviderDetailsDto.From(provider, []);
    }
}
