using System.Runtime.InteropServices;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

namespace Claims.Api.Hosting;

/// <summary>Starts throwaway SQL Server and MongoDB containers so the API runs locally with no setup.</summary>
/// <remarks>
/// A development convenience, not part of the application. Switched off by setting
/// UseDevelopmentContainers to false, which is what a hosted deployment does: it then reads
/// ConnectionStrings:AuditDatabase and ConnectionStrings:ClaimsDatabase from ordinary configuration.
/// </remarks>
public sealed class DevelopmentContainers : IAsyncDisposable
{
    /// <summary>Configuration key that switches container bootstrapping on or off.</summary>
    public const string EnabledKey = "UseDevelopmentContainers";

    private readonly MsSqlContainer? _sqlContainer;
    private readonly MongoDbContainer? _mongoContainer;

    private DevelopmentContainers(MsSqlContainer? sqlContainer, MongoDbContainer? mongoContainer)
    {
        _sqlContainer = sqlContainer;
        _mongoContainer = mongoContainer;
    }

    /// <summary>Starts the containers when enabled and overlays their connection strings onto configuration.</summary>
    public static async Task<DevelopmentContainers> StartIfEnabledAsync(WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue(EnabledKey, false))
        {
            return new DevelopmentContainers(null, null);
        }

        // On Linux the default image is unavailable, so pin the published one.
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

    /// <inheritdoc />
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
