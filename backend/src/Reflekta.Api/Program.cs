using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Data;
using Reflekta.Api.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<ReflektaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ConversationTurnService>();
builder.Services.AddScoped<SessionSummaryService>();



builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Clerk:Authority"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new()
        {
            ValidateAudience = false,
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();

    
builder.Services.AddSingleton<IDiceService, DiceService>();
builder.Services.AddSingleton<ICardSelectionService, CardSelectionService>();
builder.Services.Configure<AiServiceOptions>(builder.Configuration.GetSection("AiService"));
builder.Services.AddHttpClient<IAiClient, HttpAiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});



builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        { if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins("http://localhost:5173");
        
        policy.AllowAnyHeader()
              .AllowAnyMethod();
        }
              );
});




var app = builder.Build();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReflektaDbContext>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }

    await CardSeeder.SeedAsync(dbContext);
}



app.Run();



// Required so WebApplicationFactory<Program> (used by integration tests in Task 5+)
// can see this type — top-level statement programs generate an internal Program class.
public partial class Program { }
