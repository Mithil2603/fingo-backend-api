using System.Data;

namespace Fingo.Shared.Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}