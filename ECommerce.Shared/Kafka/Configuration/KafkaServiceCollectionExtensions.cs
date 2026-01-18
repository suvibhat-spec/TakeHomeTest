using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Shared.Kafka.Producer;
using Microsoft.Extensions.Options;

namespace ECommerce.Shared.Kafka.Configuration
{
    public static class KafkaServiceCollectionExtensions
    {
        public static IServiceCollection AddKafkaProducer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
            services.AddSingleton<IKafkaProducer, KafkaProducerService>();
            return services;
        }

        public static IServiceCollection AddKafkaConsumer<TConsumer>(
            this IServiceCollection services)
            where TConsumer : class, Microsoft.Extensions.Hosting.IHostedService
        {
            services.AddHostedService<TConsumer>();
            return services;
        }
    }
}