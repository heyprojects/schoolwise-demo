using Npgsql;
using SchoolWise.Infrastructure.Persistence;

namespace SchoolWise.Integration.Tests;

public sealed class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void NeonUriIsConvertedToNpgsqlConnectionString()
    {
        var connectionString = DatabaseConnectionStringResolver.Normalize(
            "postgresql://parent%40schoolwise.gr:p%40ss%3Aword@ep-example-pooler.eu-central-1.aws.neon.tech/schoolwise?sslmode=require&channel_binding=require");
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("ep-example-pooler.eu-central-1.aws.neon.tech", parsed.Host);
        Assert.Equal("schoolwise", parsed.Database);
        Assert.Equal("parent@schoolwise.gr", parsed.Username);
        Assert.Equal("p@ss:word", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
        Assert.Equal(ChannelBinding.Require, parsed.ChannelBinding);
    }

    [Fact]
    public void NativeNpgsqlConnectionStringIsPreserved()
    {
        const string connectionString = "Host=localhost;Database=schoolwise;Username=schoolwise";

        Assert.Equal(connectionString, DatabaseConnectionStringResolver.Normalize(connectionString));
    }
}
