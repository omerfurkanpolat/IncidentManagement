using IncidentManagement.Orchestrator.Services;
using IncidentManagement.Shared.Models;
using IncidentManagement.Shared.Services;

namespace IncidentManagement.Orchestrator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly JobResultRepository _jobRepo;
    private readonly IncidentReportRepository _reportRepo;
    private readonly LlmService _llmService;

    public Worker(ILogger<Worker> logger, JobResultRepository jobRepo,
        IncidentReportRepository reportRepo, LlmService llmService)
    {
        _logger = logger;
        _jobRepo = jobRepo;
        _reportRepo = reportRepo;
        _llmService = llmService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessFailedJobsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessFailedJobsAsync()
    {
        var failedJobs = await _jobRepo.GetFailedUnprocessedAsync();
        if (!failedJobs.Any()) return;

        _logger.LogInformation("[Orchestrator] {Count} hatalı job işlenecek", failedJobs.Count);

        foreach (var job in failedJobs)
        {
            try
            {
                _logger.LogInformation("[Orchestrator] Analiz ediliyor: {JobName}", job.JobName);
                var analysis = await _llmService.AnalyzeIncidentAsync(job);

                var report = new IncidentReport
                {
                    JobResultId = job.Id!,
                    JobName = job.JobName,
                    ErrorCode = job.ErrorCode ?? "UNKNOWN",
                    ErrorMessage = job.ErrorMessage ?? string.Empty,
                    LlmAnalysis = analysis.Analysis,
                    SourcesUsed = analysis.SourcesUsed.Distinct().ToList()
                };

                await _reportRepo.InsertAsync(report);
                await _jobRepo.MarkAsProcessedAsync(job.Id!);

                _logger.LogInformation("[Orchestrator] {JobName} analiz tamamlandı. Kaynaklar: {Sources}",
                    job.JobName, string.Join(", ", report.SourcesUsed));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Orchestrator] {JobName} analiz edilemedi", job.JobName);
            }
        }
    }
}
