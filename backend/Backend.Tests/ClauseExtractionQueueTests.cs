using RentalAdvisor.Backend.Services;
using Xunit;

namespace Backend.Tests;

public class ClauseExtractionQueueTests
{
    [Fact]
    public void Enqueue_SetsStatusToQueued()
    {
        var queue = new ClauseExtractionQueue();

        queue.Enqueue(new ClauseExtractionJob(LeaseDocumentId: 1, UserId: 1));

        Assert.True(queue.Statuses.TryGetValue(1, out var status));
        Assert.Equal(JobStatus.Queued, status!.Status);
    }

    [Fact]
    public async Task Enqueue_WritesToChannel_ReaderReceivesJob()
    {
        var queue = new ClauseExtractionQueue();
        var job = new ClauseExtractionJob(LeaseDocumentId: 5, UserId: 2);

        queue.Enqueue(job);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = await queue.Reader.ReadAsync(cts.Token);

        Assert.Equal(job, received);
    }
}
