using MongoDB.Driver;
using UrlShortener.API.Models;

namespace UrlShortener.API.Data;

public class MongoDbService
{
    private readonly IMongoCollection<ShortUrl> _collection;

    // Parameterless constructor required for Moq to create a mock subclass
    protected MongoDbService() { _collection = null!; }

    public MongoDbService(string connectionString)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("url_shortener");
        _collection = database.GetCollection<ShortUrl>("short_urls");

        try
        {
            var indexModel = new CreateIndexModel<ShortUrl>(
                Builders<ShortUrl>.IndexKeys.Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true }
            );
            _collection.Indexes.CreateOne(indexModel);
        }
        catch { /* Safe to skip in tests */ }
    }

    // virtual — allows Moq to override this property in tests
    public virtual IMongoCollection<ShortUrl> ShortUrls => _collection;
}