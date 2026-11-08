using Microsoft.EntityFrameworkCore;

namespace Organizer.Core;

/// <summary>The one SQLite database. Entities arrive in later tickets.</summary>
public sealed class OrganizerDb(DbContextOptions<OrganizerDb> options) : DbContext(options);
