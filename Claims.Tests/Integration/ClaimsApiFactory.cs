using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Claims.Infrastructure.Auditing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

namespace Claims.Tests.Integration;

/// <summary>Boots the API against throwaway SQL Server and MongoDB containers, started once per run.</summary>
public class ClaimsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "ClaimsIntegrationTests";

    private readonly MsSqlContainer _sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            : new MsSqlBuilder())
        .Build();

    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:latest")
        .Build();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _mongoContainer.StartAsync());

        // The API resolves its configuration inside Program before the host is built, so
        // WebApplicationFactory's configuration hooks run too late to influence it.
        // Environment variables are read by WebApplication.CreateBuilder itself.
        Environment.SetEnvironmentVariable("UseDevelopmentContainers", "false");
        Environment.SetEnvironmentVariable("ConnectionStrings__AuditDatabase", _sqlContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__ClaimsDatabase", _mongoContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("MongoDb__DatabaseName", DatabaseName);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        await _mongoContainer.DisposeAsync();

        Environment.SetEnvironmentVariable("UseDevelopmentContainers", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__AuditDatabase", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__ClaimsDatabase", null);
        Environment.SetEnvironmentVariable("MongoDb__DatabaseName", null);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public async Task ResetAsync()
    {
        var client = Services.GetRequiredService<IMongoClient>();
        var database = client.GetDatabase(DatabaseName);

        await database.DropCollectionAsync("claims");
        await database.DropCollectionAsync("covers");
    }

    public async Task<List<TAudit>> WaitForAuditsAsync<TAudit>(
        Func<AuditContext, IQueryable<TAudit>> query,
        int expected,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        List<TAudit> audits;

        while (true)
        {
            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
                audits = await query(context).ToListAsync();
            }

            if (audits.Count >= expected || DateTime.UtcNow >= deadline)
            {
                return audits;
            }

            await Task.Delay(100);
        }
    }

    public async Task<int> CountAuditsAsync<TAudit>(Func<AuditContext, IQueryable<TAudit>> query)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        return await query(context).CountAsync();
    }
}

[CollectionDefinition(ApiCollection.Name)]
public class ApiCollection : ICollectionFixture<ClaimsApiFactory>
{
    public const string Name = "Claims API";
}
