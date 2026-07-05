namespace RentalAdvisor.Backend.Services;

public class ClauseExtractionWorker : BackgroundService
{
    private readonly ClauseExtractionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClauseExtractionWorker> _logger;

    public ClauseExtractionWorker(ClauseExtractionQueue queue, IServiceScopeFactory scopeFactory, ILogger<ClauseExtractionWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            _queue.Statuses[job.LeaseDocumentId] = new JobStatusInfo(JobStatus.Processing, null, null);
            _logger.LogInformation("Clause extraction job started for lease {LeaseId}", job.LeaseDocumentId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ClauseExtractionService>();
                var created = await service.ExtractAndPersistAsync(job.LeaseDocumentId, job.UserId, stoppingToken);

                _queue.Statuses[job.LeaseDocumentId] = new JobStatusInfo(JobStatus.Done, null, created);
                _logger.LogInformation("Clause extraction job finished for lease {LeaseId}: {Count} clauses", job.LeaseDocumentId, created);
            }
            catch (Exception ex)
            {
                _queue.Statuses[job.LeaseDocumentId] = new JobStatusInfo(JobStatus.Failed, ex.Message, null);
                _logger.LogWarning(ex, "Clause extraction job failed for lease {LeaseId}", job.LeaseDocumentId);
            }
        }
    }
}
