using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UrlShortener.API.Controllers;
using UrlShortener.API.Data;
using UrlShortener.API.Models;

namespace UrlShortener.Tests;

public class UrlControllerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a mock IMongoCollection that returns the provided list of ShortUrls
    /// for any Find() call, and accepts any InsertOne/UpdateOne calls.
    /// </summary>
    private static Mock<IMongoCollection<ShortUrl>> BuildCollectionMock(
        List<ShortUrl>? documents = null)
    {
        documents ??= new List<ShortUrl>();

        var mockCursor = new Mock<IAsyncCursor<ShortUrl>>();
        mockCursor.SetupSequence(c => c.MoveNextAsync(default))
                  .ReturnsAsync(true)
                  .ReturnsAsync(false);
        mockCursor.Setup(c => c.Current).Returns(documents);

        var mockCollection = new Mock<IMongoCollection<ShortUrl>>();

        // Find() — return cursor over our documents list
        mockCollection
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                It.IsAny<FindOptions<ShortUrl, ShortUrl>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        // InsertOneAsync — do nothing (success)
        mockCollection
            .Setup(c => c.InsertOneAsync(
                It.IsAny<ShortUrl>(),
                It.IsAny<InsertOneOptions>(),
                default))
            .Returns(Task.CompletedTask);

        // UpdateOneAsync — do nothing (success)
        mockCollection
            .Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                It.IsAny<UpdateDefinition<ShortUrl>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        return mockCollection;
    }

    /// <summary>
    /// Wraps a mock collection in a MongoDbService and wires up the HTTP context.
    /// </summary>
    private static UrlController BuildController(Mock<IMongoCollection<ShortUrl>> mockCollection)
    {
        var mockDb = new Mock<MongoDbService>("mongodb://localhost:27017");
        mockDb.Setup(d => d.ShortUrls).Returns(mockCollection.Object);

        var ctrl = new UrlController(mockDb.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        ctrl.ControllerContext.HttpContext.Request.Scheme = "https";
        ctrl.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        return ctrl;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidUrl_ReturnsOk()
    {
        // Empty list means no existing code clash — unique code generated on first try
        var mock = BuildCollectionMock(new List<ShortUrl>());
        var result = await BuildController(mock).Create(new CreateUrlRequest("https://example.com"));

        Assert.IsType<OkObjectResult>(result);
        // Verify InsertOneAsync was called exactly once
        mock.Verify(c => c.InsertOneAsync(
            It.IsAny<ShortUrl>(),
            It.IsAny<InsertOneOptions>(),
            default), Times.Once);
    }

    [Fact]
    public async Task Create_EmptyUrl_ReturnsBadRequest()
    {
        var mock = BuildCollectionMock();
        var result = await BuildController(mock).Create(new CreateUrlRequest(""));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidUrl_ReturnsBadRequest()
    {
        var mock = BuildCollectionMock();
        var result = await BuildController(mock).Create(new CreateUrlRequest("not-a-url"));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_KnownCode_ReturnsRedirect()
    {
        var docs = new List<ShortUrl>
        {
            new ShortUrl { Code = "abc123", OriginalUrl = "https://example.com" }
        };
        var mock = BuildCollectionMock(docs);
        var result = await BuildController(mock).RedirectToUrl("abc123");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirect.Url);
    }

    [Fact]
    public async Task Redirect_UnknownCode_ReturnsNotFound()
    {
        // Empty collection — no match for any code
        var mock = BuildCollectionMock(new List<ShortUrl>());
        var result = await BuildController(mock).RedirectToUrl("xxxxxx");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_IncrementsClickCount()
    {
        var docs = new List<ShortUrl>
        {
            new ShortUrl { Code = "click1", OriginalUrl = "https://example.com", ClickCount = 0 }
        };
        var mock = BuildCollectionMock(docs);
        await BuildController(mock).RedirectToUrl("click1");

        // Verify UpdateOneAsync was called to increment click count
        mock.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<ShortUrl>>(),
            It.IsAny<UpdateDefinition<ShortUrl>>(),
            It.IsAny<UpdateOptions>(),
            default), Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsAllUrls()
    {
        var docs = new List<ShortUrl>
        {
            new ShortUrl { Code = "aaa", OriginalUrl = "https://a.com" },
            new ShortUrl { Code = "bbb", OriginalUrl = "https://b.com" }
        };
        var mock = BuildCollectionMock(docs);
        var result = await BuildController(mock).GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ((IEnumerable<ShortUrl>)ok.Value!).Count());
    }
}