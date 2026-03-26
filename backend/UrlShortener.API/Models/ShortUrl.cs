using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UrlShortener.API.Models;

public class ShortUrl
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string OriginalUrl { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ClickCount { get; set; } = 0;
}