using IncidentManagement.Shared.Models;
using IncidentManagement.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentManagement.JobApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulatorController : ControllerBase
{
    private readonly JobResultRepository _repo;
    private readonly ILogger<SimulatorController> _logger;

    private static readonly List<JobResultRequest> _scenarios = new()
    {
        new()
        {
            JobName = "OrderProcessingJob",
            JobId = "CTM-001",
            Status = "Failed",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            ErrorCode = "JOB_ABEND_U4038",
            ErrorMessage = "Connection timeout to /api/orders/update after 30s. Max retry count exceeded.",
            EndpointUrl = "/api/orders/update"
        },
        new()
        {
            JobName = "DailyReportJob",
            JobId = "CTM-002",
            Status = "Failed",
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow,
            ErrorCode = "JOB_ABEND_U0001",
            ErrorMessage = "NullReferenceException in ReportGenerator.Generate(). Object reference not set.",
            EndpointUrl = "/api/reports/daily"
        },
        new()
        {
            JobName = "DataSyncJob",
            JobId = "CTM-003",
            Status = "Failed",
            StartTime = DateTime.UtcNow.AddMinutes(-2),
            EndTime = DateTime.UtcNow,
            ErrorCode = "JOB_ABEND_U9999",
            ErrorMessage = "Database deadlock detected. Transaction rolled back.",
            EndpointUrl = "/api/sync/data"
        }
    };

    public SimulatorController(JobResultRepository repo, ILogger<SimulatorController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet("scenarios")]
    public IActionResult GetScenarios() => Ok(_scenarios);

    [HttpPost("trigger-random")]
    public async Task<IActionResult> TriggerRandom()
    {
        var scenario = _scenarios[Random.Shared.Next(_scenarios.Count)];
        return await SaveScenario(scenario);
    }

    [HttpPost("trigger/{index}")]
    public async Task<IActionResult> TriggerScenario(int index)
    {
        if (index < 0 || index >= _scenarios.Count)
            return BadRequest(new { message = $"Index must be between 0 and {_scenarios.Count - 1}" });
        return await SaveScenario(_scenarios[index]);
    }

    private async Task<IActionResult> SaveScenario(JobResultRequest scenario)
    {
        var s = new JobResultRequest
        {
            JobName = scenario.JobName,
            JobId = scenario.JobId + "-" + DateTime.UtcNow.Ticks,
            Status = scenario.Status,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            ErrorCode = scenario.ErrorCode,
            ErrorMessage = scenario.ErrorMessage,
            EndpointUrl = scenario.EndpointUrl,
            Metadata = scenario.Metadata
        };

        var result = new JobResult
        {
            JobName = s.JobName,
            JobId = s.JobId,
            Status = Enum.Parse<JobStatus>(s.Status, ignoreCase: true),
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            ErrorCode = s.ErrorCode,
            ErrorMessage = s.ErrorMessage,
            EndpointUrl = s.EndpointUrl,
            Metadata = s.Metadata ?? new()
        };

        await _repo.InsertAsync(result);
        _logger.LogInformation("Simulator triggered: {JobName}", result.JobName);
        return Ok(new { message = "Scenario triggered", jobName = result.JobName, id = result.Id });
    }
}
