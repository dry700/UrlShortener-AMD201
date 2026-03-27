using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UrlShortener.API.Data;
using UrlShortener.API.Models;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlController : ControllerBase
{
    private readonly IMongoCollection<ShortUrl> _collection;

    public UrlController(MongoDbService db)
    {
        _collection = db.ShortUrls;
    }

    // ── CREATE ────────────────────────────────────────────────────────────────
    // POST /api/url
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalUrl))
            return BadRequest(new { error = "URL is required." });

        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest(new { error = "Invalid URL. Must start with http:// or https://" });

        string code;
        do { code = GenerateCode(); }
        while (await _collection.CountDocumentsAsync(u => u.Code == code) > 0);

        var shortUrl = new ShortUrl
        {
            OriginalUrl = request.OriginalUrl,
            Code        = code,
            CreatedAt   = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(shortUrl);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return CreatedAtAction(nameof(GetByCode), new { code }, new
        {
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.Code,
            ShortLink  = $"{baseUrl}/r/{code}",
            shortUrl.CreatedAt,
            shortUrl.ClickCount
        });
    }

    // ── READ ALL ──────────────────────────────────────────────────────────────
    // GET /api/url
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var urls = await _collection
            .Find(_ => true)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync();
        return Ok(urls);
    }

    // ── READ ONE ──────────────────────────────────────────────────────────────
    // GET /api/url/{code}
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var entry = await _collection.Find(u => u.Code == code).FirstOrDefaultAsync();
        if (entry == null)
            return NotFound(new { error = $"No URL found with code '{code}'." });

        return Ok(entry);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────
    // PUT /api/url/{code}
    [HttpPut("{code}")]
    public async Task<IActionResult> Update(string code, [FromBody] UpdateUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalUrl))
            return BadRequest(new { error = "URL is required." });

        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest(new { error = "Invalid URL. Must start with http:// or https://" });

        var update = Builders<ShortUrl>.Update
            .Set(u => u.OriginalUrl, request.OriginalUrl);

        var result = await _collection.UpdateOneAsync(u => u.Code == code, update);

        if (result.MatchedCount == 0)
            return NotFound(new { error = $"No URL found with code '{code}'." });

        var updated = await _collection.Find(u => u.Code == code).FirstOrDefaultAsync();
        return Ok(updated);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────
    // DELETE /api/url/{code}
    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code)
    {
        var result = await _collection.DeleteOneAsync(u => u.Code == code);

        if (result.DeletedCount == 0)
            return NotFound(new { error = $"No URL found with code '{code}'." });

        return NoContent();
    }

    // ── REDIRECT ──────────────────────────────────────────────────────────────
    // GET /r/{code}
    [HttpGet("/r/{code}")]
    public async Task<IActionResult> RedirectToUrl(string code)
    {
        var entry = await _collection.Find(u => u.Code == code).FirstOrDefaultAsync();
        if (entry == null)
            return NotFound(new { error = "Short URL not found." });

        var update = Builders<ShortUrl>.Update.Inc(u => u.ClickCount, 1);
        await _collection.UpdateOneAsync(u => u.Code == code, update);

        return Redirect(entry.OriginalUrl);
    }

    private static string GenerateCode()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}

public record CreateUrlRequest(string OriginalUrl);
public record UpdateUrlRequest(string OriginalUrl);