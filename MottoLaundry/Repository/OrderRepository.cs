
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
        // =====================================================
        // SECTION 1: LOOKUP OPERATIONS
        // =====================================================

        public async Task<bool> TrackingNumberExistsAsync(
            string trackingNumber)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Orders
                WHERE TrackingNumber = @TrackingNumber;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@TrackingNumber", SqlDbType.VarChar, 50
            ).Value = trackingNumber;

            await connection.OpenAsync();

            return Convert.ToInt32(
                await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> ServiceCodeExistsAsync(
            string serviceCode)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Service
                WHERE ServiceCode = @ServiceCode;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@ServiceCode", SqlDbType.VarChar, 10
            ).Value = serviceCode;

            await connection.OpenAsync();

            return Convert.ToInt32(
                await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<List<string>> GetServiceCodesAsync()
        {
            var codes = new List<string>();

            const string sql = @"
                SELECT ServiceCode FROM Service;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                codes.Add(reader.GetString(0).Trim());
            }

            return codes;
        }

        // =====================================================
        // SECTION 2: EMPLOYEE NAME LOOKUP
        // =====================================================

        public async Task<int?> GetActiveEmployeeIdByNameAsync(
            string employeeName)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
                return null;

            const string sql = @"
                SELECT EmployeeID
                FROM Employee
                WHERE IsActive = 1
                  AND LOWER(LTRIM(RTRIM(EmployeeName))) =
                      LOWER(@EmployeeName);";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeName", SqlDbType.VarChar, 100
            ).Value = employeeName.Trim();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            int? employeeId = null;

            while (await reader.ReadAsync())
            {
                if (employeeId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Multiple active employees are named " +
                        $"'{employeeName}'. Use a unique employee name.");
                }

                employeeId = reader.GetInt32(0);
            }

            return employeeId;
        }

        public async Task<string> GetEmployeeNameByIdAsync(
            int employeeId)
        {
            const string sql = @"
                SELECT EmployeeName
                FROM Employee
                WHERE EmployeeID = @EmployeeID;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeID", SqlDbType.Int
            ).Value = employeeId;

            await connection.OpenAsync();

            object result = await command.ExecuteScalarAsync();

            return result == null || result == DBNull.Value
                ? ""
                : Convert.ToString(result) ?? "";
        }

        public async Task<bool> ActiveEmployeeExistsAsync(
            int employeeId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Employee
                WHERE EmployeeID = @EmployeeID
                  AND IsActive = 1;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeID", SqlDbType.Int
            ).Value = employeeId;

            await connection.OpenAsync();

            return Convert.ToInt32(
                await command.ExecuteScalarAsync()) > 0;
        }

        // =====================================================
        // SECTION 3: RETRIEVE ORDERS
        // =====================================================

        public async Task<List<OrderModel>> GetOrdersAsync()
        {
            var orders = new List<OrderModel>();

            const string sql = @"
                SELECT
                    o.OrderID,
                    o.TrackingNumber,
                    c.CustomerName,
                    s.ServiceCode,
                    o.LoadQuantity,
                    o.IntakeDate,
                    o.ServiceStatus,
                    o.Remarks,
                    s.TurnaroundTime,
                    o.DispatchedDate
                FROM Orders o
                INNER JOIN Customer c
                    ON o.CustomerID = c.CustomerID
                INNER JOIN Service s
                    ON o.ServiceID = s.ServiceID
                ORDER BY o.IntakeDate DESC;";

            using var connection = DatabaseConnection.GetConnection();

            await connection.OpenAsync();

            using (var command = new SqlCommand(sql, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    orders.Add(new OrderModel
                    {
                        TrackingNumber = reader.GetString(1),
                        CustomerName = reader.GetString(2),
                        ServiceCode = reader.GetString(3).Trim(),
                        Load = Convert.ToDouble(reader.GetValue(4)),
                        CreatedAt = reader.GetDateTime(5),
                        ServiceStatus = reader.GetString(6),
                        Notes = reader.IsDBNull(7)
                            ? "" : reader.GetString(7),
                        TurnaroundTime =
                            Convert.ToInt32(reader.GetValue(8)),
                        IsDispatched = !reader.IsDBNull(9)
                    });
                }
            }

            // Include EmployeeName in the progress query.
            const string progressSql = @"
                SELECT
                    o.TrackingNumber,
                    ps.StageName,
                    op.StageStatus,
                    e.EmployeeName
                FROM OrderProgress op
                INNER JOIN Orders o
                    ON op.OrderID = o.OrderID
                INNER JOIN ProgressStage ps
                    ON op.StageID = ps.StageID
                LEFT JOIN Employee e
                    ON op.PerformedByEmployeeID = e.EmployeeID;";

            var lookup = new Dictionary<string, OrderModel>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var order in orders)
            {
                lookup[order.TrackingNumber] = order;
            }

            using (var command =
                new SqlCommand(progressSql, connection))
            using (var reader =
                await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    string tracking = reader.GetString(0);

                    if (!lookup.TryGetValue(tracking, out var order))
                        continue;

                    string stageName = reader.GetString(1);
                    string status = reader.GetString(2);

                    StageProgress stage = stageName switch
                    {
                        "Washing" => order.Washing,
                        "Drying" => order.Drying,
                        "Folding" => order.Folding,
                        "Packing" => order.Packing,
                        _ => null
                    };

                    if (stage == null)
                        continue;

                    stage.Status = status == "Complete"
                        ? "Completed" : status;

                    stage.EmployeeNameOrId = reader.IsDBNull(3)
                        ? ""
                        : reader.GetString(3);
                }
            }

            return orders;
        }

        // =====================================================
        // SECTION 4: CREATE ORDER AND INITIALIZE PROGRESS
        // =====================================================

        public async Task<int> AddOrderAsync(OrderModel order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            using var connection = DatabaseConnection.GetConnection();

            await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // STEP 1: Find the service.

                const string serviceSql = @"
                    SELECT ServiceID
                    FROM Service
                    WHERE ServiceCode = @ServiceCode;";

                int serviceId;

                using (var command = new SqlCommand(
                    serviceSql, connection, transaction))
                {
                    command.Parameters.Add(
                        "@ServiceCode", SqlDbType.VarChar, 10
                    ).Value = order.ServiceCode;

                    object result =
                        await command.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                    {
                        throw new InvalidOperationException(
                            "Service code does not exist.");
                    }

                    serviceId = Convert.ToInt32(result);
                }

                // STEP 2: Insert customer.

                const string customerSql = @"
                    INSERT INTO Customer (CustomerName)
                    OUTPUT INSERTED.CustomerID
                    VALUES (@CustomerName);";

                int customerId;

                using (var command = new SqlCommand(
                    customerSql, connection, transaction))
                {
                    command.Parameters.Add(
                        "@CustomerName", SqlDbType.VarChar, 100
                    ).Value = order.CustomerName;

                    customerId = Convert.ToInt32(
                        await command.ExecuteScalarAsync());
                }

                // STEP 3: Insert order.

                const string orderSql = @"
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
                        'Not Started',
                        @Remarks
                    );";

                int orderId;

                using (var command = new SqlCommand(
                    orderSql, connection, transaction))
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

                    var load = command.Parameters.Add(
                        "@LoadQuantity", SqlDbType.Decimal);

                    load.Precision = 10;
                    load.Scale = 2;
                    load.Value = Convert.ToDecimal(order.Load);

                    command.Parameters.Add(
                        "@IntakeDate", SqlDbType.DateTime
                    ).Value = order.CreatedAt;

                    command.Parameters.Add(
                        "@Remarks", SqlDbType.VarChar, 500
                    ).Value = string.IsNullOrWhiteSpace(order.Notes)
                        ? DBNull.Value : order.Notes;

                    orderId = Convert.ToInt32(
                        await command.ExecuteScalarAsync());
                }

                // STEP 4: Initialize progress.

                const string progressSql = @"
                    INSERT INTO OrderProgress
                    (
                        OrderID,
                        StageID,
                        StageStatus,
                        PerformedByEmployeeID,
                        StartedAt,
                        CompletedAt
                    )
                    SELECT
                        @OrderID,
                        StageID,
                        'Not Started',
                        NULL,
                        NULL,
                        NULL
                    FROM ProgressStage
                    WHERE StageName IN
                    (
                        'Washing',
                        'Drying',
                        'Folding',
                        'Packing'
                    );";

                using (var command = new SqlCommand(
                    progressSql, connection, transaction))
                {
                    command.Parameters.Add(
                        "@OrderID", SqlDbType.Int
                    ).Value = orderId;

                    int inserted =
                        await command.ExecuteNonQueryAsync();

                    if (inserted != 4)
                    {
                        throw new InvalidOperationException(
                            "Exactly four progress stages must exist.");
                    }
                }

                await transaction.CommitAsync();

                return orderId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // SECTION 5: UPDATE STATUS AND PROGRESS
        // =====================================================

        public async Task UpdateOrderStatusAsync(
            string trackingNumber,
            string[] statuses,
            int?[] employeeIds,
            string serviceStatus,
            string notes)
        {
            if (statuses == null || statuses.Length != 4 ||
                employeeIds == null || employeeIds.Length != 4)
            {
                throw new ArgumentException(
                    "Exactly four progress stages are required.");
            }

            string[] stageNames =
            {
                "Washing", "Drying", "Folding", "Packing"
            };

            using var connection = DatabaseConnection.GetConnection();

            await connection.OpenAsync();

            using var transaction =
                (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                // STEP 1: Find and lock the order.

                const string findSql = @"
                    SELECT OrderID
                    FROM Orders WITH (UPDLOCK, HOLDLOCK)
                    WHERE TrackingNumber = @TrackingNumber
                      AND DispatchedDate IS NULL;";

                int orderId;

                using (var command = new SqlCommand(
                    findSql, connection, transaction))
                {
                    command.Parameters.Add(
                        "@TrackingNumber", SqlDbType.VarChar, 50
                    ).Value = trackingNumber;

                    object result =
                        await command.ExecuteScalarAsync();

                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            "Order not found or already dispatched.");
                    }

                    orderId = Convert.ToInt32(result);
                }

                // STEP 2: Validate employee IDs.

                const string employeeSql = @"
                    SELECT COUNT(1)
                    FROM Employee
                    WHERE EmployeeID = @EmployeeID
                      AND IsActive = 1;";

                for (int i = 0; i < 4; i++)
                {
                    if (!employeeIds[i].HasValue)
                        continue;

                    using var command = new SqlCommand(
                        employeeSql, connection, transaction);

                    command.Parameters.Add(
                        "@EmployeeID", SqlDbType.Int
                    ).Value = employeeIds[i].Value;

                    int count = Convert.ToInt32(
                        await command.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Employee ID {employeeIds[i]} " +
                            "does not exist or is inactive.");
                    }
                }

                // STEP 3: Update the order.

                const string updateOrderSql = @"
                    UPDATE Orders
                    SET ServiceStatus = @ServiceStatus,
                        Remarks = @Remarks
                    WHERE OrderID = @OrderID
                      AND DispatchedDate IS NULL;";

                using (var command = new SqlCommand(
                    updateOrderSql, connection, transaction))
                {
                    command.Parameters.Add(
                        "@OrderID", SqlDbType.Int
                    ).Value = orderId;

                    command.Parameters.Add(
                        "@ServiceStatus", SqlDbType.VarChar, 20
                    ).Value = serviceStatus;

                    command.Parameters.Add(
                        "@Remarks", SqlDbType.VarChar, 500
                    ).Value = string.IsNullOrWhiteSpace(notes)
                        ? DBNull.Value : notes;

                    int affected =
                        await command.ExecuteNonQueryAsync();

                    if (affected != 1)
                    {
                        throw new InvalidOperationException(
                            "Order could not be updated.");
                    }
                }

                // STEP 4: Update or insert stage records.

                // Every order is initialized with all four progress rows.
                // Update only: never insert as a fallback after a failed UPDATE.
                const string updateProgressSql = @"
                    UPDATE dbo.OrderProgress
                    SET
                        StageStatus = @StageStatus,
                        PerformedByEmployeeID = @EmployeeID,
                        StartedAt = CASE
                            WHEN @StageStatus = 'Not Started' THEN NULL
                            ELSE COALESCE(StartedAt, GETDATE())
                        END,
                        CompletedAt = CASE
                            WHEN @StageStatus = 'Completed'
                                THEN COALESCE(CompletedAt, GETDATE())
                            ELSE NULL
                        END
                    WHERE OrderID = @OrderID
                      AND StageID = @StageID;";

                for (int i = 0; i < 4; i++)
                {
                    const string stageSql = @"
                        SELECT StageID
                        FROM ProgressStage
                        WHERE StageName = @StageName;";

                    int stageId;

                    using (var command = new SqlCommand(
                        stageSql, connection, transaction))
                    {
                        command.Parameters.Add(
                            "@StageName", SqlDbType.VarChar, 50
                        ).Value = stageNames[i];

                        object result =
                            await command.ExecuteScalarAsync();

                        if (result == null)
                        {
                            throw new InvalidOperationException(
                                $"Stage {stageNames[i]} is missing.");
                        }

                        stageId = Convert.ToInt32(result);
                    }

                    using var progressCommand = new SqlCommand(
                        updateProgressSql, connection, transaction);

                    progressCommand.Parameters.Add(
                        "@OrderID", SqlDbType.Int
                    ).Value = orderId;

                    progressCommand.Parameters.Add(
                        "@StageID", SqlDbType.Int
                    ).Value = stageId;

                    progressCommand.Parameters.Add(
                        "@StageStatus", SqlDbType.VarChar, 20
                    ).Value = statuses[i];

                    progressCommand.Parameters.Add(
                        "@EmployeeID", SqlDbType.Int
                    ).Value = employeeIds[i].HasValue
                        ? employeeIds[i].Value
                        : DBNull.Value;

                    int updatedRows = await progressCommand.ExecuteNonQueryAsync();
                    if (updatedRows != 1)
                    {
                        throw new InvalidOperationException(
                            $"Missing or duplicate progress record for {stageNames[i]}. " +
                            "Verify the four progress rows were initialized.");
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =====================================================
        // SECTION 6: DISPATCH
        // =====================================================

        public async Task DispatchOrderAsync(
            string trackingNumber)
        {
            const string sql = @"
                UPDATE Orders
                SET ServiceStatus = 'Dispatched',
                    DispatchedDate = GETDATE()
                WHERE TrackingNumber = @TrackingNumber
                  AND DispatchedDate IS NULL
                  AND ServiceStatus IN ('Completed', 'Unclaimed');";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@TrackingNumber", SqlDbType.VarChar, 50
            ).Value = trackingNumber;

            await connection.OpenAsync();

            int affected = await command.ExecuteNonQueryAsync();

            if (affected != 1)
            {
                throw new InvalidOperationException(
                    "Order must be completed and not already dispatched.");
            }
        }

        // =====================================================
        // SECTION 7: PHOTO UPLOAD
        // =====================================================

        public async Task AddOrderPhotoAsync(
            string trackingNumber,
            string photoPath)
        {
            const string sql = @"
                INSERT INTO OrderPhoto
                (
                    OrderID,
                    PhotoPath,
                    UploadedAt
                )
                SELECT
                    OrderID,
                    @PhotoPath,
                    GETDATE()
                FROM Orders
                WHERE TrackingNumber = @TrackingNumber
                  AND DispatchedDate IS NULL;";

            using var connection = DatabaseConnection.GetConnection();
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@TrackingNumber", SqlDbType.VarChar, 50
            ).Value = trackingNumber;

            command.Parameters.Add(
                "@PhotoPath", SqlDbType.VarChar, 255
            ).Value = photoPath;

            await connection.OpenAsync();

            int affected = await command.ExecuteNonQueryAsync();

            if (affected != 1)
            {
                throw new InvalidOperationException(
                    "Photo could not be saved for this order.");
            }
        }
    }
}
