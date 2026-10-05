using Microsoft.Extensions.DependencyInjection;
using QueueDemo.Data;

namespace QueueDemo.Services;

public class UserQueueWorker : BackgroundService
{
    private readonly UserQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;

    public UserQueueWorker(
        UserQueue queue,
        IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var user = await _queue.DequeueAsync(stoppingToken);

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            db.Users.Add(user);

            await db.SaveChangesAsync();
        }
    }
}