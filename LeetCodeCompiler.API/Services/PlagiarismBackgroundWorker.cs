namespace LeetCodeCompiler.API.Services
{
    public class PlagiarismBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PlagiarismBackgroundWorker> _logger;

        public PlagiarismBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<PlagiarismBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (PlagiarismService.TryDequeue(out var job))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var plagiarismService = scope.ServiceProvider.GetRequiredService<IPlagiarismService>();
                        await plagiarismService.ProcessCheckAsync(job);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Plagiarism check failed for submission {SubmissionId}", job.SubmissionId);
                    }
                }
                else
                {
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }
    }
}
