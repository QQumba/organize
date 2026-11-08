using Microsoft.EntityFrameworkCore;

namespace Organizer.Core;

/// <summary>The one SQLite database.</summary>
public sealed class OrganizerDb(DbContextOptions<OrganizerDb> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Item>(e =>
        {
            e.Property(i => i.Text).IsRequired();
            e.Property(i => i.State).HasConversion<string>();
            e.HasMany(i => i.Tags).WithMany(t => t.Items);
            e.HasIndex(i => i.State);
        });
        b.Entity<Tag>(e =>
        {
            e.Property(t => t.Name).IsRequired().UseCollation("NOCASE");
            e.HasIndex(t => t.Name).IsUnique();
        });
    }
}
