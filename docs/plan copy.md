
And inside `OnModelCreating`, add (alongside the existing `modelBuilder.Entity<...>` blocks):

```csharp
modelBuilder.Entity<Reflection>(e =>
{
    e.HasIndex(r => r.PlayedCardId).IsUnique();
});
```

The unique index is what makes "one `Reflection` per `PlayedCard`" a database-enforced fact, not just an application convention.

- [ ] **Step 5: Run the test to verify it passes**

```bash
dotnet test tests/Reflekta.Api.Tests --filter ReflectionPersistenceTests
```

Expected: PASS.

- [ ] **Step 6: Create and inspect the migration**

```bash
cd src/Reflekta.Api
dotnet ef migrations add AddReflection --project . --startup-project .
cat Migrations/*_AddReflection.cs
```

Confirm it creates a `Reflections` table with a unique index on `PlayedCardId`.

- [ ] **Step 7: Run the full backend test suite**

```bash
cd ../..
dotnet test
```

Expected: all tests (Phase 1's 20 plus this task's new one) pass.

- [ ] **Step 8: Commit**

```bash
git add backend
git commit -m "feat: add Reflection entity, DbContext registration, and migration

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 11: Submit a reflection, and require one before the next roll

**Files:**
- Modify: `backend/src/Reflekta.Api/Controllers/JourneysController.cs` (new endpoint, new DTOs, new guard in `Roll`)
- Modify: `backend/tests/Reflekta.Api.Tests/Controllers/JourneysControllerTests.cs`

**Interfaces:**
- Consumes: `Reflection` model, `ReflektaDbContext.Reflections` (Task 10).
- Produces:
  - `POST /api/journeys/{journeyId}/reflection` body `{ "text": string }` → `ReflectionDto` or `400`/`403`/`404`.
  - `record ReflectionDto(Guid Id, Guid PlayedCardId, string Text, DateTimeOffset CreatedAt)` — consumed by Task 13's frontend work.
  - `Roll` now returns `400 "Write a reflection before continuing the journey."` if the most recent `PlayedCard` has no `Reflection` yet.

- [ ] **Step 1: Write the failing tests**

Add to `backend/tests/Reflekta.Api.Tests/Controllers/JourneysControllerTests.cs` (inside the existing `JourneysControllerTests` class, alongside the Phase 1 tests — keep those unchanged):

```csharp
    [Fact]
    public async Task Roll_WithoutReflectingOnThePreviousCard_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        var secondRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);

        Assert.Equal(HttpStatusCode.BadRequest, secondRoll.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_ThenRoll_Succeeds()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        var reflectionResponse = await _client.PostAsJsonAsync(
            $"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("This made me think of my old job."));
        reflectionResponse.EnsureSuccessStatusCode();
        var reflection = await reflectionResponse.Content.ReadFromJsonAsync<ReflectionDto>();
        Assert.Equal("This made me think of my old job.", reflection!.Text);

        var secondRoll = await _client.PostAsync($"/api/journeys/{journey.Id}/roll", null);
        Assert.True(secondRoll.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SubmitReflection_Twice_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("First."));

        var second = await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Second."));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_WithBlankText_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_BeforeAnyRoll_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        var response = await _client.PostAsJsonAsync($"/api/journeys/{journey!.Id}/reflection", new SubmitReflectionRequest("Too early."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReflection_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        AuthenticateAs("user-2");
        var response = await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Not mine."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
cd backend
dotnet test tests/Reflekta.Api.Tests --filter JourneysControllerTests
```

Expected: FAIL to compile — `SubmitReflectionRequest`/`ReflectionDto` and the `/reflection` route do not exist yet, and `Roll_WithoutReflectingOnThePreviousCard_ReturnsBadRequest` fails against the current unguarded `Roll`.

- [ ] **Step 3: Add the DTOs, the new endpoint, and the roll guard**

In `backend/src/Reflekta.Api/Controllers/JourneysController.cs`, add these two records next to the existing ones (`CreateJourneyRequest`, `JourneyDto`, `RollResultDto`):

```csharp
public record SubmitReflectionRequest(string Text);
public record ReflectionDto(Guid Id, Guid PlayedCardId, string Text, DateTimeOffset CreatedAt);
```

Replace the body of `Roll` (keep the method signature and the `NotFound`/`Forbid`/"not active" checks exactly as they are) by inserting this guard immediately after the "journey is not active" check and before `var cards = ...`:

```csharp
        if (journey.PlayedCards.Count > 0)
        {
            var lastPlayedCardId = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).First().Id;
            var hasReflection = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayedCardId, ct);
            if (!hasReflection)
                return BadRequest("Write a reflection before continuing the journey.");
        }
```

Add the new endpoint, after `Roll` and before `GetCurrentCard`:

```csharp
    [HttpPost("{journeyId:guid}/reflection")]
    public async Task<ActionResult<ReflectionDto>> SubmitReflection(
        Guid journeyId, [FromBody] SubmitReflectionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Reflection text is required.");

        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed is null)
            return BadRequest("Roll before writing a reflection.");

        var alreadyReflected = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayed.Id, ct);
        if (alreadyReflected)
            return BadRequest("This card already has a reflection.");

        var reflection = new Reflection
        {
            Id = Guid.NewGuid(),
            PlayedCardId = lastPlayed.Id,
            Text = request.Text.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Reflections.Add(reflection);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new ReflectionDto(reflection.Id, reflection.PlayedCardId, reflection.Text, reflection.CreatedAt));
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test tests/Reflekta.Api.Tests --filter JourneysControllerTests
```

Expected: PASS — the 4 Phase 1 tests plus this task's 6 new ones (10 total).

- [ ] **Step 5: Commit**

```bash
cd ../..
git add backend
git commit -m "feat: require a reflection before continuing a journey's next roll

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 12: Complete a journey

**Files:**
- Modify: `backend/src/Reflekta.Api/Controllers/JourneysController.cs` (`JourneyDto` gains `CompletedAt`, new `/complete` endpoint)
- Modify: `backend/tests/Reflekta.Api.Tests/Controllers/JourneysControllerTests.cs`

**Interfaces:**
- Produces:
  - `record JourneyDto(Guid Id, Guid IntentionId, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt)` — the `CompletedAt` field is new; every existing call site that constructs a `JourneyDto` must be updated.
  - `POST /api/journeys/{journeyId}/complete` → updated `JourneyDto` with `Status: "Completed"` or `400`/`403`/`404`.

- [ ] **Step 1: Write the failing tests**

Add to `JourneysControllerTests`:

```csharp
    [Fact]
    public async Task CompleteJourney_AfterReflecting_SetsStatusToCompleted()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Enough for today."));

        var completeResponse = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<JourneyDto>();
        Assert.Equal("Completed", completed!.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task CompleteJourney_WithoutAnyRoll_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();

        var response = await _client.PostAsync($"/api/journeys/{journey!.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteJourney_WithUnreflectedLatestCard_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);

        var response = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteJourney_AlreadyCompleted_ReturnsBadRequest()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Done."));
        await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        var response = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteJourney_OnAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var intentionId = await CreateIntentionAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("Mine."));

        AuthenticateAs("user-2");
        var response = await _client.PostAsync($"/api/journeys/{journey.Id}/complete", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
cd backend
dotnet test tests/Reflekta.Api.Tests --filter JourneysControllerTests
```

Expected: FAIL — `/complete` route doesn't exist, and `JourneyDto` has no `CompletedAt` (compile error in the tests referencing `completed.CompletedAt`).

- [ ] **Step 3: Update `JourneyDto` and its two existing call sites, then add `/complete`**

In `JourneysController.cs`, change the record:

```csharp
public record JourneyDto(Guid Id, Guid IntentionId, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);
```

Update the `Create` action's return statement (the only other place a `JourneyDto` is constructed):

```csharp
        return Ok(new JourneyDto(journey.Id, journey.IntentionId, journey.Status.ToString(), journey.StartedAt, journey.CompletedAt));
```

Add the new endpoint after `SubmitReflection`:

```csharp
    [HttpPost("{journeyId:guid}/complete")]
    public async Task<ActionResult<JourneyDto>> Complete(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.PlayedCards)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();
        if (journey.Status != JourneyStatus.Active)
            return BadRequest("Journey is already completed.");

        var lastPlayed = journey.PlayedCards.OrderByDescending(pc => pc.SequenceNumber).FirstOrDefault();
        if (lastPlayed is null)
            return BadRequest("Roll at least once before completing the journey.");

        var hasReflection = await _dbContext.Reflections.AnyAsync(r => r.PlayedCardId == lastPlayed.Id, ct);
        if (!hasReflection)
            return BadRequest("Write a reflection before completing the journey.");

        journey.Status = JourneyStatus.Completed;
        journey.CompletedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new JourneyDto(journey.Id, journey.IntentionId, journey.Status.ToString(), journey.StartedAt, journey.CompletedAt));
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test tests/Reflekta.Api.Tests --filter JourneysControllerTests
```

Expected: PASS — 15 tests in this class (4 from Phase 1 + 6 from Task 11 + 5 new).

- [ ] **Step 5: Run the full backend suite**

```bash
cd ../..
dotnet test
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add backend
git commit -m "feat: add journey completion with lifecycle guards

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 13: Journey list and detail endpoints (history)

**Files:**
- Create: `backend/src/Reflekta.Api/Models/Journey.cs` → modify (add `Intention` navigation property)
- Modify: `backend/src/Reflekta.Api/Data/ReflektaDbContext.cs` (configure the new navigation as a real foreign key)
- Modify: `backend/src/Reflekta.Api/Controllers/JourneysController.cs` (new DTOs, `List` and `GetById` actions)
- Create: `backend/tests/Reflekta.Api.Tests/Controllers/JourneyHistoryTests.cs`

**Interfaces:**
- Produces:
  - `GET /api/journeys` → `List<JourneySummaryDto>`, the current user's journeys ordered newest-first.
  - `GET /api/journeys/{journeyId}` → `JourneyDetailDto` or `403`/`404`.
  - `record JourneySummaryDto(Guid Id, string IntentionText, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CardCount)`
  - `record PlayedCardDetailDto(Guid Id, int SequenceNumber, int DiceResult, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, string? ReflectionText)`
  - `record JourneyDetailDto(Guid Id, string IntentionText, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, List<PlayedCardDetailDto> PlayedCards)`

- [ ] **Step 1: Write the failing tests**

`backend/tests/Reflekta.Api.Tests/Controllers/JourneyHistoryTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Reflekta.Api.Controllers;
using Reflekta.Api.Data;
using Reflekta.Api.Tests.TestInfrastructure;
using Xunit;

namespace Reflekta.Api.Tests.Controllers;

public class JourneyHistoryTests : IAsyncLifetime
{
    private readonly ReflektaWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReflektaDbContext>();
        await CardSeeder.SeedAsync(dbContext);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private void AuthenticateAs(string userId)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", userId);
    }

    private async Task<Guid> CreateIntentionAsync(string text)
    {
        var response = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest(text));
        var dto = await response.Content.ReadFromJsonAsync<IntentionDto>();
        return dto!.Id;
    }

    private async Task<Guid> CreateReflectedJourneyAsync(string intentionText)
    {
        var intentionId = await CreateIntentionAsync(intentionText);
        var createResponse = await _client.PostAsJsonAsync("/api/journeys", new CreateJourneyRequest(intentionId));
        var journey = await createResponse.Content.ReadFromJsonAsync<JourneyDto>();
        await _client.PostAsync($"/api/journeys/{journey!.Id}/roll", null);
        await _client.PostAsJsonAsync($"/api/journeys/{journey.Id}/reflection", new SubmitReflectionRequest("A reflection."));
        return journey.Id;
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCurrentUsersJourneys_NewestFirst()
    {
        AuthenticateAs("user-1");
        var firstId = await CreateReflectedJourneyAsync("First intention");
        var secondId = await CreateReflectedJourneyAsync("Second intention");

        AuthenticateAs("user-2");
        await CreateReflectedJourneyAsync("Someone else's intention");

        AuthenticateAs("user-1");
        var response = await _client.GetAsync("/api/journeys");

        response.EnsureSuccessStatusCode();
        var journeys = await response.Content.ReadFromJsonAsync<List<JourneySummaryDto>>();

        Assert.Equal(2, journeys!.Count);
        Assert.Equal(secondId, journeys[0].Id);
        Assert.Equal(firstId, journeys[1].Id);
        Assert.Equal("Second intention", journeys[0].IntentionText);
        Assert.Equal(1, journeys[0].CardCount);
    }

    [Fact]
    public async Task GetById_ReturnsPlayedCardsWithTheirReflections()
    {
        AuthenticateAs("user-1");
        var journeyId = await CreateReflectedJourneyAsync("My intention");

        var response = await _client.GetAsync($"/api/journeys/{journeyId}");

        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<JourneyDetailDto>();

        Assert.Equal("My intention", detail!.IntentionText);
        Assert.Single(detail.PlayedCards);
        Assert.Equal("A reflection.", detail.PlayedCards[0].ReflectionText);
        Assert.False(string.IsNullOrEmpty(detail.PlayedCards[0].CardTitle));
    }

    [Fact]
    public async Task GetById_ForAnotherUsersJourney_ReturnsForbidden()
    {
        AuthenticateAs("user-1");
        var journeyId = await CreateReflectedJourneyAsync("Mine");

        AuthenticateAs("user-2");
        var response = await _client.GetAsync($"/api/journeys/{journeyId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownJourney_ReturnsNotFound()
    {
        AuthenticateAs("user-1");

        var response = await _client.GetAsync($"/api/journeys/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
cd backend
dotnet test tests/Reflekta.Api.Tests --filter JourneyHistoryTests
```

Expected: FAIL to compile — `JourneySummaryDto`/`JourneyDetailDto`/`PlayedCardDetailDto` don't exist, and `GET /api/journeys` / `GET /api/journeys/{id}` aren't mapped.

- [ ] **Step 3: Add the `Intention` navigation property**

`backend/src/Reflekta.Api/Models/Journey.cs` — add one property (keep everything else unchanged):

```csharp
namespace Reflekta.Api.Models;

public enum JourneyStatus
{
    Active,
    Completed
}

public class Journey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid IntentionId { get; set; }
    public Intention? Intention { get; set; }
    public JourneyStatus Status { get; set; } = JourneyStatus.Active;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public List<PlayedCard> PlayedCards { get; set; } = new();
}
```

In `ReflektaDbContext.cs`, inside the existing `modelBuilder.Entity<Journey>(e => { ... })` block, add:

```csharp
            e.HasOne(j => j.Intention)
                .WithMany()
                .HasForeignKey(j => j.IntentionId);
```

(This adds a real foreign-key constraint that `IntentionId` never had before — a genuine data-integrity improvement, not just a convenience for this query.)

- [ ] **Step 4: Add the DTOs and the two actions**

In `JourneysController.cs`, add these records next to the others:

```csharp
public record JourneySummaryDto(Guid Id, string IntentionText, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int CardCount);
public record PlayedCardDetailDto(Guid Id, int SequenceNumber, int DiceResult, string CardTitle, string CardWisdomText, string CardReflectionPrompt, List<string> CardThemes, string? ReflectionText);
public record JourneyDetailDto(Guid Id, string IntentionText, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, List<PlayedCardDetailDto> PlayedCards);
```

Add the two actions after `Complete`:

```csharp
    [HttpGet]
    public async Task<ActionResult<List<JourneySummaryDto>>> List(CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journeys = await _dbContext.Journeys
            .Include(j => j.Intention)
            .Include(j => j.PlayedCards)
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync(ct);

        var result = journeys
            .Select(j => new JourneySummaryDto(
                j.Id, j.Intention!.OriginalText, j.Status.ToString(), j.StartedAt, j.CompletedAt, j.PlayedCards.Count))
            .ToList();

        return Ok(result);
    }

    [HttpGet("{journeyId:guid}")]
    public async Task<ActionResult<JourneyDetailDto>> GetById(Guid journeyId, CancellationToken ct)
    {
        var userId = await _currentUserService.GetOrCreateCurrentUserIdAsync(ct);

        var journey = await _dbContext.Journeys
            .Include(j => j.Intention)
            .Include(j => j.PlayedCards).ThenInclude(pc => pc.Card)
            .FirstOrDefaultAsync(j => j.Id == journeyId, ct);

        if (journey is null)
            return NotFound();
        if (journey.UserId != userId)
            return Forbid();

        var playedCardIds = journey.PlayedCards.Select(pc => pc.Id).ToList();
        var reflectionsByPlayedCardId = await _dbContext.Reflections
            .Where(r => playedCardIds.Contains(r.PlayedCardId))
            .ToDictionaryAsync(r => r.PlayedCardId, r => r.Text, ct);

        var playedCards = journey.PlayedCards
            .OrderBy(pc => pc.SequenceNumber)
            .Select(pc => new PlayedCardDetailDto(
                pc.Id, pc.SequenceNumber, pc.DiceResult, pc.Card!.Title, pc.Card.WisdomText,
                pc.Card.ReflectionPrompt, pc.Card.Themes,
                reflectionsByPlayedCardId.GetValueOrDefault(pc.Id)))
            .ToList();

        return Ok(new JourneyDetailDto(
            journey.Id, journey.Intention!.OriginalText, journey.Status.ToString(),
            journey.StartedAt, journey.CompletedAt, playedCards));
    }
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
dotnet test tests/Reflekta.Api.Tests --filter JourneyHistoryTests
```

Expected: PASS (4 tests).

- [ ] **Step 6: Create the migration for the new foreign key**

```bash
cd src/Reflekta.Api
dotnet ef migrations add AddJourneyIntentionForeignKey --project . --startup-project .
cat Migrations/*_AddJourneyIntentionForeignKey.cs
```

Confirm it adds a foreign-key constraint from `Journeys.IntentionId` to `Intentions.Id` — it should not need to touch any column types, since `IntentionId` already exists.

- [ ] **Step 7: Run the full backend suite**

```bash
cd ../..
dotnet test
```

Expected: all pass (Phase 1's 20 + Task 10's 1 + Task 11's 6 + Task 12's 5 + Task 13's 4 = 36).

- [ ] **Step 8: Commit**

```bash
git add backend
git commit -m "feat: add journey list and detail endpoints for history

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 14: Frontend — reflect, then continue or complete the journey

**Files:**
- Modify: `frontend/src/lib/apiClient.ts` (new DTOs, `submitReflection`, `completeJourney`; `JourneyDto` gains `completedAt`)
- Modify: `frontend/src/pages/JourneyPage.tsx`
- Modify: `frontend/src/pages/__tests__/JourneyPage.test.tsx`

**Interfaces:**
- Consumes: `POST /.../reflection`, `POST /.../complete` (Tasks 11-12), whose shapes mirror `ReflectionDto`/`JourneyDto` exactly.
- Produces: `JourneyPage` gains a required `onJourneyCompleted: () => void` prop, called after a successful `completeJourney`. This is consumed by Task 15's routing change in `App.tsx`.

- [ ] **Step 1: Update `apiClient.ts`**

Add to the interfaces section:

```typescript
export interface ReflectionDto {
  id: string;
  playedCardId: string;
  text: string;
  createdAt: string;
}
```

Change `JourneyDto` to add the new field:

```typescript
export interface JourneyDto {
  id: string;
  intentionId: string;
  status: "Active" | "Completed";
  startedAt: string;
  completedAt: string | null;
}
```

Add two methods inside `createApiClient`'s returned object, next to `rollDice`:

```typescript
    submitReflection: (journeyId: string, text: string) =>
      request<ReflectionDto>(getToken, `/api/journeys/${journeyId}/reflection`, {
        method: "POST",
        body: JSON.stringify({ text }),
      }),

    completeJourney: (journeyId: string) =>
      request<JourneyDto>(getToken, `/api/journeys/${journeyId}/complete`, { method: "POST" }),
```

- [ ] **Step 2: Write the failing test for the reflect → continue/complete flow**

Replace the entire contents of `frontend/src/pages/__tests__/JourneyPage.test.tsx` with:

```typescript
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { JourneyPage } from "../JourneyPage";
import type { ApiClient } from "../../lib/apiClient";

function createApiClientMock(): ApiClient {
  return {
    createIntention: vi.fn(),
    createJourney: vi.fn(),
    rollDice: vi.fn().mockResolvedValue({
      diceResult: 4,
      cardId: "card-1",
      cardTitle: "Control",
      cardWisdomText: "Notice where you try to hold on tightly.",
      cardReflectionPrompt: "What are you trying hardest to control today?",
      cardThemes: ["Control"],
      sequenceNumber: 1,
    }),
    getCurrentCard: vi.fn(),
    submitReflection: vi.fn().mockResolvedValue({
      id: "reflection-1",
      playedCardId: "card-1",
      text: "I noticed I grip tightly onto plans.",
      createdAt: "",
    }),
    completeJourney: vi.fn().mockResolvedValue({
      id: "journey-1",
      intentionId: "intention-1",
      status: "Completed",
      startedAt: "",
      completedAt: "2026-01-01T00:00:00Z",
    }),
  } as unknown as ApiClient;
}

describe("JourneyPage", () => {
  it("rolls the dice and displays the resulting card", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));

    await waitFor(() => expect(screen.getByText("Control")).toBeInTheDocument());
    expect(screen.getByText(/notice where you try to hold on tightly/i)).toBeInTheDocument();
    expect(screen.getByText(/what are you trying hardest to control today/i)).toBeInTheDocument();
    expect(apiClient.rollDice).toHaveBeenCalledWith("journey-1");
  });

  it("shows a reflection form after the card is revealed, and hides it once submitted", async () => {
    const apiClient = createApiClientMock();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={vi.fn()} />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));
    await waitFor(() => expect(screen.getByLabelText(/your reflection/i)).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText(/your reflection/i), {
      target: { value: "I noticed I grip tightly onto plans." },
    });
    fireEvent.click(screen.getByRole("button", { name: /save reflection/i }));

    await waitFor(() =>
      expect(apiClient.submitReflection).toHaveBeenCalledWith("journey-1", "I noticed I grip tightly onto plans."),
    );
    await waitFor(() => expect(screen.queryByLabelText(/your reflection/i)).not.toBeInTheDocument());
    expect(screen.getByRole("button", { name: /continue journey/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /complete journey/i })).toBeInTheDocument();
  });

  it("calls onJourneyCompleted after completing the journey", async () => {
    const apiClient = createApiClientMock();
    const onJourneyCompleted = vi.fn();
    render(<JourneyPage apiClient={apiClient} journeyId="journey-1" onJourneyCompleted={onJourneyCompleted} />);

    fireEvent.click(screen.getByRole("button", { name: /roll/i }));
    await waitFor(() => screen.getByLabelText(/your reflection/i));
    fireEvent.change(screen.getByLabelText(/your reflection/i), { target: { value: "Enough for today." } });
    fireEvent.click(screen.getByRole("button", { name: /save reflection/i }));
    await waitFor(() => screen.getByRole("button", { name: /complete journey/i }));

    fireEvent.click(screen.getByRole("button", { name: /complete journey/i }));

    await waitFor(() => expect(apiClient.completeJourney).toHaveBeenCalledWith("journey-1"));
    await waitFor(() => expect(onJourneyCompleted).toHaveBeenCalled());
  });
});
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
cd frontend
npm test -- JourneyPage
```

Expected: FAIL — `JourneyPage` doesn't accept `onJourneyCompleted` yet, has no reflection form, and `submitReflection`/`completeJourney` don't exist on `apiClient`.

- [ ] **Step 4: Rewrite `JourneyPage.tsx`**

```typescript
import { useState } from "react";
import type { ApiClient, RollResultDto } from "../lib/apiClient";

interface JourneyPageProps {
  apiClient: ApiClient;
  journeyId: string;
  onJourneyCompleted: () => void;
}

export function JourneyPage({ apiClient, journeyId, onJourneyCompleted }: JourneyPageProps) {
  const [card, setCard] = useState<RollResultDto | null>(null);
  const [reflectionText, setReflectionText] = useState("");
  const [submittedReflection, setSubmittedReflection] = useState<string | null>(null);
  const [isRolling, setIsRolling] = useState(false);
  const [isSubmittingReflection, setIsSubmittingReflection] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleRoll() {
    setError(null);
    setIsRolling(true);
    try {
      const result = await apiClient.rollDice(journeyId);
      setCard(result);
      setReflectionText("");
      setSubmittedReflection(null);
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsRolling(false);
    }
  }

  async function handleSubmitReflection(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmittingReflection(true);
    try {
      await apiClient.submitReflection(journeyId, reflectionText);
      setSubmittedReflection(reflectionText);
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsSubmittingReflection(false);
    }
  }

  async function handleComplete() {
    setError(null);
    setIsCompleting(true);
    try {
      await apiClient.completeJourney(journeyId);
      onJourneyCompleted();
    } catch {
      setError("Something went wrong. Please try again.");
    } finally {
      setIsCompleting(false);
    }
  }

  return (
    <div className="stack">
      {!card && (
        <button onClick={handleRoll} disabled={isRolling} className="btn">
          Roll
        </button>
      )}

      {card && (
        <article className="card">
          <h2>{card.cardTitle}</h2>
          <p>{card.cardWisdomText}</p>
          <p className="prompt">{card.cardReflectionPrompt}</p>
        </article>
      )}

      {card && submittedReflection === null && (
        <form className="stack" onSubmit={handleSubmitReflection}>
          <div className="field">
            <label htmlFor="reflection-text">Your reflection</label>
            <textarea
              id="reflection-text"
              value={reflectionText}
              onChange={(event) => setReflectionText(event.target.value)}
              placeholder="What came up for you?"
            />
          </div>
          <button
            type="submit"
            className="btn"
            disabled={isSubmittingReflection || reflectionText.trim().length === 0}
          >
            Save reflection
          </button>
        </form>
      )}

      {submittedReflection !== null && (
        <div className="stack">
          <p>{submittedReflection}</p>
          <button onClick={handleRoll} disabled={isRolling} className="btn">
            Continue journey
          </button>
          <button onClick={handleComplete} disabled={isCompleting} className="btn btn-ghost">
            Complete journey
          </button>
        </div>
      )}

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
npm test -- JourneyPage
```

Expected: PASS (3 tests).

- [ ] **Step 6: Run the full frontend suite and the build**

```bash
npm test
npm run build
```

Expected: all tests pass; build succeeds (note: `App.tsx` will not compile yet, since it doesn't pass `onJourneyCompleted` — that's fixed in Task 15, which must land before this task's changes reach `main`. If executing task-by-task with review checkpoints, it is acceptable for `npm run build` to fail here specifically because of the missing `App.tsx` wiring; confirm the *only* build error is the missing prop on `<JourneyPage>` in `App.tsx`, and record that as this task's known, deliberate handoff to Task 15).

- [ ] **Step 7: Commit**

```bash
cd ..
git add frontend
git commit -m "feat: add reflection form and continue/complete actions to JourneyPage

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 15: Frontend — real routes for intention, active journey, and history

**Files:**
- Modify: `frontend/src/App.tsx`
- Modify: `frontend/src/__tests__/App.test.tsx`
- Modify: `frontend/src/App.css` (small addition: `.nav-links`)

**Interfaces:**
- Consumes: `JourneyPage`'s new `onJourneyCompleted` prop (Task 14); `HistoryPage`/`JourneyDetailPage` (Task 16) — this task creates the routes that render them, so it lands together with or immediately before Task 16 in the same review cycle if built strictly in order; the plan lists it first because `App.tsx`'s shape is the contract Task 16's pages must satisfy.
- Produces: real URLs — `/` (new intention), `/journey/:journeyId` (active journey), `/history` (list), `/history/:journeyId` (detail) — replacing the Phase 1 local `journeyId` state.

- [ ] **Step 1: Write the failing test**

Replace `frontend/src/__tests__/App.test.tsx` with:

```typescript
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import App from "../App";

vi.mock("@clerk/clerk-react", () => ({
  SignedIn: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignedOut: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignOutButton: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  AuthenticateWithRedirectCallback: () => null,
  useAuth: () => ({ getToken: vi.fn() }),
  useSignIn: () => ({
    isLoaded: true,
    signIn: { create: vi.fn(), authenticateWithRedirect: vi.fn() },
    setActive: vi.fn(),
  }),
  useClerk: () => ({ openSignIn: vi.fn(), openSignUp: vi.fn() }),
}));

describe("App", () => {
  it("renders the wordmark, a history link, and both auth controls on the root route", () => {
    render(
      <MemoryRouter initialEntries={["/"]}>
        <App />
      </MemoryRouter>,
    );

    expect(screen.getAllByText("Reflekta").length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: /history/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign out/i })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd frontend
npm test -- App.test
```

Expected: FAIL — there is no `role="link"` named "History" yet (Phase 1's `App.tsx` has no navigation link at all).

- [ ] **Step 3: Rewrite `App.tsx`**

```typescript
import { Link, Navigate, Route, Routes, useNavigate, useParams } from "react-router-dom";
import {
  AuthenticateWithRedirectCallback,
  SignedIn,
  SignedOut,
  SignOutButton,
  useAuth,
} from "@clerk/clerk-react";
import { createApiClient, type ApiClient } from "./lib/apiClient";
import { IntentionPage } from "./pages/IntentionPage";
import { JourneyPage } from "./pages/JourneyPage";
import { SignInPage } from "./pages/SignInPage";
import { HistoryPage } from "./pages/HistoryPage";
import { JourneyDetailPage } from "./pages/JourneyDetailPage";
import "./App.css";

function NewIntentionRoute({ apiClient }: { apiClient: ApiClient }) {
  const navigate = useNavigate();
  return <IntentionPage apiClient={apiClient} onJourneyStarted={(id) => navigate(`/journey/${id}`)} />;
}

function ActiveJourneyRoute({ apiClient }: { apiClient: ApiClient }) {
  const { journeyId } = useParams<{ journeyId: string }>();
  const navigate = useNavigate();
  if (!journeyId) return <Navigate to="/" replace />;
  return (
    <JourneyPage
      apiClient={apiClient}
      journeyId={journeyId}
      onJourneyCompleted={() => navigate(`/history/${journeyId}`)}
    />
  );
}

function HistoryDetailRoute({ apiClient }: { apiClient: ApiClient }) {
  const { journeyId } = useParams<{ journeyId: string }>();
  if (!journeyId) return <Navigate to="/history" replace />;
  return <JourneyDetailPage apiClient={apiClient} journeyId={journeyId} />;
}

function MainApp() {
  const { getToken } = useAuth();
  const apiClient = createApiClient(getToken);

  return (
    <>
      <SignedOut>
        <SignInPage />
      </SignedOut>

      <SignedIn>
        <div className="shell">
          <header className="topbar">
            <span className="wordmark">Reflekta</span>
            <nav className="nav-links">
              <Link to="/history" className="link-btn">
                History
              </Link>
              <SignOutButton>
                <button className="btn btn-ghost btn-sm">Sign out</button>
              </SignOutButton>
            </nav>
          </header>

          <main className="main">
            <Routes>
              <Route path="/" element={<NewIntentionRoute apiClient={apiClient} />} />
              <Route path="/journey/:journeyId" element={<ActiveJourneyRoute apiClient={apiClient} />} />
              <Route path="/history" element={<HistoryPage apiClient={apiClient} />} />
              <Route path="/history/:journeyId" element={<HistoryDetailRoute apiClient={apiClient} />} />
            </Routes>
          </main>
        </div>
      </SignedIn>
    </>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/sso-callback" element={<AuthenticateWithRedirectCallback />} />
      <Route path="*" element={<MainApp />} />
    </Routes>
  );
}
```

Note: this references `HistoryPage` and `JourneyDetailPage`, which do not exist until Task 16. This is a deliberate, explicit forward reference — the same pattern step 6 of Task 14 already established. If executing strictly task-by-task, either land Task 16 in the same review cycle, or add two trivial placeholder files first (`export function HistoryPage() { return null; }` / `export function JourneyDetailPage() { return null; }`) purely so `tsc` resolves the imports, then replace them fully in Task 16.

Add to `App.css`, near `.topbar`:

```css
.nav-links {
  display: flex;
  align-items: center;
  gap: 20px;
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
npm test -- App.test
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
cd ..
git add frontend
git commit -m "feat: add real routes for intention, active journey, and history

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 16: Frontend — history list and journey detail pages

**Files:**
- Create: `frontend/src/pages/HistoryPage.tsx`
- Create: `frontend/src/pages/JourneyDetailPage.tsx`
- Create: `frontend/src/pages/__tests__/HistoryPage.test.tsx`
- Create: `frontend/src/pages/__tests__/JourneyDetailPage.test.tsx`
- Modify: `frontend/src/lib/apiClient.ts` (`listJourneys`, `getJourneyDetail`, and their DTOs)
- Modify: `frontend/src/App.css` (`.history-list`, `.history-item`)

**Interfaces:**
- Consumes: `GET /api/journeys`, `GET /api/journeys/{id}` (Task 13), whose shapes mirror `JourneySummaryDto`/`JourneyDetailDto`/`PlayedCardDetailDto` exactly.
- Produces: `HistoryPage`, `JourneyDetailPage` — the two components `App.tsx` (Task 15) already imports and routes to.

- [ ] **Step 1: Add the DTOs and methods to `apiClient.ts`**

Add to the interfaces section:

```typescript
export interface JourneySummaryDto {
  id: string;
  intentionText: string;
  status: "Active" | "Completed";
  startedAt: string;
  completedAt: string | null;
  cardCount: number;
}

export interface PlayedCardDetailDto {
  id: string;
  sequenceNumber: number;
  diceResult: number;
  cardTitle: string;
  cardWisdomText: string;
  cardReflectionPrompt: string;
  cardThemes: string[];
  reflectionText: string | null;
}

export interface JourneyDetailDto {
  id: string;
  intentionText: string;
  status: "Active" | "Completed";
  startedAt: string;
  completedAt: string | null;
  playedCards: PlayedCardDetailDto[];
}
```

Add two methods next to `completeJourney`:

```typescript
    listJourneys: () => request<JourneySummaryDto[]>(getToken, "/api/journeys"),

    getJourneyDetail: (journeyId: string) =>
      request<JourneyDetailDto>(getToken, `/api/journeys/${journeyId}`),
```

- [ ] **Step 2: Write the failing tests**

`frontend/src/pages/__tests__/HistoryPage.test.tsx`:

```typescript
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import { HistoryPage } from "../HistoryPage";
import type { ApiClient } from "../../lib/apiClient";

describe("HistoryPage", () => {
  it("lists the user's journeys with a link to each one", async () => {
    const apiClient = {
      listJourneys: vi.fn().mockResolvedValue([
        {
          id: "journey-1",
          intentionText: "Should I change my career?",
          status: "Completed",
          startedAt: "2026-01-01T00:00:00Z",
          completedAt: "2026-01-01T01:00:00Z",
          cardCount: 2,
        },
      ]),
    } as unknown as ApiClient;

    render(
      <MemoryRouter>
        <HistoryPage apiClient={apiClient} />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText("Should I change my career?")).toBeInTheDocument());
    expect(screen.getByRole("link", { name: /should i change my career/i })).toHaveAttribute(
      "href",
      "/history/journey-1",
    );
  });

  it("shows a message when there are no journeys yet", async () => {
    const apiClient = { listJourneys: vi.fn().mockResolvedValue([]) } as unknown as ApiClient;

    render(
      <MemoryRouter>
        <HistoryPage apiClient={apiClient} />
      </MemoryRouter>,
    );

    await waitFor(() => expect(screen.getByText(/haven't completed a journey yet/i)).toBeInTheDocument());
  });
});
```

`frontend/src/pages/__tests__/JourneyDetailPage.test.tsx`:

```typescript
import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { JourneyDetailPage } from "../JourneyDetailPage";
import type { ApiClient } from "../../lib/apiClient";

describe("JourneyDetailPage", () => {
  it("shows the intention and each played card with its reflection", async () => {
    const apiClient = {
      getJourneyDetail: vi.fn().mockResolvedValue({
        id: "journey-1",
        intentionText: "Should I change my career?",
        status: "Completed",
        startedAt: "2026-01-01T00:00:00Z",
        completedAt: "2026-01-01T01:00:00Z",
        playedCards: [
          {
            id: "played-1",
            sequenceNumber: 1,
            diceResult: 4,
            cardTitle: "Control",
            cardWisdomText: "Notice where you try to hold on tightly.",
            cardReflectionPrompt: "What are you trying hardest to control today?",
            cardThemes: ["Control"],
            reflectionText: "I noticed I grip tightly onto plans.",
          },
        ],
      }),
    } as unknown as ApiClient;

    render(<JourneyDetailPage apiClient={apiClient} journeyId="journey-1" />);

    await waitFor(() => expect(screen.getByText("Should I change my career?")).toBeInTheDocument());
    expect(screen.getByText("Control")).toBeInTheDocument();
    expect(screen.getByText("I noticed I grip tightly onto plans.")).toBeInTheDocument();
  });
});
```

- [ ] **Step 3: Run the tests to verify they fail**

```bash
cd frontend
npm test -- HistoryPage JourneyDetailPage
```

Expected: FAIL — neither component exists yet.

- [ ] **Step 4: Implement `HistoryPage.tsx`**

```typescript
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { ApiClient, JourneySummaryDto } from "../lib/apiClient";

interface HistoryPageProps {
  apiClient: ApiClient;
}

export function HistoryPage({ apiClient }: HistoryPageProps) {
  const [journeys, setJourneys] = useState<JourneySummaryDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .listJourneys()
      .then(setJourneys)
      .catch(() => setError("Something went wrong. Please try again."));
  }, [apiClient]);

  if (error) {
    return (
      <p className="error" role="alert">
        {error}
      </p>
    );
  }

  if (journeys === null) {
    return <p>Loading…</p>;
  }

  if (journeys.length === 0) {
    return <p>You haven't completed a journey yet.</p>;
  }

  return (
    <ul className="history-list">
      {journeys.map((journey) => (
        <li key={journey.id} className="history-item">
          <Link to={`/history/${journey.id}`}>{journey.intentionText}</Link>
          <span>
            {" "}
            — {journey.status} — {journey.cardCount} card{journey.cardCount === 1 ? "" : "s"}
          </span>
        </li>
      ))}
    </ul>
  );
}
```

- [ ] **Step 5: Implement `JourneyDetailPage.tsx`**

```typescript
import { useEffect, useState } from "react";
import type { ApiClient, JourneyDetailDto } from "../lib/apiClient";

interface JourneyDetailPageProps {
  apiClient: ApiClient;
  journeyId: string;
}

export function JourneyDetailPage({ apiClient, journeyId }: JourneyDetailPageProps) {
  const [journey, setJourney] = useState<JourneyDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient
      .getJourneyDetail(journeyId)
      .then(setJourney)
      .catch(() => setError("Something went wrong. Please try again."));
  }, [apiClient, journeyId]);

  if (error) {
    return (
      <p className="error" role="alert">
        {error}
      </p>
    );
  }

  if (journey === null) {
    return <p>Loading…</p>;
  }

  return (
    <div className="stack">
      <h1>{journey.intentionText}</h1>
      <p>{journey.status}</p>
      {journey.playedCards.map((playedCard) => (
        <article className="card" key={playedCard.id}>
          <h2>{playedCard.cardTitle}</h2>
          <p>{playedCard.cardWisdomText}</p>
          <p className="prompt">{playedCard.cardReflectionPrompt}</p>
          {playedCard.reflectionText && <p>{playedCard.reflectionText}</p>}
        </article>
      ))}
    </div>
  );
}
```

- [ ] **Step 6: Add the small CSS additions**

Add to `App.css`:

```css
.history-list {
  list-style: none;
  margin: 0;
  padding: 0;
  width: 100%;
  max-width: 560px;
  text-align: left;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.history-item {
  padding: 12px 0;
  border-bottom: 1px solid var(--color-border);
}
```

- [ ] **Step 7: Run the tests to verify they pass**

```bash
npm test -- HistoryPage JourneyDetailPage
```

Expected: PASS (3 tests).

- [ ] **Step 8: Run the full frontend suite and the build**

```bash
npm test
npm run build
```

Expected: all pass; build succeeds (this is the point where Task 15's forward reference to these two components is finally satisfied for real).

- [ ] **Step 9: Commit**

```bash
cd ..
git add frontend
git commit -m "feat: add history list and journey detail pages

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 17: End-to-end wiring verification

**Files:** none created — this task exercises Tasks 10-16 together.

**Interfaces:** none new.

- [ ] **Step 1: Boot the full stack**

```bash
docker compose up --build
```

- [ ] **Step 2: Walk through the manual checklist**

Sign in at `http://localhost:5173` and confirm, in order:

1. Create an intention → lands on `/journey/:id` with a "Roll" button.
2. Click "Roll" → a card appears with a reflection form beneath it.
3. Clicking "Continue journey" or "Complete journey" is **not** offered yet — only a reflection form is visible.
4. Write a reflection and click "Save reflection" → the form disappears, replaced by "Continue journey" and "Complete journey" buttons.
5. Click "Continue journey" → a new "Roll" button appears (no card yet); rolling again requires a fresh reflection before those two buttons reappear.
6. Click "Complete journey" (after reflecting) → navigates to `/history/:id`, showing the intention, both played cards, and both reflections.
7. Click "History" in the top bar → the completed journey appears in the list with the right card count and status "Completed".
8. Open a second browser session (or sign in as a different Clerk user) and confirm `GET /api/journeys` for that user does not include the first user's journey (check via the Network tab, or `curl` with each user's token).
9. Attempt `POST /api/journeys/{id}/complete` a second time on the same journey (e.g. via `curl` with a valid token) → confirm `400`.

- [ ] **Step 3: Record the result**

If every checklist item passes, PRODUCT_REQUIREMENTS.md §24 acceptance-criteria steps 8, 10, 11 (write a reflection, continue to another card, complete the journey) and §15 HIS-001/003/004 (view, open, and review reflections in history) are satisfied end-to-end, without any AI involvement, exactly as scoped. Steps 9, 12, and 15-17 of §24 (the AI dialogue and the summary) remain for Phase 2b/2c.

---

## Self-Review Notes — Phase 2a

- **Spec coverage:** REF-001..005 (Task 11, 14), JRN-004 (Task 10-11), JRN-006/007/008 (Task 12-13), SES-001/006 (Task 12), HIS-001/003/004 (Task 13, 16). SES-002..005 (AI-generated summary) and HIS-002/005's "summary" field are explicitly out of scope, per Global Constraints, and belong to Phase 2b/2c.
- **Placeholder scan:** no TBD/TODO markers; every step has runnable code. Two forward references are called out explicitly, not left implicit: Task 14 Step 6 (frontend build fails until Task 15 wires `onJourneyCompleted`) and Task 15 Step 3 (imports `HistoryPage`/`JourneyDetailPage` before Task 16 creates them). Both name the exact reason and the exact task that resolves them.
- **Type consistency:** verified field-by-field between backend records and frontend interfaces — `ReflectionDto`/`ReflectionDto`, `JourneySummaryDto.cardCount: number` ↔ `int CardCount`, `PlayedCardDetailDto.reflectionText: string | null` ↔ `string? ReflectionText`, `JourneyDetailDto.playedCards: PlayedCardDetailDto[]` ↔ `List<PlayedCardDetailDto> PlayedCards`.
- **Task dependency order:** 10 → 11 → 12 → 13 are strictly sequential on the backend (each reuses the previous task's guard logic or DTOs). 14 depends only on 11-12 (not 13). 15 depends on 14 (the new `JourneyPage` prop) and forward-references 16. 16 depends only on 13. If executing task-by-task with review gates, land 15 and 16 in the same review cycle, or land 16 immediately before 15, to avoid an intentionally broken build persisting between reviews.

---

**Phase 2a plan complete and appended to `docs/plan.md`.**
