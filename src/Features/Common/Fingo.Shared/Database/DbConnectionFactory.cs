using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using Fingo.Shared.Configuration;

namespace Fingo.Shared.Database
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly DatabaseOptions _databaseOptions;

        public DbConnectionFactory(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_databaseOptions.PostgresConnection);
        }
    }
}