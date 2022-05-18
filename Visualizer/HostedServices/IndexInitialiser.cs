using Redis.OM;
using Visualizer.Model;

namespace Visualizer.HostedServices;

public class IndexInitializer : IHostedService
{
    private readonly RedisConnectionProvider _redisConnectionProvider;

    public IndexInitializer(RedisConnectionProvider redisConnectionProvider)
    {
        _redisConnectionProvider = redisConnectionProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _redisConnectionProvider.Connection.CreateIndexAsync(typeof(TweetModel));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}