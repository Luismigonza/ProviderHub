using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Services.Contracts;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Services.UseCases;

/// <summary>Adds a service to the catalogue.</summary>
public sealed record CreateServiceCommand(string Name, decimal HourlyRate);

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Service.NameMaxLength);

        RuleFor(command => command.HourlyRate)
            .MustBuildDomainValue(rate => Money.Usd(rate));
    }
}

/// <summary>
/// One use case, one class. The name of the class is the name of the operation, so the list of
/// files in this folder reads as the list of things the system can do.
/// </summary>
public sealed class CreateServiceHandler(
    IServiceRepository services,
    IUnitOfWork unitOfWork,
    IValidator<CreateServiceCommand> validator)
{
    public async Task<ServiceDto> HandleAsync(
        CreateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        if (await services.NameExistsAsync(command.Name, null, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A service named '{command.Name}' already exists.");
        }

        var service = Service.Create(command.Name, Money.Usd(command.HourlyRate));

        services.Add(service);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServiceDto.From(service);
    }
}
