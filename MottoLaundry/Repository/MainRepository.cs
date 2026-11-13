using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace MottoLaundry.Repository
{
    class MainRepository
    {
        private string dbConnection { get; set; } = "Server=(localdb)//MSSQLLocalDB; Trusted_Connection=True;TrustServerCertificate=True;";
        
    }
}
