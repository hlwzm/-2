#nullable disable
using System.Data;
using Dapper;
using MySqlConnector;
using Jx3.Common.Config;

namespace Jx3.Common.Database;

public class DbHelper : IDisposable
{
    private readonly string _connStr;

    public DbHelper(string? connStr = null)
    {
        _connStr = connStr ?? GameConfig.MySQLConn;
        if (string.IsNullOrEmpty(_connStr))
            throw new InvalidOperationException("MySQL connection string not configured. Set GameConfig.MySQLConn or appsettings.json Database:MySQL.");
    }

    // Per-query connections: MySqlConnector handles pooling internally.
    // This avoids holding a single open connection and is the correct Dapper pattern.

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();
        return await conn.QueryAsync<T>(sql, param);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();
        return await conn.ExecuteAsync(sql, param);
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }

    // No-op: connections are per-query and auto-disposed by using statements.
    public void Dispose() { }
}