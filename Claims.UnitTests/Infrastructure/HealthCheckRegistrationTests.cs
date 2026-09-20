using Claims.Infrastructure;
using Claims.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Claims.UnitTests.Infrastructure;

/// <summary>
/// Exercises the registration itself rather than the individual checks: with nothing registered,
/// a health report is Healthy by default, which is precisely the failure mode being guarded against.
/// </summary>
public class HealthCheckRegistrationTests
{
    private static HealthCheckService ServiceForUnreachableDatabases()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Ports nothing is listening on, with short timeouts so the test stays quick.
                ["ConnectionStrings:AuditDatabase"] =
                    "Server=127.0.0.1,14330;Database=Claims;User Id=sa;Password=no;TrustServerCertificate=True;Connect Timeout=1",
                ["ConnectionStrings:ClaimsDatabase"] =
                    "mongodb://127.0.0.1:27099/?serverSelectionTimeoutMS=300&connectTimeoutMS=300",
                ["MongoDb:DatabaseName"] = "Claims"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider().GetRequiredService<HealthCheckService>();
    }

    [Fact]
    public async Task Both_databases_are_registered_as_readiness_checks()
    {
        var report = await ServiceForUnreachableDatabases().CheckHealthAsync();

        Assert.Contains(DependencyInjection.AuditDatabaseCheck, report.Entries.Keys);
        Assert.Contains(MongoHealthCheck.Name, report.Entries.Keys);
    }

    [Fact]
    public async Task The_report_is_unhealthy_when_neither_database_can_be_reached()
    {
        var report = await ServiceForUnreachableDatabases().CheckHealthAsync();

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.All(report.Entries, entry => Assert.Equal(HealthStatus.Unhealthy, entry.Value.Status));
    }

    [Fact]
    public async Task A_liveness_probe_excluding_readiness_checks_stays_healthy()
    {
        var report = await ServiceForUnreachableDatabases()
            .CheckHealthAsync(registration => !registration.Tags.Contains(DependencyInjection.ReadyTag));

        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Empty(report.Entries);
    }
}
