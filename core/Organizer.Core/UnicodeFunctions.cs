using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Organizer.Core;

/// <summary>Registers Unicode-aware SQLite functions on every connection EF Core opens.</summary>
public sealed class UnicodeFunctions : DbConnectionInterceptor
{
    public const string LowerName = "lower_u";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
        Register(connection);

    public override Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
        {
            sqlite.CreateFunction<string?, string?>(LowerName, s => s?.ToLowerInvariant(), isDeterministic: true);
        }
    }
}
