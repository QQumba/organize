using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace Organizer.Core;

public sealed record CaptureRequest(string? Text);

public sealed record CategoryRequest(string? Name);

public sealed record CategoryDto(int Id, string Name);

public sealed record ItemDto(
    int Id, string Text, string? Notes, string? Category, string[] Tags,
    DateOnly? DueDate, bool Important, bool Urgent, string State, DateTime CreatedAt)
{
    public static ItemDto From(Item i) => new(
        i.Id, i.Text, i.Notes, i.Category?.Name, i.Tags.Select(t => t.Name).Order().ToArray(),
        i.DueDate, i.Important, i.Urgent, i.State.ToString().ToLowerInvariant(), i.CreatedAt);
}

public static class ItemEndpoints
{
    private static readonly HashSet<string> EditableFields = new(StringComparer.OrdinalIgnoreCase)
        { "text", "notes", "category", "tags", "dueDate", "important", "urgent" };

    private static IResult Bad(string title, string detail) =>
        Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status400BadRequest);

    public static void MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/items", async (CaptureRequest request, OrganizerDb db) =>
        {
            var text = request.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return Bad("Text is required.", "Capture needs a line of text that is not empty or whitespace.");
            }
            var item = new Item { Text = text, CreatedAt = DateTime.UtcNow };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/items/{item.Id}", ItemDto.From(item));
        });

        app.MapPatch("/items/{id:int}", async (int id, JsonObject body, OrganizerDb db) =>
        {
            var item = await db.Items.Include(i => i.Tags).Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            if (item is null)
            {
                return Results.Problem(
                    title: "Item not found.", detail: $"There is no Item with ID {id}.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var unknown = body.Select(p => p.Key).FirstOrDefault(k => !EditableFields.Contains(k));
            if (unknown is not null)
            {
                return Bad("Unknown field.",
                    $"'{unknown}' cannot be edited. Editable fields: text, notes, category, tags, dueDate, important, urgent.");
            }

            // An omitted property is unchanged; an explicit null clears it.
            try
            {
                if (Has(body, "text", out var text))
                {
                    var t = text?.GetValue<string>().Trim();
                    if (string.IsNullOrEmpty(t))
                    {
                        return Bad("Text is required.", "Text cannot be cleared or blank.");
                    }
                    item.Text = t;
                }
                if (Has(body, "notes", out var notes))
                {
                    var n = notes?.GetValue<string>();
                    item.Notes = string.IsNullOrWhiteSpace(n) ? null : n;
                }
                if (Has(body, "dueDate", out var due))
                {
                    item.DueDate = due is null
                        ? null
                        : DateOnly.ParseExact(due.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                if (Has(body, "important", out var important))
                {
                    item.Important = important is not null && important.GetValue<bool>();
                }
                if (Has(body, "urgent", out var urgent))
                {
                    item.Urgent = urgent is not null && urgent.GetValue<bool>();
                }
                if (Has(body, "category", out var category))
                {
                    var name = category?.GetValue<string>().Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        item.Category = null;
                    }
                    else
                    {
                        var found = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
                        if (found is null)
                        {
                            return Bad("Unknown Category.", $"There is no Category '{name}'. Create it first with POST /categories.");
                        }
                        item.Category = found;
                    }
                }
                if (Has(body, "tags", out var tags))
                {
                    var names = (tags is null ? [] : tags.AsArray().Select(t => t!.GetValue<string>().Trim()))
                        .Where(n => n.Length > 0)
                        .DistinctBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var existing = await db.Tags.Where(t => names.Contains(t.Name)).ToListAsync();
                    item.Tags = names
                        .Select(n => existing.FirstOrDefault(t => string.Equals(t.Name, n, StringComparison.OrdinalIgnoreCase))
                                     ?? new Tag { Name = n })
                        .ToList();
                }
            }
            catch (Exception e) when (e is InvalidOperationException or FormatException)
            {
                return Bad("Invalid value.",
                    "A field has the wrong type or format. dueDate is yyyy-MM-dd; tags is an array of strings; important and urgent are true/false.");
            }

            await db.SaveChangesAsync();
            return Results.Ok(ItemDto.From(item));
        });

        app.MapPost("/items/{id:int}/done", (int id, OrganizerDb db) => Transition(id, ItemState.Done, db));
        app.MapPost("/items/{id:int}/archive", (int id, OrganizerDb db) => Transition(id, ItemState.Archived, db));
        app.MapPost("/items/{id:int}/restore", (int id, OrganizerDb db) => Transition(id, ItemState.Open, db));

        app.MapGet("/done", async (OrganizerDb db) =>
        {
            var items = await db.Items.Where(i => i.State == ItemState.Done)
                .Include(i => i.Tags).Include(i => i.Category)
                .OrderByDescending(i => i.DoneAt).ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemDto.From));
        });

        app.MapGet("/archive", async (OrganizerDb db) =>
        {
            var items = await db.Items.Where(i => i.State == ItemState.Archived)
                .Include(i => i.Tags).Include(i => i.Category)
                .OrderByDescending(i => i.ArchivedAt).ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemDto.From));
        });

        // Substring match on text and notes, any state. SQLite LIKE is case-insensitive for ASCII only.
        app.MapGet("/search", async (OrganizerDb db, string? q) =>
        {
            var term = q?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                return Bad("Query is required.", "Search needs a query (?q=...) that is not empty or whitespace.");
            }
            var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var items = await db.Items
                .Where(i => EF.Functions.Like(i.Text, pattern, "\\") ||
                            (i.Notes != null && EF.Functions.Like(i.Notes, pattern, "\\")))
                .Include(i => i.Tags).Include(i => i.Category)
                .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemDto.From));
        });

        app.MapPost("/categories", async (CategoryRequest request, OrganizerDb db) =>
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Bad("Name is required.", "A Category needs a name that is not empty or whitespace.");
            }
            if (await db.Categories.AnyAsync(c => c.Name == name))
            {
                return Results.Problem(
                    title: "Category exists.", detail: $"A Category named '{name}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            var category = new Category { Name = name };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/categories/{category.Id}", new CategoryDto(category.Id, category.Name));
        });

        app.MapGet("/categories", async (OrganizerDb db) =>
            Results.Ok(await db.Categories.OrderBy(c => c.Name).Select(c => new CategoryDto(c.Id, c.Name)).ToListAsync()));

        app.MapGet("/inbox", async (OrganizerDb db, string? category, string[] tag) =>
        {
            var query = db.Items.Where(i => i.State == ItemState.Open);
            if (!string.IsNullOrWhiteSpace(category))
            {
                var c = category.Trim();
                query = query.Where(i => i.Category != null && i.Category.Name == c);
            }
            foreach (var name in tag.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var n = name;
                query = query.Where(i => i.Tags.Any(t => t.Name == n));
            }
            var items = await query
                .Include(i => i.Tags)
                .Include(i => i.Category)
                .OrderBy(i => i.DueDate == null)
                .ThenBy(i => i.DueDate)
                .ThenByDescending(i => i.CreatedAt)
                .ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemDto.From));
        });
    }

    /// <summary>Moves an Item to a state. Moving to the current state is a no-op (200, unchanged).</summary>
    private static async Task<IResult> Transition(int id, ItemState target, OrganizerDb db)
    {
        var item = await db.Items.Include(i => i.Tags).Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return Results.Problem(
                title: "Item not found.", detail: $"There is no Item with ID {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }
        if (item.State != target)
        {
            var now = DateTime.UtcNow;
            item.State = target;
            item.DoneAt = target == ItemState.Done ? now : null;
            item.ArchivedAt = target == ItemState.Archived ? now : null;
            await db.SaveChangesAsync();
        }
        return Results.Ok(ItemDto.From(item));
    }

    /// <summary>True if the property is present; the value is null for an explicit null.</summary>
    private static bool Has(JsonObject body, string name, out JsonNode? value)
    {
        foreach (var p in body)
        {
            if (string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }
        value = null;
        return false;
    }
}
