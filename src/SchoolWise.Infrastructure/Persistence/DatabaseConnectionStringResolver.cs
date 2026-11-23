using Npgsql;

namespace SchoolWise.Infrastructure.Persistence;

public static class DatabaseConnectionStringResolver
{
    public static string Normalize(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return value;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')))
        {
            throw new InvalidOperationException("DATABASE_URL must include a database name and username/password.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]).ToLowerInvariant();
            var parameterValue = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;

            switch (key)
            {
                case "sslmode":
                    builder["SSL Mode"] = parameterValue;
                    break;
                case "channel_binding":
                    builder["Channel Binding"] = parameterValue;
                    break;
                case "application_name":
                    builder.ApplicationName = parameterValue;
                    break;
                case "options":
                    builder["Options"] = parameterValue;
                    break;
                case "connect_timeout":
                    builder.Timeout = int.Parse(parameterValue, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported DATABASE_URL parameter '{key}'.");
            }
        }

        return builder.ConnectionString;
    }
}
