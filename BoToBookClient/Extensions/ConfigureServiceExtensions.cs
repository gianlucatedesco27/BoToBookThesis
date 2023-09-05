using BoToBookClient.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BoToBookClient.Extensions
{
    public static class ConfigureServiceExtensions
    {
        public static IServiceCollection AddChatbotServices(this IServiceCollection services)
        {
            services
                .AddScoped<IBoToBookWrapper, BoToBookWrapper>();
            return services;
        }
    }
}
