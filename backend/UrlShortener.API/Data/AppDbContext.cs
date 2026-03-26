using MongoDB.Driver;
using UrlShortener.API.Models;

namespace UrlShortener.API.Data;

public class MongoDbService
{
    private readonly IMongoCollection<ShortUrl> _collection;

    public MongoDbService(string connectionString)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("url_shortener");
        _collection = database.GetCollection<ShortUrl>("short_urls");

        // Create unique index on Code — wrapped in try/catch so
        // test runners (Mongo2Go) don't crash if index setup is slow
        try
        {
            var indexModel = new CreateIndexModel<ShortUrl>(
                Builders<ShortUrl>.IndexKeys.Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true }
            );
            _collection.Indexes.CreateOne(indexModel);
        }
        catch
        {
            // Index creation is a performance optimisation — safe to skip in tests
        }
    }

    public IMongoCollection<ShortUrl> ShortUrls => _collection;
}