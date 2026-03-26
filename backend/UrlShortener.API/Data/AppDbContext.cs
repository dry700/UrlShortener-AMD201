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

        // Create unique index on Code field for fast lookups
        var indexModel = new CreateIndexModel<ShortUrl>(
            Builders<ShortUrl>.IndexKeys.Ascending(x => x.Code),
            new CreateIndexOptions { Unique = true }
        );
        _collection.Indexes.CreateOne(indexModel);
    }

    public IMongoCollection<ShortUrl> ShortUrls => _collection;
}