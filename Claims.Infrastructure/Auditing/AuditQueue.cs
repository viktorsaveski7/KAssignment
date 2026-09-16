using System.Threading.Channels;

namespace Claims.Infrastructure.Auditing;

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
