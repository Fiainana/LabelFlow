using LabelFlow.Models;
using Microsoft.Data.SqlClient;

namespace LabelFlow.Services;

public static class SqlConnectionFactory
{
    public static string CreateConnectionString(ConnectionConfig config, string? plainPassword = null)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = config.Server,
            InitialCatalog = config.Database,
            TrustServerCertificate = true,
            ConnectTimeout = 15
        };

        if (config.UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = config.User ?? string.Empty;
            builder.Password = plainPassword ?? ConfigService.DecryptPassword(config) ?? string.Empty;
        }

        return builder.ConnectionString;
    }

    public static SqlConnection Create(ConnectionConfig config, string? plainPassword = null)
        => new(CreateConnectionString(config, plainPassword));
}
