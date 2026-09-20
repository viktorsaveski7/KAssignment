using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Claims.Api.Common;

/// <summary>
/// Writes a health report as JSON so a failing probe says which dependency is down.
/// </summary>
/// <remarks>
/// Each entry carries its own description; exception detail is deliberately left out of the
/// response and goes to the log instead, for the same reason 500s are opaque.
/// </remarks>
public static class HealthCheckResponse
{
    /// <summary>Serialises the report to the response body.</summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds)
            })
        });
    }
}
