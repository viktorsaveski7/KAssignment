using Claims.Application.Common.Interfaces;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Claims.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var auditConnectionString = Require(configuration, "ConnectionStrings:AuditDatabase");
        var claimsConnectionString = Require(configuration, "ConnectionStrings:ClaimsDatabase");
        var claimsDatabaseName = Require(configuration, "MongoDb:DatabaseName");

        services.AddDbContext<AuditContext>(options => options.UseSqlServer(auditConnectionString));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(claimsConnectionString));

        services.AddDbContext<ClaimsContext>((provider, options) =>
            options.UseMongoDB(provider.GetRequiredService<IMongoClient>(), claimsDatabaseName));

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<ICoverRepository, CoverRepository>();

        services.AddSingleton<AuditQueue>();
        services.AddScoped<IAuditService, QueuedAuditService>();
        services.AddHostedService<AuditWriterService>();

        return services;
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Configuration value '{key}' is missing. The application cannot start without it.")
            : value;
    }
}
