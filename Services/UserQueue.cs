using System.Threading.Channels;
using QueueDemo.Models;

namespace QueueDemo.Services;

public class UserQueue
{
    private readonly Channel<User> _queue = Channel.CreateUnbounded<User>();

    public async Task EnqueueAsync(User user)
    {
        await _queue.Writer.WriteAsync(user);
    }

    public async Task<User> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}