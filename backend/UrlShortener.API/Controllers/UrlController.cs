using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.API.Data;
using UrlShortener.API.Models;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlController : ControllerBase
{
    private readonly AppDbContext _db;

    public UrlController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/url — create a short URL
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
        while (await _db.ShortUrls.AnyAsync(u => u.Code == code));

        var shortUrl = new ShortUrl
        {
            OriginalUrl = request.OriginalUrl,
            Code = code,
            CreatedAt = DateTime.UtcNow
        };

        _db.ShortUrls.Add(shortUrl);
        await _db.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.Code,
            ShortLink = $"{baseUrl}/r/{code}",
            shortUrl.CreatedAt
        });
    }

    // GET /api/url — list all URLs
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var urls = await _db.ShortUrls
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
        return Ok(urls);
    }

    // GET /r/{code} — redirect to original URL
    [HttpGet("/r/{code}")]
    public async Task<IActionResult> RedirectToUrl(string code)
    {
        var entry = await _db.ShortUrls.FirstOrDefaultAsync(u => u.Code == code);
        if (entry == null)
            return NotFound(new { error = "Short URL not found." });

        entry.ClickCount++;
        await _db.SaveChangesAsync();

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