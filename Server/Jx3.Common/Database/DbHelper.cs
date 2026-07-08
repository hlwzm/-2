#nullable disable
using System.Data;
using Dapper;
using MySqlConnector;

namespace Jx3.Common.Database;

public class DbHelper : IDisposable
{
    private readonly MySqlConnection _conn;

    public DbHelper(string? connStr = null)
    {
        connStr ??= "server=127.0.0.1;port=3306;database=jx3;user=root;password=123456;AllowUserVariables=True;";
        _conn = new MySqlConnection(connStr);
        _conn.Open();
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
        => await _conn.QueryAsync<T>(sql, param);

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
        => await _conn.QueryFirstOrDefaultAsync<T>(sql, param);

    public async Task<int> ExecuteAsync(string sql, object? param = null)
        => await _conn.ExecuteAsync(sql, param);

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null)
        => await _conn.ExecuteScalarAsync<T>(sql, param);

    public void Dispose() => _conn?.Dispose();
}