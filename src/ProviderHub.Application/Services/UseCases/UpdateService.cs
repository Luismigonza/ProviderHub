using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;
using ProviderHub.Application.Services.Contracts;
using ProviderHub.Domain.Common.ValueObjects;
using ProviderHub.Domain.Services;

namespace ProviderHub.Application.Services.UseCases;

/// <summary>Edits the name or the hourly rate of a catalogue service.</summary>
public sealed record UpdateServiceCommand(int Id, string Name, decimal HourlyRate);

public sealed class UpdateServiceValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceValidator()
    {
        RuleFor(command => command.Id).GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Service.NameMaxLength);

        RuleFor(command => command.HourlyRate)
            .MustBuildDomainValue(rate => Money.Usd(rate));
    }
}

public sealed class UpdateServiceHandler(
    IServiceRepository services,
    IUnitOfWork unitOfWork,
    IValidator<UpdateServiceCommand> validator)
{
    public async Task<ServiceDto> HandleAsync(
        UpdateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        var service = await services.FindAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Service", command.Id);

        // Excluding the service being edited: saving it under its own name is not a duplicate.
        if (await services.NameExistsAsync(command.Name, command.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException($"A service named '{command.Name}' already exists.");
        }

        service.Rename(command.Name);
        service.ChangeHourlyRate(Money.Usd(command.HourlyRate));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ServiceDto.From(service);
    }
}
