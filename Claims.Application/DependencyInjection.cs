using System.Reflection;
using Claims.Domain.Premium;
using Microsoft.Extensions.DependencyInjection;

namespace Claims.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));

        services.AddSingleton<IPremiumCalculator, PremiumCalculator>();

        return services;
    }
}
