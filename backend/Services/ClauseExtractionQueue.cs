using System.Collections.Concurrent;
using System.Threading.Channels;

namespace RentalAdvisor.Backend.Services;

public class ClauseExtractionQueue
{
    private readonly Channel<ClauseExtractionJob> _channel = Channel.CreateUnbounded<ClauseExtractionJob>();

    public ConcurrentDictionary<int, JobStatusInfo> Statuses { get; } = new();

    public ChannelReader<ClauseExtractionJob> Reader => _channel.Reader;

    public void Enqueue(ClauseExtractionJob job)
    {
        Statuses[job.LeaseDocumentId] = new JobStatusInfo(JobStatus.Queued, null, null);
        _channel.Writer.TryWrite(job);
    }
}
