using Microsoft.Extensions.DependencyInjection;

namespace Order.Ia.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IAService>();
        return services;
    }
}
