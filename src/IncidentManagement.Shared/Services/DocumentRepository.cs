using IncidentManagement.Shared.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace IncidentManagement.Shared.Services;

public class DocumentRepository
{
    private readonly IMongoCollection<Document> _collection;

    public DocumentRepository(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("MongoDB"));
        var db = client.GetDatabase(config["MongoDB:DatabaseName"]);
        _collection = db.GetCollection<Document>("documents");
    }

    public async Task<List<Document>> GetAllAsync() =>
        await _collection.Find(_ => true).SortByDescending(d => d.CreatedAt).ToListAsync();

    public async Task<List<Document>> GetPendingAsync() =>
        await _collection.Find(d => d.Status == DocumentStatus.Pending).ToListAsync();

    public async Task<List<Document>> GetApprovedNotIndexedAsync() =>
        await _collection.Find(d => d.Status == DocumentStatus.Approved && !d.IsIndexed).ToListAsync();

    public async Task InsertAsync(Document doc) =>
        await _collection.InsertOneAsync(doc);

    public async Task ApproveAsync(string id, string approvedBy)
    {
        var update = Builders<Document>.Update
            .Set(d => d.Status, DocumentStatus.Approved)
            .Set(d => d.ApprovedAt, DateTime.UtcNow)
            .Set(d => d.ApprovedBy, approvedBy);
        await _collection.UpdateOneAsync(d => d.Id == id, update);
    }

    public async Task RejectAsync(string id)
    {
        var update = Builders<Document>.Update.Set(d => d.Status, DocumentStatus.Rejected);
        await _collection.UpdateOneAsync(d => d.Id == id, update);
    }

    public async Task MarkAsIndexedAsync(string id)
    {
        var update = Builders<Document>.Update
            .Set(d => d.IsIndexed, true)
            .Set(d => d.IndexedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(d => d.Id == id, update);
    }

    public async Task<long> CountAsync() =>
        await _collection.CountDocumentsAsync(_ => true);
}
