using Microsoft.EntityFrameworkCore;

namespace Organizer.Core;

/// <summary>The one SQLite database.</summary>
public sealed class OrganizerDb(DbContextOptions<OrganizerDb> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Item>(e =>
        {
            e.Property(i => i.Text).IsRequired();
            e.Property(i => i.State).HasConversion<string>();
            e.HasMany(i => i.Tags).WithMany(t => t.Items);
            e.HasIndex(i => i.State);
            e.HasOne(i => i.Category).WithMany().HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Category>(e =>
        {
            e.Property(c => c.Name).IsRequired().UseCollation("NOCASE");
            e.HasIndex(c => c.Name).IsUnique();
        });
        b.Entity<Tag>(e =>
        {
            e.Property(t => t.Name).IsRequired().UseCollation("NOCASE");
            e.HasIndex(t => t.Name).IsUnique();
        });
    }
}
