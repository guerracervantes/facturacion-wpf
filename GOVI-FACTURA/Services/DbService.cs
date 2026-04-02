using System.Configuration;
using Microsoft.Data.SqlClient;

namespace GOVI_FACTURA.Services
{
    public class DbService
    {
        private string connectionString;

        public DbService()
        {
            connectionString = ConfigurationManager
                .ConnectionStrings["cnSucursal"]
                .ConnectionString;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
