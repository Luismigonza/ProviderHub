using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Application.Common;
using ProviderHub.Application.Providers.UseCases;
using ProviderHub.Application.Services.UseCases;

namespace ProviderHub.Application;

/// <summary>
/// Registers everything this layer offers, so that the API only has to call one method and does
/// not need to know the name of a single handler.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Validators are discovered by scanning: one per command, always found the same way.
        // PageRequestValidator is skipped: it takes the set of sortable fields as a constructor
        // argument, so it is built by the list validators that know their own fields, never
        // resolved from the container.
        services.AddValidatorsFromAssemblyContaining<CreateProviderCommand>(
            filter: result => result.ValidatorType != typeof(PageRequestValidator));

        // Handlers are registered by hand. There are eleven of them and the list doubles as an
        // inventory of what the system can do; assembly scanning would hide that behind magic.
        services.AddScoped<CreateProviderHandler>();
        services.AddScoped<UpdateProviderHandler>();
        services.AddScoped<GetProvidersHandler>();
        services.AddScoped<GetProviderByIdHandler>();
        services.AddScoped<OfferServiceHandler>();
        services.AddScoped<ChangeOfferedCountriesHandler>();
        services.AddScoped<WithdrawServiceHandler>();

        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<UpdateServiceHandler>();
        services.AddScoped<GetServicesHandler>();
        services.AddScoped<GetServiceByIdHandler>();

        return services;
    }
}
