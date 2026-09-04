using Microsoft.Extensions.DependencyInjection;
using ProviderHub.Application.Abstractions.Persistence;
using ProviderHub.Application.Providers.UseCases;
using ProviderHub.Application.Services.UseCases;
using ProviderHub.Application.Tests.TestDoubles;

namespace ProviderHub.Application.Tests;

/// <summary>
/// A forgotten registration in <see cref="DependencyInjection"/> does not break the build: it
/// breaks the first request that reaches the endpoint, in production. Resolving every handler
/// here turns that into a failing test instead.
/// </summary>
public class DependencyInjectionTests
{
    public static TheoryData<Type> Handlers =>
    [
        typeof(CreateProviderHandler),
        typeof(UpdateProviderHandler),
        typeof(GetProvidersHandler),
        typeof(GetProviderByIdHandler),
        typeof(OfferServiceHandler),
        typeof(ChangeOfferedCountriesHandler),
        typeof(WithdrawServiceHandler),
        typeof(CreateServiceHandler),
        typeof(UpdateServiceHandler),
        typeof(GetServicesHandler),
        typeof(GetServiceByIdHandler),
    ];

    [Theory]
    [MemberData(nameof(Handlers))]
    public void Every_use_case_can_be_resolved_with_all_its_dependencies(Type handlerType)
    {
        var services = new ServiceCollection();

        // The ports are filled with the in-memory doubles: the container does not care which
        // implementation it gets, which is exactly the property the architecture is after.
        services.AddSingleton<IProviderRepository, InMemoryProviderRepository>();
        services.AddSingleton<IServiceRepository, InMemoryServiceRepository>();
        services.AddSingleton<IUnitOfWork, RecordingUnitOfWork>();
        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(handlerType));
    }
}
