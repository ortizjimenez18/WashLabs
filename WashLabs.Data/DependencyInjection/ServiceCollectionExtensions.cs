using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WashLabs.Data.Context;

namespace WashLabs.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWashLabsData(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WashLabsDB");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:WashLabsDB using User Secrets in development " +
                "or the ConnectionStrings__WashLabsDB environment variable.");
        }

        services.AddDbContext<WashLabsDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(150)));

        return services;
    }
}
