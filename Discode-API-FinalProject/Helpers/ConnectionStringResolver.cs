using Microsoft.Data.SqlClient;

namespace Discode_API_FinalProject.Helpers
{
    public static class ConnectionStringResolver
    {
        private const string DefaultDatabase = "DiscodeDB";

        public static string Resolve(IConfiguration configuration, ILogger logger)
        {
            var configured = configuration.GetConnectionString("DefaultConnection");
            var baseBuilder = string.IsNullOrWhiteSpace(configured)
                ? new SqlConnectionStringBuilder
                {
                    InitialCatalog = DefaultDatabase,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true
                }
                : new SqlConnectionStringBuilder(configured);

            var servers = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseBuilder.DataSource))
            {
                servers.Add(baseBuilder.DataSource);
            }

            foreach (var candidate in Candidates())
            {
                if (!servers.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    servers.Add(candidate);
                }
            }

            foreach (var server in servers)
            {
                var builder = With(baseBuilder, server, baseBuilder.InitialCatalog);
                if (CanConnect(builder))
                {
                    logger.LogInformation("SQL Server found: {Server} (database: {Database})", server, baseBuilder.InitialCatalog);
                    return builder.ConnectionString;
                }
            }

            foreach (var server in servers)
            {
                if (CanConnect(With(baseBuilder, server, "master")))
                {
                    logger.LogWarning("SQL Server found: {Server}, but '{Database}' does not exist yet. Migration will create it.", server, baseBuilder.InitialCatalog);
                    return With(baseBuilder, server, baseBuilder.InitialCatalog).ConnectionString;
                }
            }

            logger.LogError(
                "SQL Server was not found. Checked: {Servers}. Update ConnectionStrings:DefaultConnection in appsettings.json.",
                string.Join(", ", servers));
            return baseBuilder.ConnectionString;
        }

        private static IEnumerable<string> Candidates()
        {
            yield return @".\SQLEXPRESS";
            yield return @"(localdb)\MSSQLLocalDB";
            yield return ".";
            yield return "localhost";
            yield return @"localhost\SQLEXPRESS";
            yield return $@"{Environment.MachineName}\SQLEXPRESS";
            yield return Environment.MachineName;
        }

        private static SqlConnectionStringBuilder With(SqlConnectionStringBuilder source, string server, string database)
        {
            var builder = new SqlConnectionStringBuilder(source.ConnectionString)
            {
                DataSource = server,
                InitialCatalog = database,
                ConnectTimeout = 5
            };
            builder.TrustServerCertificate = true;
            return builder;
        }

        private static bool CanConnect(SqlConnectionStringBuilder builder)
        {
            try
            {
                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
