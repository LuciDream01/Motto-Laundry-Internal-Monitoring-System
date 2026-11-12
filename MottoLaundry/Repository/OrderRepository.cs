using System;
using System.Collections.Generic;
using System.Text;

namespace MottoLaundry.Repository
{
    class OrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

            _connectionString = connectionString;
        }
    }   
}
