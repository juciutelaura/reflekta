using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;

namespace Reflekta.Api.Services;

public interface ICurrentUserService
{
    Task<Guid> GetOrCreateCurrentUserIdAsync(CancellationToken ct);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly ReflektaDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(ReflektaDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> GetOrCreateCurrentUserIdAsync(CancellationToken ct)
    {
        var externalAuthId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(externalAuthId))
            throw new UnauthorizedAccessException("No authenticated user found on the request.");

        var existing = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalAuthId == externalAuthId, ct);

        if (existing is not null)
            return existing.Id;

        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalAuthId = externalAuthId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);
        return user.Id;
    }
}