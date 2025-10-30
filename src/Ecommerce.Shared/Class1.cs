using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Shared.Services;

namespace Ecommerce.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        
        return services;
    }
}
