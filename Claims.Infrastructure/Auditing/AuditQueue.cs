using System.Threading.Channels;

namespace Claims.Infrastructure.Auditing;

/// <summary>
/// Hands audit entries from the request thread to the background writer. Bounded so a burst cannot
/// exhaust memory, and it waits rather than drops when full: losing an audit record is worse than
/// brief back-pressure.
/// </summary>
public sealed class AuditQueue
{
    public const int DefaultCapacity = 10_000;

    private readonly Channel<AuditEntry> _channel;

    public AuditQueue(int capacity = DefaultCapacity)
    {
        _channel = Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelReader<AuditEntry> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(AuditEntry entry, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(entry, cancellationToken);

    public void CompleteAdding() => _channel.Writer.TryComplete();
}
