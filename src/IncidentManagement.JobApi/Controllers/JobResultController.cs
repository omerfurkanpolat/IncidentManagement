using IncidentManagement.Shared.Models;
using IncidentManagement.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace IncidentManagement.JobApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobResultController : ControllerBase
{
    private readonly JobResultRepository _repo;
    private readonly ILogger<JobResultController> _logger;

    public JobResultController(JobResultRepository repo, ILogger<JobResultController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveJobResult([FromBody] JobResultRequest request)
    {
        var result = new JobResult
        {
            JobName = request.JobName,
            JobId = request.JobId,
            Status = Enum.Parse<JobStatus>(request.Status, ignoreCase: true),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ErrorCode = request.ErrorCode,
            ErrorMessage = request.ErrorMessage,
            ErrorStackTrace = request.ErrorStackTrace,
            EndpointUrl = request.EndpointUrl,
            Metadata = request.Metadata ?? new()
        };

        await _repo.InsertAsync(result);
        _logger.LogInformation("Job result received: {JobName} - {Status}", result.JobName, result.Status);

        return Ok(new { message = "Job result received", id = result.Id });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
    {
        var results = await _repo.GetRecentAsync(count);
        return Ok(results);
    }
}
