using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace Infrastructure.Caching;

public class CacheService(IDistributedCache cacher) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var dataInCache = await cacher.GetStringAsync(key, cancellationToken);

        if (dataInCache is not null)
        {
            await Console.Out.WriteLineAsync(new string('-', 50));
            Log.Information("Redis: Data retrieved from cache key: {k}", key);
            await Console.Out.WriteLineAsync(new string('-', 50));
            
            return JsonSerializer.Deserialize<T>(dataInCache);
        }
        
        await Console.Out.WriteLineAsync(new string('-', 50));
        Log.Warning("Redis: There is no data in the cache for this key: {k}", key);
        await Console.Out.WriteLineAsync(new string('-', 50));
        return default;
    }

    public async Task AddAsync<T>(string key, T value, DateTimeOffset expirationTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
            var jsonObject = JsonSerializer.Serialize(value, jsonSerializerOptions);
            var cacheOptions = new DistributedCacheEntryOptions() { AbsoluteExpiration = expirationTime };
            
            await cacher.SetStringAsync(key, jsonObject, cacheOptions, cancellationToken);
            
            
            await Console.Out.WriteLineAsync(new string('-', 50));
            Log.Information("Redis : Added new data to cache by key: {key}, expiration time before : {expirationTime}", key, expirationTime);
            await Console.Out.WriteLineAsync(new string('-', 50));
        }
        catch (Exception e)
        {
            Log.Error("Error: {0}", e.Message);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cacher.RemoveAsync(key, cancellationToken);
            await Console.Out.WriteLineAsync(new string('-', 50));
            Log.Information("Redis : Deleted data from cache key: {key}", key);
        }
        catch (Exception e)
        {
            Log.Error("Error: {0}", e.Message);
        }
    }
}