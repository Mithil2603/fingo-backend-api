using Fingo.Shared.Configuration;
using Fingo.Shared.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fingo.Shared.Extensions
{
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.Configure<DatabaseOptions>(
                configuration.GetSection(DatabaseOptions.SectionName)
            );

            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            return services;
        }
    }
}