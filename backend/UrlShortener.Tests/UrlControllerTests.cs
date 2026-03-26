using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mongo2Go;
using UrlShortener.API.Controllers;
using UrlShortener.API.Data;
using UrlShortener.API.Models;
using MongoDB.Driver;

namespace UrlShortener.Tests;

public class UrlControllerTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly MongoDbService _db;

    public UrlControllerTests()
    {
        // Spins up a real temporary MongoDB instance for each test class
        _runner = MongoDbRunner.Start();
        _db = new MongoDbService(_runner.ConnectionString);
    }

    public void Dispose()
    {
        _runner.Dispose();
    }

    private UrlController MakeController()
    {
        var ctrl = new UrlController(_db);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        ctrl.ControllerContext.HttpContext.Request.Scheme = "https";
        ctrl.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        return ctrl;
    }

    [Fact]
    public async Task Create_ValidUrl_ReturnsOk()
    {
        var result = await MakeController().Create(new CreateUrlRequest("https://example.com"));
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, await _db.ShortUrls.CountDocumentsAsync(_ => true));
    }

    [Fact]
    public async Task Create_EmptyUrl_ReturnsBadRequest()
    {
        var result = await MakeController().Create(new CreateUrlRequest(""));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidUrl_ReturnsBadRequest()
    {
        var result = await MakeController().Create(new CreateUrlRequest("not-a-url"));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_KnownCode_ReturnsRedirect()
    {
        await _db.ShortUrls.InsertOneAsync(new ShortUrl
        {
            Code = "abc123",
            OriginalUrl = "https://example.com"
        });

        var result = await MakeController().RedirectToUrl("abc123");
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirect.Url);
    }

    [Fact]
    public async Task Redirect_UnknownCode_ReturnsNotFound()
    {
        var result = await MakeController().RedirectToUrl("xxxxxx");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_IncrementsClickCount()
    {
        await _db.ShortUrls.InsertOneAsync(new ShortUrl
        {
            Code = "click1",
            OriginalUrl = "https://example.com",
            ClickCount = 0
        });

        await MakeController().RedirectToUrl("click1");

        var entry = await _db.ShortUrls.Find(u => u.Code == "click1").FirstAsync();
        Assert.Equal(1, entry.ClickCount);
    }

    [Fact]
    public async Task GetAll_ReturnsAllUrls()
    {
        await _db.ShortUrls.InsertManyAsync(new[]
        {
            new ShortUrl { Code = "aaa", OriginalUrl = "https://a.com" },
            new ShortUrl { Code = "bbb", OriginalUrl = "https://b.com" }
        });

        var result = await MakeController().GetAll();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ((IEnumerable<ShortUrl>)ok.Value!).Count());
    }
}