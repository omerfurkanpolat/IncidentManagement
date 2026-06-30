using IncidentManagement.Shared.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace IncidentManagement.Shared.Services;

public class IncidentReportRepository
{
    private readonly IMongoCollection<IncidentReport> _collection;

    public IncidentReportRepository(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("MongoDB"));
        var db = client.GetDatabase(config["MongoDB:DatabaseName"]);
        _collection = db.GetCollection<IncidentReport>("incident_reports");
    }

    public async Task InsertAsync(IncidentReport report) =>
        await _collection.InsertOneAsync(report);

    public async Task<List<IncidentReport>> GetAllAsync() =>
        await _collection.Find(_ => true).SortByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<IncidentReport?> GetByIdAsync(string id) =>
        await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task UpdateStatusAsync(string id, IncidentStatus status)
    {
        var update = Builders<IncidentReport>.Update.Set(r => r.Status, status);
        await _collection.UpdateOneAsync(r => r.Id == id, update);
    }

    public async Task<long> CountTodayAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _collection.CountDocumentsAsync(r => r.CreatedAt >= today);
    }

    public async Task<long> CountOpenAsync() =>
        await _collection.CountDocumentsAsync(r => r.Status == IncidentStatus.Open);
}
