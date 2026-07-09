using StackExchange.Redis;
using Jx3.Common.Config;

namespace Jx3.Common.Database;

public class RedisHelper : IDisposable
{
    // ConnectionMultiplexer is designed to be a long-lived singleton.
    // Reusing one instance avoids expensive reconnect overhead.
    private static ConnectionMultiplexer? _instance;
    private static readonly object _lock = new();
    private static string _lastConnStr = "";

    private readonly IDatabase _db;

    public RedisHelper(string? connStr = null)
    {
        connStr ??= GameConfig.RedisConn;
        if (string.IsNullOrEmpty(connStr))
            throw new InvalidOperationException("Redis connection string not configured. Set GameConfig.RedisConn or appsettings.json Database:Redis.");

        var mux = GetOrCreateMultiplexer(connStr);
        _db = mux.GetDatabase();
    }

    private static ConnectionMultiplexer GetOrCreateMultiplexer(string connStr)
    {
        if (_instance != null && connStr == _lastConnStr)
            return _instance;

        lock (_lock)
        {
            if (_instance != null && connStr == _lastConnStr)
                return _instance;

            _instance?.Dispose();
            _instance = ConnectionMultiplexer.Connect(connStr);
            _lastConnStr = connStr;
            return _instance;
        }
    }

    public IDatabase Db => _db;

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
        => await _db.StringSetAsync(key, value, expiry);

    public async Task<string?> GetAsync(string key)
        => await _db.StringGetAsync(key);

    public async Task<bool> DeleteAsync(string key)
        => await _db.KeyDeleteAsync(key);

    public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
        => await _db.KeyExpireAsync(key, expiry);

    // No-op: ConnectionMultiplexer is shared (singleton). Do not dispose per call.
    public void Dispose() { }
}