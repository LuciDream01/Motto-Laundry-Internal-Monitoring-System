
using Microsoft.Data.SqlClient;

namespace MottoLaundry.Data
{
    public class DatabaseConnection
    {
        // Connection to LocalDB: DraftMottoDB
        private static readonly string connectionString =
            @"Server=(localdb)\MSSQLLocalDB;
              Database=DraftMottoDB;
              Integrated Security=True;
              TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
