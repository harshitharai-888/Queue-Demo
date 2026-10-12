
using StackExchange.Redis;
using QueueDemo.Models;

namespace QueueDemo.Services;

public class UserQueue
{
    private readonly IDatabase _db;

    private const string StreamKey = "QueueDemo:users";
    private const string GroupName = "user-workers";

    public UserQueue(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task EnsureConsumerGroupAsync()
    {
        try
        {
            await _db.StreamCreateConsumerGroupAsync(
                StreamKey,
                GroupName,
                "0-0",
                createStream: true);
        }
        catch (RedisServerException ex)
            when (ex.Message.Contains(
                "BUSYGROUP",
                StringComparison.OrdinalIgnoreCase))
        {
            
        }
    }

    public async Task EnqueueAsync(User user)
    {
        await _db.StreamAddAsync(
            StreamKey,
            new[]
            {
                new NameValueEntry("Name", user.Name),
                new NameValueEntry("Email", user.Email)
            });
    }

   
    public async Task<StreamEntry[]> ReadBatchAsync(
        string consumer,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();


        var pending = await _db.StreamAutoClaimAsync(
        StreamKey,
        GroupName,
        consumer,
        minIdleTimeInMs: 60000,
        startAtId: "0-0",
        count: 10);

        token.ThrowIfCancellationRequested();

        if (pending.ClaimedEntries.Length > 0)
        {
            return pending.ClaimedEntries;
        }

        return await _db.StreamReadGroupAsync(
            StreamKey,
            GroupName,
            consumer,
            ">",
            count: 10);
    }

    
    public Task<long> AcknowledgeAsync(RedisValue messageId)
    {
        return _db.StreamAcknowledgeAsync(
            StreamKey,
            GroupName,
            messageId);
    }
}
