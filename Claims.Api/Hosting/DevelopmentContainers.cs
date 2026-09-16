using System.Runtime.InteropServices;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

namespace Claims.Api.Hosting;

public sealed class DevelopmentContainers : IAsyncDisposable
{
    public const string EnabledKey = "UseDevelopmentContainers";

    private readonly MsSqlContainer? _sqlContainer;
    private readonly MongoDbContainer? _mongoContainer;

    private DevelopmentContainers(MsSqlContainer? sqlContainer, MongoDbContainer? mongoContainer)
    {
        _sqlContainer = sqlContainer;
        _mongoContainer = mongoContainer;
    }

    public static async Task<DevelopmentContainers> StartIfEnabledAsync(WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue(EnabledKey, false))
        {
            return new DevelopmentContainers(null, null);
        }

        var sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                : new MsSqlBuilder())
            .Build();

        var mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .Build();

        await Task.WhenAll(sqlContainer.StartAsync(), mongoContainer.StartAsync());

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AuditDatabase"] = sqlContainer.GetConnectionString(),
            ["ConnectionStrings:ClaimsDatabase"] = mongoContainer.GetConnectionString()
        });

        return new DevelopmentContainers(sqlContainer, mongoContainer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }

        if (_mongoContainer is not null)
        {
            await _mongoContainer.DisposeAsync();
        }
    }
}
