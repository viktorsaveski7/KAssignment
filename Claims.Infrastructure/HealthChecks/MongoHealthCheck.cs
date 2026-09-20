using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Claims.Infrastructure.HealthChecks;

/// <summary>
/// Reports whether the claims database is reachable by issuing a <c>ping</c> command.
/// </summary>
/// <remarks>
/// A ping rather than a query: it proves the connection works without depending on any collection
/// existing or holding data.
/// </remarks>
public sealed class MongoHealthCheck : IHealthCheck
{
    /// <summary>Name this check is registered under.</summary>
    public const string Name = "claims-database";

    private readonly IMongoClient _client;
    private readonly string _databaseName;

    /// <summary>Creates the check.</summary>
    public MongoHealthCheck(IMongoClient client, string databaseName)
    {
        _client = client;
        _databaseName = databaseName;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument("ping", 1));

            await _client
                .GetDatabase(_databaseName)
                .RunCommandAsync(command, cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Claims database is reachable.");
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Claims database is unreachable.",
                exception);
        }
    }
}
