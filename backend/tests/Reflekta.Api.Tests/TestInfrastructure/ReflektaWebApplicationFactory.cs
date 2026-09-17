using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reflekta.Api.Data;

namespace Reflekta.Api.Tests.TestInfrastructure;

public class ReflektaWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Program.cs registers the Npgsql provider via AddDbContext, which adds a
            // DbContextOptions<ReflektaDbContext> AND an IDbContextOptionsConfiguration<ReflektaDbContext>
            // delegate. Removing only DbContextOptions<ReflektaDbContext> leaves that delegate behind,
            // so both Npgsql and InMemory end up configured on the same options instance. Remove all
            // three registrations before adding the InMemory provider.
            services.RemoveAll<DbContextOptions<ReflektaDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<ReflektaDbContext>>();

            services.AddDbContext<ReflektaDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}