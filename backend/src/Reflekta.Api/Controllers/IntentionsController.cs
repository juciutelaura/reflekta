using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Models;
using Reflekta.Api.Services;

namespace Reflekta.Api.Controllers;

public record CreateIntentionRequest(string Text);
public record IntentionDto(Guid Id, string OriginalText, string? ClarifiedText, DateTimeOffset CreatedAt);

[ApiController]
[Route("api/intentions")]
[Authorize]
public class IntentionsController : ControllerBase
{
    private readonly ReflektaDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public IntentionsController(ReflektaDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<IntentionDto>> Create([FromBody] CreateIntentionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Intention text is required.");

        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var intention = new Intention
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalText = request.Text.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Intentions.Add(intention);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ToDto(intention));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IntentionDto>> GetById(Guid id, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);
        var intention = await _dbContext.Intentions.FirstOrDefaultAsync(i => i.Id == id, ct);

        if (intention is null)
            return NotFound();

        if (intention.UserId != userId)
            return Forbid();

        return Ok(ToDto(intention));
    }

    private static IntentionDto ToDto(Intention intention) =>
        new(intention.Id, intention.OriginalText, intention.ClarifiedText, intention.CreatedAt);
}