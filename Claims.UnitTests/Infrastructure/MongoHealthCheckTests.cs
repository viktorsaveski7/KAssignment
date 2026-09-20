using Claims.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Claims.UnitTests.Infrastructure;

public class MongoHealthCheckTests
{
    private const string DatabaseName = "Claims";

    private static HealthCheckContext Context(HealthStatus failureStatus = HealthStatus.Unhealthy) => new()
    {
        Registration = new HealthCheckRegistration(
            MongoHealthCheck.Name,
            _ => Substitute.For<IHealthCheck>(),
            failureStatus,
            tags: null)
    };

    private static (MongoHealthCheck Check, IMongoDatabase Database) Create()
    {
        var client = Substitute.For<IMongoClient>();
        var database = Substitute.For<IMongoDatabase>();
        client.GetDatabase(DatabaseName, Arg.Any<MongoDatabaseSettings>()).Returns(database);

        return (new MongoHealthCheck(client, DatabaseName), database);
    }

    [Fact]
    public async Task Reports_healthy_when_the_ping_succeeds()
    {
        var (check, database) = Create();
        database
            .RunCommandAsync(
                Arg.Any<Command<BsonDocument>>(),
                Arg.Any<ReadPreference>(),
                Arg.Any<CancellationToken>())
            .Returns(new BsonDocument("ok", 1));

        var result = await check.CheckHealthAsync(Context());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Reports_unhealthy_when_the_database_cannot_be_reached()
    {
        var (check, database) = Create();
        database
            .RunCommandAsync(
                Arg.Any<Command<BsonDocument>>(),
                Arg.Any<ReadPreference>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("no server available"));

        var result = await check.CheckHealthAsync(Context());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Claims database is unreachable.", result.Description);
        Assert.IsType<TimeoutException>(result.Exception);
    }

    [Fact]
    public async Task Honours_the_configured_failure_status()
    {
        var (check, database) = Create();
        database
            .RunCommandAsync(
                Arg.Any<Command<BsonDocument>>(),
                Arg.Any<ReadPreference>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("no server available"));

        var result = await check.CheckHealthAsync(Context(HealthStatus.Degraded));

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }
}
