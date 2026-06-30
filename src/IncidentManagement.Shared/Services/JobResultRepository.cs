using IncidentManagement.Shared.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace IncidentManagement.Shared.Services;

public class JobResultRepository
{
    private readonly IMongoCollection<JobResult> _collection;

    public JobResultRepository(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("MongoDB"));
        var db = client.GetDatabase(config["MongoDB:DatabaseName"]);
        _collection = db.GetCollection<JobResult>("job_results");
    }

    public async Task InsertAsync(JobResult result) =>
        await _collection.InsertOneAsync(result);

    public async Task<List<JobResult>> GetFailedUnprocessedAsync() =>
        await _collection.Find(r => r.Status == JobStatus.Failed && !r.IsProcessed).ToListAsync();

    public async Task MarkAsProcessedAsync(string id)
    {
        var update = Builders<JobResult>.Update.Set(r => r.IsProcessed, true);
        await _collection.UpdateOneAsync(r => r.Id == id, update);
    }

    public async Task<List<JobResult>> GetRecentAsync(int count = 50) =>
        await _collection.Find(_ => true)
            .SortByDescending(r => r.ReceivedAt)
            .Limit(count)
            .ToListAsync();
}
