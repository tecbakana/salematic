using Salematic.Domain.Interfaces;
using StackExchange.Redis;

namespace Salematic.Infrastructure.Locking;

public class RedisStockLockService : IStockLockService
{
    private static readonly string UnlockScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IDatabase _db;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 300;
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(30);

    public RedisStockLockService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<IAsyncDisposable?> AcquireAsync(int produtoId, CancellationToken ct = default)
    {
        var key = $"lock:estoque:{produtoId}";
        var token = Guid.NewGuid().ToString("N");

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            if (await _db.StringSetAsync(key, token, LockTtl, When.NotExists))
                return new RedisLockHandle(_db, key, token);

            await Task.Delay(RetryDelayMs, ct);
        }

        return null;
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public RedisLockHandle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            await _db.ScriptEvaluateAsync(UnlockScript, new RedisKey[] { _key }, new RedisValue[] { _token });
        }
    }
}