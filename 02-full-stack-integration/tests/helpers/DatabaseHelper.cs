using Npgsql;
using System.Data;

namespace EcommerceTests.Helpers
{
    public class DatabaseHelper
    {
        private NpgsqlConnection _connection;

        public DatabaseHelper(string host, int port, string database, string username, string password)
        {
            var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Timeout=10;CommandTimeout=30";
            _connection = new NpgsqlConnection(connectionString);
        }

        public void Connect()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        public void Disconnect()
        {
            if (_connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        public Order GetOrderById(string orderId)
        {
            const string sql = @"
                SELECT order_id, customer_email,
                       original_amount, discount_amount, final_amount,
                       promotion_code, status, created_at
                FROM   orders
                WHERE  order_id = @orderId
                LIMIT  1";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("orderId", orderId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new KeyNotFoundException($"Order '{orderId}' not found in database.");

            return new Order
            {
                OrderId        = reader.GetString(0),
                CustomerEmail  = reader.GetString(1),
                OriginalAmount = reader.GetDecimal(2),
                DiscountAmount = reader.GetDecimal(3),
                FinalAmount    = reader.GetDecimal(4),
                PromotionCode  = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Status         = reader.GetString(6),
                CreatedAt      = reader.GetDateTime(7),
            };
        }

        public AuditLog GetAuditLogByOrderId(string orderId)
        {
            const string sql = @"
                SELECT audit_id, promotion_id, order_id, discount_applied, used_at
                FROM   promotion_audit_log
                WHERE  order_id = @orderId
                ORDER  BY used_at DESC
                LIMIT  1";

            using var cmd = new NpgsqlCommand(sql, _connection);
            cmd.Parameters.AddWithValue("orderId", orderId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new KeyNotFoundException($"Audit log for order '{orderId}' not found.");

            return new AuditLog
            {
                AuditId         = reader.GetInt32(0),
                PromotionId     = reader.GetString(1),
                OrderId         = reader.GetString(2),
                DiscountApplied = reader.GetDecimal(3),
                UsedAt          = reader.GetDateTime(4),
            };
        }

        public void DeleteOrder(string orderId)
        {
            using var cmd1 = new NpgsqlCommand(
                "DELETE FROM promotion_audit_log WHERE order_id = @id", _connection);
            cmd1.Parameters.AddWithValue("id", orderId);
            cmd1.ExecuteNonQuery();

            using var cmd2 = new NpgsqlCommand(
                "DELETE FROM orders WHERE order_id = @id", _connection);
            cmd2.Parameters.AddWithValue("id", orderId);
            cmd2.ExecuteNonQuery();
        }

        public bool VerifyOrderTotals(string orderId, decimal expectedOriginal,
            decimal expectedDiscount, decimal expectedFinal)
        {
            var order = GetOrderById(orderId);
            return Math.Abs(order.OriginalAmount - expectedOriginal) <= 0.01m
                && Math.Abs(order.DiscountAmount - expectedDiscount) <= 0.01m
                && Math.Abs(order.FinalAmount    - expectedFinal)    <= 0.01m;
        }
    }

    public class Order
    {
        public string OrderId { get; set; }
        public string CustomerEmail { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PromotionCode { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLog
    {
        public int AuditId { get; set; }
        public string PromotionId { get; set; }
        public string OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime UsedAt { get; set; }
    }
}