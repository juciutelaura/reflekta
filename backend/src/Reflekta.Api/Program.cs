var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Required so WebApplicationFactory<Program> (used by integration tests in Task 5+)
// can see this type — top-level statement programs generate an internal Program class.
public partial class Program { }
