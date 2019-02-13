using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class DbConnectionFactory
{
    readonly string constrKey;
    readonly Func<SqlConnection> factory;
    public DbConnectionFactory(string constrKey, IConfiguration config) =>
        factory = () =>
            new SqlConnection(config.GetConnectionString(constrKey));
    public SqlConnection Create() => factory();
}



