using System.Reflection;
using System.Text.Json.Serialization;
using Claims.Api.Common;
using Claims.Api.Hosting;
using Claims.Application;
using Claims.Infrastructure;
using Claims.Infrastructure.Auditing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Local development only; a hosted deployment supplies connection strings through configuration.
await using var developmentContainers = await DevelopmentContainers.StartIfEnabledAsync(builder);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Expected failures already arrive as Result values; this covers everything else.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Claims API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


var app = builder.Build();

// Hosted behind a reverse proxy that terminates TLS, so the original scheme and caller address
// arrive as headers. Without this the app sees plain HTTP and HTTPS redirection loops.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseExceptionHandler();

// On in development, and switchable on elsewhere so a hosted demo can expose its own API surface.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
// Readiness: can this instance actually serve requests? Checks both databases.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(Claims.Infrastructure.DependencyInjection.ReadyTag),
    ResponseWriter = HealthCheckResponse.WriteAsync
});

// Liveness: is the process up? Deliberately checks nothing, so a database outage takes the
// instance out of rotation rather than getting it restarted.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponse.WriteAsync
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();
    await auditContext.Database.MigrateAsync();
}

await app.RunAsync();

/// <summary>Exposed so that WebApplicationFactory&lt;Program&gt; can bootstrap the API in tests.</summary>
public partial class Program;
