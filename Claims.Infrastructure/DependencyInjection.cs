using Claims.Application.Common.Interfaces;
using Claims.Infrastructure.Auditing;
using Claims.Infrastructure.Persistence;
using Claims.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<ClaimsContext>(options =>
        {
            var client = new MongoClient(claimsConnectionString);
            options.UseMongoDB(client, claimsDatabaseName);
        });

        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<ICoverRepository, CoverRepository>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }

    private static string Require(IConfiguration configuration, string key) =>
        configuration[key]
        ?? throw new InvalidOperationException(
            $"Configuration value '{key}' is missing. The application cannot start without it.");
}
