
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QueueDemo.Data;
using QueueDemo.Models;

namespace QueueDemo.Services;

public class UserQueueWorker : BackgroundService
{
    private readonly UserQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserQueueWorker> _logger;

    private readonly string _consumer =
        $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public UserQueueWorker(
        UserQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<UserQueueWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "UserQueueWorker started. Consumer: {Consumer}",
            _consumer);

        try
        {
            
            await _queue.EnsureConsumerGroupAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                  
                    var entries = await _queue.ReadBatchAsync(
                        _consumer,
                        stoppingToken);

                    if (entries.Length == 0)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(1),
                            stoppingToken);

                        continue;
                    }

                    foreach (var entry in entries)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        try
                        {
                            
                            string name = entry.Values
                                .First(x => x.Name == "Name")
                                .Value.ToString();

                            string email = entry.Values
                                .First(x => x.Name == "Email")
                                .Value.ToString();

                            using var scope =
                                _scopeFactory.CreateScope();

                            var db = scope.ServiceProvider
                                .GetRequiredService<AppDbContext>();

                           
                            db.Users.Add(new User
                            {
                                Name = name,
                                Email = email
                            });

                            await db.SaveChangesAsync(
                                stoppingToken);

                           
                            await _queue.AcknowledgeAsync(entry.Id);

                            _logger.LogInformation(
                                "Processed message {MessageId} " +
                                "for user {Name}",
                                entry.Id,
                                name);
                        }
                        catch (OperationCanceledException)
                            when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                           
                            _logger.LogError(
                                ex,
                                "Failed to process message {MessageId}. " +
                                "The message remains pending.",
                                entry.Id);
                        }
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred while reading the queue.");

                    await Task.Delay(
                        TimeSpan.FromSeconds(2),
                        stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
           
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "The queue worker stopped because initialization failed.");
        }
        finally
        {
            _logger.LogInformation(
                "UserQueueWorker stopped.");
        }
    }
}
