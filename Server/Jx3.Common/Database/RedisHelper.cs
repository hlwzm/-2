using StackExchange.Redis;

namespace Jx3.Common.Database;

public class RedisHelper : IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    public IDatabase Db { get; }

    public RedisHelper(string? connStr = null)
    {
        connStr ??= "127.0.0.1:6379";
        _redis = ConnectionMultiplexer.Connect(connStr);
        Db = _redis.GetDatabase();
    }

    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
        => await Db.StringSetAsync(key, value, expiry);

    public async Task<string?> GetAsync(string key)
        => await Db.StringGetAsync(key);

    public async Task<bool> DeleteAsync(string key)
        => await Db.KeyDeleteAsync(key);

    public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
        => await Db.KeyExpireAsync(key, expiry);

    public void Dispose() => _redis?.Dispose();
}