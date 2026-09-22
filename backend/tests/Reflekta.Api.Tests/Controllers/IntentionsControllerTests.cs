using System.Net;
using System.Net.Http.Json;
using Reflekta.Api.Controllers;
using Reflekta.Api.Tests.TestInfrastructure;
using Xunit;


namespace Reflekta.Api.Tests.Controllers;

public class IntentionsControllerTests : IDisposable
{
    private readonly ReflektaWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public IntentionsControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_WithValidText_ReturnsIntention()
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "user-1");

        var response = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("Should I change my career?"));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<IntentionDto>();
        Assert.Equal("Should I change my career?", dto!.OriginalText);
        Assert.Null(dto.ClarifiedText);
    }

    [Fact]
    public async Task Create_WithBlankText_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "user-1");

        var response = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("test"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherUsersIntention_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "user-1");
        var createResponse = await _client.PostAsJsonAsync("/api/intentions", new CreateIntentionRequest("test"));
        var created = await createResponse.Content.ReadFromJsonAsync<IntentionDto>();

        _client.DefaultRequestHeaders.Remove("X-Test-User");
        _client.DefaultRequestHeaders.Add("X-Test-User", "user-2");

        var response = await _client.GetAsync($"/api/intentions/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}