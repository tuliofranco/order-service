using Microsoft.Extensions.DependencyInjection;
using Order.Ia.Application.Services;

namespace Order.Ia.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IAService>();
        services.AddSingleton<IAHistoryService>();
        return services;
    }
}
