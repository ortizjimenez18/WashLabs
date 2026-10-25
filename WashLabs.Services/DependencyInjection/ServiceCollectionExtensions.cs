using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WashLabs.Data.DependencyInjection;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Implementations;

namespace WashLabs.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWashLabsServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddWashLabsData(configuration);
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<IInicioService, InicioService>();
        services.AddScoped<IFacturaService, FacturaService>();
        services.AddScoped<IReservaService, ReservaService>();
        services.AddScoped<IOperacionService, OperacionService>();
        services.AddScoped<ICuentaService, CuentaService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        return services;
    }
}
