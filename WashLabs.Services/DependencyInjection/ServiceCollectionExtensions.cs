using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WashLabs.Data.DependencyInjection;

namespace WashLabs.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWashLabsServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddWashLabsData(configuration);
        return services;
    }
}
