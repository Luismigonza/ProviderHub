using FluentValidation;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Common;

namespace ProviderHub.Application.Providers.UseCases;

/// <summary>Stops a provider from offering a service. The catalogue entry itself is untouched.</summary>
public sealed record WithdrawServiceCommand(int ProviderId, int ServiceId);

public sealed class WithdrawServiceValidator : AbstractValidator<WithdrawServiceCommand>
{
    public WithdrawServiceValidator()
    {
        RuleFor(command => command.ProviderId).GreaterThan(0);
        RuleFor(command => command.ServiceId).GreaterThan(0);
    }
}

public sealed class WithdrawServiceHandler(
    IProviderRepository providers,
    IUnitOfWork unitOfWork,
    IValidator<WithdrawServiceCommand> validator)
{
    public async Task HandleAsync(
        WithdrawServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken).ConfigureAwait(false);

        var provider = await providers.FindAsync(command.ProviderId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Provider", command.ProviderId);

        if (!provider.Offers(command.ServiceId))
        {
            throw NotFoundException.For("Service offering", command.ServiceId);
        }

        provider.WithdrawService(command.ServiceId);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
