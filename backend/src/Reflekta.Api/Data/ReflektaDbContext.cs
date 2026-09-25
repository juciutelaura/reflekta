using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reflekta.Api.Models;

namespace Reflekta.Api.Data;

public class ReflektaDbContext : DbContext
{
    public ReflektaDbContext(DbContextOptions<ReflektaDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Intention> Intentions => Set<Intention>();
    public DbSet<Journey> Journeys => Set<Journey>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<PlayedCard> PlayedCards => Set<PlayedCard>();
    public DbSet<Reflection> Reflections => Set<Reflection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.ExternalAuthId).IsUnique();
        });

        modelBuilder.Entity<Journey>(e =>
        {
            e.Property(j => j.Status).HasConversion<string>();
            e.HasMany(j => j.PlayedCards)
                .WithOne()
                .HasForeignKey(pc => pc.JourneyId);
            e.HasOne(j => j.Intention)
                .WithMany()
                .HasForeignKey(j => j.IntentionId);
        });

        modelBuilder.Entity<Card>(e =>
        {
            e.Property(c => c.Themes).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        });

        modelBuilder.Entity<PlayedCard>(e =>
        {
            e.HasOne(pc => pc.Card)
                .WithMany()
                .HasForeignKey(pc => pc.CardId);
        });

        modelBuilder.Entity<Reflection>(e =>
        {
            e.HasIndex(r => r.PlayedCardId).IsUnique();
        });


    }
}

