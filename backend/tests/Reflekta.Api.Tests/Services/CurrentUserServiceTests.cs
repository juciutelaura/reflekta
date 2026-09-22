using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Services;
using Xunit;

namespace Reflekta.Api.Tests.Services;

public class CurrentUserServiceTests
{
    private static (ReflektaDbContext db, ICurrentUserService service) CreateSut(string externalAuthId)
    {
        var options = new DbContextOptionsBuilder<ReflektaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ReflektaDbContext(options);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", externalAuthId) }, "Test"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return (db, new CurrentUserService(db, accessor));
    }

    [Fact]
    public async Task GetOrCreateCurrentUserIdAsync_CreatesUserOnFirstCall()
    {
        var (db, service) = CreateSut("clerk_user_123");

        var userId = await service.GetOrCreateCurrentUserIdAsync(default);

        var stored = await db.Users.SingleAsync();
        Assert.Equal(userId, stored.Id);
        Assert.Equal("clerk_user_123", stored.ExternalAuthId);
    }

    [Fact]
    public async Task GetOrCreateCurrentUserIdAsync_ReturnsSameIdOnSecondCall()
    {
        var (db, service) = CreateSut("clerk_user_123");

        var first = await service.GetOrCreateCurrentUserIdAsync(default);
        var second = await service.GetOrCreateCurrentUserIdAsync(default);

        Assert.Equal(first, second);
        Assert.Equal(1, await db.Users.CountAsync());
    }
}
