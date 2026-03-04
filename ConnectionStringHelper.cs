using Npgsql;

namespace UserManagementApp
{
    public static class ConnectionStringHelper
    {
        public static string BuildPostgresConnectionString(string? rawConnectionString)
        {
            if (string.IsNullOrWhiteSpace(rawConnectionString))
            {
                throw new InvalidOperationException(
                    "Connection string is empty. Set DATABASE_URL or ConnectionStrings:DefaultConnection.");
            }

            var normalizedInput = rawConnectionString.Trim();

            if (!normalizedInput.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !normalizedInput.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedInput;
            }

            var databaseUri = new Uri(normalizedInput);
            var userInfo = databaseUri.UserInfo.Split(':', 2);

            if (userInfo.Length != 2)
                throw new InvalidOperationException("DATABASE_URL must include username and password in the URI user info.");

            var databaseName = databaseUri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("DATABASE_URL must include a database name in the path.");

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port > 0 ? databaseUri.Port : 5432,
                Database = databaseName,
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = Uri.UnescapeDataString(userInfo[1]),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }
    }
}
