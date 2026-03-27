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
    private static Mock<IMongoCollection<ShortUrl>> BuildCollectionMock(
        List<ShortUrl>? documents = null)
    {
        documents ??= new List<ShortUrl>();

        var mockCursor = new Mock<IAsyncCursor<ShortUrl>>();
        mockCursor.Setup(c => c.Current).Returns(documents);
        mockCursor.SetupSequence(c => c.MoveNextAsync(default))
                  .ReturnsAsync(documents.Count > 0)
                  .ReturnsAsync(false);

        var mock = new Mock<IMongoCollection<ShortUrl>>();

        mock.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                It.IsAny<FindOptions<ShortUrl, ShortUrl>>(),
                default))
            .ReturnsAsync(mockCursor.Object);

        mock.Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                It.IsAny<CountOptions>(),
                default))
            .ReturnsAsync(0L);

        mock.Setup(c => c.InsertOneAsync(
                It.IsAny<ShortUrl>(),
                It.IsAny<InsertOneOptions>(),
                default))
            .Returns(Task.CompletedTask);

        mock.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                It.IsAny<UpdateDefinition<ShortUrl>>(),
                It.IsAny<UpdateOptions>(),
                default))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        mock.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                default))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        return mock;
    }

    private static UrlController BuildController(Mock<IMongoCollection<ShortUrl>> mock)
    {
        var mockDb = new Mock<MongoDbService>("mongodb://localhost:27017");
        mockDb.Setup(d => d.ShortUrls).Returns(mock.Object);

        var ctrl = new UrlController(mockDb.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        ctrl.ControllerContext.HttpContext.Request.Scheme = "https";
        ctrl.ControllerContext.HttpContext.Request.Host   = new HostString("localhost");
        return ctrl;
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidUrl_ReturnsCreated()
    {
        var mock   = BuildCollectionMock(new List<ShortUrl>());
        var result = await BuildController(mock).Create(new CreateUrlRequest("https://example.com"));
        Assert.IsType<CreatedAtActionResult>(result);
        mock.Verify(c => c.InsertOneAsync(
            It.IsAny<ShortUrl>(), It.IsAny<InsertOneOptions>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_EmptyUrl_ReturnsBadRequest()
    {
        var result = await BuildController(BuildCollectionMock()).Create(new CreateUrlRequest(""));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidUrl_ReturnsBadRequest()
    {
        var result = await BuildController(BuildCollectionMock()).Create(new CreateUrlRequest("not-a-url"));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── READ ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllUrls()
    {
        var docs = new List<ShortUrl>
        {
            new ShortUrl { Code = "aaa", OriginalUrl = "https://a.com" },
            new ShortUrl { Code = "bbb", OriginalUrl = "https://b.com" }
        };
        var result = await BuildController(BuildCollectionMock(docs)).GetAll();
        var ok     = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ((IEnumerable<ShortUrl>)ok.Value!).Count());
    }

    [Fact]
    public async Task GetByCode_KnownCode_ReturnsOk()
    {
        var docs   = new List<ShortUrl> { new ShortUrl { Code = "abc123", OriginalUrl = "https://example.com" } };
        var result = await BuildController(BuildCollectionMock(docs)).GetByCode("abc123");
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByCode_UnknownCode_ReturnsNotFound()
    {
        var result = await BuildController(BuildCollectionMock(new List<ShortUrl>())).GetByCode("xxxxxx");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── UPDATE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidUrl_ReturnsOk()
    {
        var docs = new List<ShortUrl> { new ShortUrl { Code = "abc123", OriginalUrl = "https://old.com" } };
        var mock = BuildCollectionMock(docs);
        var result = await BuildController(mock).Update("abc123", new UpdateUrlRequest("https://new.com"));
        Assert.IsType<OkObjectResult>(result);
        mock.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<ShortUrl>>(),
            It.IsAny<UpdateDefinition<ShortUrl>>(),
            It.IsAny<UpdateOptions>(), default), Times.Once);
    }

    [Fact]
    public async Task Update_EmptyUrl_ReturnsBadRequest()
    {
        var result = await BuildController(BuildCollectionMock()).Update("abc123", new UpdateUrlRequest(""));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_UnknownCode_ReturnsNotFound()
    {
        var mock = BuildCollectionMock(new List<ShortUrl>());
        mock.Setup(c => c.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(),
                It.IsAny<UpdateDefinition<ShortUrl>>(),
                It.IsAny<UpdateOptions>(), default))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        var result = await BuildController(mock).Update("xxxxxx", new UpdateUrlRequest("https://example.com"));
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_KnownCode_ReturnsNoContent()
    {
        var result = await BuildController(BuildCollectionMock()).Delete("abc123");
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_UnknownCode_ReturnsNotFound()
    {
        var mock = BuildCollectionMock();
        mock.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<ShortUrl>>(), default))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        var result = await BuildController(mock).Delete("xxxxxx");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── REDIRECT ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Redirect_KnownCode_ReturnsRedirect()
    {
        var docs   = new List<ShortUrl> { new ShortUrl { Code = "abc123", OriginalUrl = "https://example.com" } };
        var result = await BuildController(BuildCollectionMock(docs)).RedirectToUrl("abc123");
        var r      = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", r.Url);
    }

    [Fact]
    public async Task Redirect_UnknownCode_ReturnsNotFound()
    {
        var result = await BuildController(BuildCollectionMock(new List<ShortUrl>())).RedirectToUrl("xxxxxx");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_IncrementsClickCount()
    {
        var docs = new List<ShortUrl> { new ShortUrl { Code = "click1", OriginalUrl = "https://example.com" } };
        var mock = BuildCollectionMock(docs);
        await BuildController(mock).RedirectToUrl("click1");
        mock.Verify(c => c.UpdateOneAsync(
            It.IsAny<FilterDefinition<ShortUrl>>(),
            It.IsAny<UpdateDefinition<ShortUrl>>(),
            It.IsAny<UpdateOptions>(), default), Times.Once);
    }
}