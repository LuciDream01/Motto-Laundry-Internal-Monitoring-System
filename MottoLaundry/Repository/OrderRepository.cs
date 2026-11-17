
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MottoLaundry.Data;
using MottoLaundry.Models;

namespace MottoLaundry.Repository
{
    public class OrderRepository : IOrderRepository
    {
        public async Task<bool> TrackingNumberExistsAsync(
            string trackingNumber)
        {
            const string query = @"
                SELECT COUNT(1)
                FROM Orders
                WHERE TrackingNumber = @TrackingNumber;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(query, connection);

            command.Parameters.Add(
                "@TrackingNumber", SqlDbType.VarChar, 50
            ).Value = trackingNumber;

            await connection.OpenAsync();

            int count = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            return count > 0;
        }

        public async Task<bool> ServiceCodeExistsAsync(
            string serviceCode)
        {
            const string query = @"
                SELECT COUNT(1)
                FROM Service
                WHERE ServiceCode = @ServiceCode;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(query, connection);

            command.Parameters.Add(
                "@ServiceCode", SqlDbType.VarChar, 50
            ).Value = serviceCode;

            await connection.OpenAsync();

            int count = Convert.ToInt32(
                await command.ExecuteScalarAsync());

            return count > 0;
        }

        public async Task<List<string>> GetServiceCodesAsync()
        {
            var codes = new List<string>();

            const string query = @"
                SELECT ServiceCode
                FROM Service;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                codes.Add(reader.GetString(0).Trim());
            }

            return codes;
        }

        public async Task<List<OrderModel>> GetOrdersAsync()
        {
            var orders = new List<OrderModel>();

            const string query = @"
                SELECT
                    o.TrackingNumber,
                    c.CustomerName,
                    s.ServiceCode,
                    o.LoadQuantity,
                    o.IntakeDate,
                    o.ServiceStatus,
                    o.Remarks,
                    s.TurnaroundTime
                FROM Orders o
                INNER JOIN Customer c
                    ON o.CustomerID = c.CustomerID
                INNER JOIN Service s
                    ON o.ServiceID = s.ServiceID
                ORDER BY o.IntakeDate DESC;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var order = new OrderModel
                {
                    TrackingNumber = reader.GetString(0),
                    CustomerName = reader.GetString(1),
                    ServiceCode = reader.GetString(2).Trim(),
                    Load = Convert.ToDouble(reader.GetValue(3)),
                    CreatedAt = reader.GetDateTime(4),
                    ServiceStatus = reader.GetString(5),
                    Notes = reader.IsDBNull(6)
                        ? ""
                        : reader.GetString(6),
                    TurnaroundTime = Convert.ToInt32(
                        reader.GetValue(7))
                };

                orders.Add(order);
            }

            return orders;
        }

        public async Task<int> AddOrderAsync(OrderModel order)
        {
            using var connection = DatabaseConnection.GetConnection();

            await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // STEP 1: Retrieve the ServiceID first
                const string serviceQuery = @"
                    SELECT ServiceID
                    FROM Service
                    WHERE ServiceCode = @ServiceCode;";

                int serviceId;

                using (var command = new SqlCommand(
                    serviceQuery, connection, transaction))
                {
                    command.Parameters.Add(
                        "@ServiceCode", SqlDbType.VarChar, 50
                    ).Value = order.ServiceCode;

                    object? result =
                        await command.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                    {
                        throw new InvalidOperationException(
                            "Service code does not exist.");
                    }

                    serviceId = Convert.ToInt32(result);
                }

                // STEP 2: Insert Customer and retrieve CustomerID
                const string customerQuery = @"
                    INSERT INTO Customer (CustomerName)
                    OUTPUT INSERTED.CustomerID
                    VALUES (@CustomerName);";

                int customerId;

                using (var command = new SqlCommand(
                    customerQuery, connection, transaction))
                {
                    command.Parameters.Add(
                        "@CustomerName", SqlDbType.VarChar, 255
                    ).Value = order.CustomerName;

                    customerId = Convert.ToInt32(
                        await command.ExecuteScalarAsync());
                }

                // STEP 3: Insert Order
                const string orderQuery = @"
                    INSERT INTO Orders
                    (
                        TrackingNumber,
                        CustomerID,
                        ServiceID,
                        LoadQuantity,
                        IntakeDate,
                        DispatchedDate,
                        ServiceStatus,
                        Remarks
                    )
                    OUTPUT INSERTED.OrderID
                    VALUES
                    (
                        @TrackingNumber,
                        @CustomerID,
                        @ServiceID,
                        @LoadQuantity,
                        @IntakeDate,
                        NULL,
                        @ServiceStatus,
                        @Remarks
                    );";

                int orderId;

                using (var command = new SqlCommand(
                    orderQuery, connection, transaction))
                {
                    command.Parameters.Add(
                        "@TrackingNumber", SqlDbType.VarChar, 50
                    ).Value = order.TrackingNumber;

                    command.Parameters.Add(
                        "@CustomerID", SqlDbType.Int
                    ).Value = customerId;

                    command.Parameters.Add(
                        "@ServiceID", SqlDbType.Int
                    ).Value = serviceId;

                    var loadParameter = command.Parameters.Add(
                        "@LoadQuantity", SqlDbType.Decimal);

                    loadParameter.Precision = 10;
                    loadParameter.Scale = 2;
                    loadParameter.Value = Convert.ToDecimal(order.Load);

                    command.Parameters.Add(
                        "@IntakeDate", SqlDbType.DateTime2
                    ).Value = order.CreatedAt;

                    command.Parameters.Add(
                        "@ServiceStatus", SqlDbType.VarChar, 50
                    ).Value = order.ServiceStatus;

                    command.Parameters.Add(
                        "@Remarks", SqlDbType.VarChar, -1
                    ).Value = string.IsNullOrWhiteSpace(order.Notes)
                        ? DBNull.Value
                        : order.Notes;

                    orderId = Convert.ToInt32(
                        await command.ExecuteScalarAsync());
                }

                // STEP 4: Commit transaction
                await transaction.CommitAsync();

                return orderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
