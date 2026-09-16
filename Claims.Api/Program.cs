using System.Reflection;
using System.Text.Json.Serialization;
using Claims.Api.Common;
using Claims.Api.Hosting;
using Claims.Application;
using Claims.Infrastructure;
using Claims.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

await using var developmentContainers = await DevelopmentContainers.StartIfEnabledAsync(builder);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();
    await auditContext.Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program;
