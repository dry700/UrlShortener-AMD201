using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.API.Controllers;
using UrlShortener.API.Data;
using UrlShortener.API.Models;

namespace UrlShortener.Tests;

public class UrlControllerTests
{
    private static AppDbContext GetDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name).Options;
        return new AppDbContext(options);
    }

    private static UrlController MakeController(AppDbContext db)
    {
        var ctrl = new UrlController(db);
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
        using var db = GetDb("valid");
        var result = await MakeController(db).Create(new CreateUrlRequest("https://example.com"));
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, await db.ShortUrls.CountAsync());
    }

    [Fact]
    public async Task Create_EmptyUrl_ReturnsBadRequest()
    {
        using var db = GetDb("empty");
        var result = await MakeController(db).Create(new CreateUrlRequest(""));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_InvalidUrl_ReturnsBadRequest()
    {
        using var db = GetDb("invalid");
        var result = await MakeController(db).Create(new CreateUrlRequest("not-a-url"));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_KnownCode_ReturnsRedirect()
    {
        using var db = GetDb("redirect_ok");
        db.ShortUrls.Add(new ShortUrl { Code = "abc123", OriginalUrl = "https://example.com" });
        await db.SaveChangesAsync();

        var result = await MakeController(db).RedirectToUrl("abc123");
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com", redirect.Url);
    }

    [Fact]
    public async Task Redirect_UnknownCode_ReturnsNotFound()
    {
        using var db = GetDb("redirect_404");
        var result = await MakeController(db).RedirectToUrl("xxxxxx");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Redirect_IncrementsClickCount()
    {
        using var db = GetDb("clicks");
        db.ShortUrls.Add(new ShortUrl { Code = "click1", OriginalUrl = "https://example.com", ClickCount = 0 });
        await db.SaveChangesAsync();

        await MakeController(db).RedirectToUrl("click1");
        Assert.Equal(1, (await db.ShortUrls.FirstAsync(u => u.Code == "click1")).ClickCount);
    }

    [Fact]
    public async Task GetAll_ReturnsAllUrls()
    {
        using var db = GetDb("getall");
        db.ShortUrls.AddRange(
            new ShortUrl { Code = "aaa", OriginalUrl = "https://a.com" },
            new ShortUrl { Code = "bbb", OriginalUrl = "https://b.com" }
        );
        await db.SaveChangesAsync();

        var result = await MakeController(db).GetAll();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, ((IEnumerable<ShortUrl>)ok.Value!).Count());
    }
}