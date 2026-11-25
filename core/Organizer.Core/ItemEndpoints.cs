using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Organizer.Contracts;

namespace Organizer.Core;

public sealed record CategoryRequest(string? Name);

public sealed record CategoryDto(int Id, string Name);

public sealed record HealthDto(string Status);

public static class ItemMapping
{
    public static ItemDto ToDto(this Item i) => new(
        i.Id, i.Text, i.Notes, i.Category?.Name, i.Tags.Select(t => t.Name).Order(NameComparer.Order).ToArray(),
        i.DueDate, i.Important, i.Urgent, i.State.ToString().ToLowerInvariant(), i.CreatedAt, i.DoneAt, i.ArchivedAt);
}

/// <summary>Maximum lengths (UTF-16 units, as a browser maxlength counts) checked on every write.</summary>
public static class Limits
{
    public const int Text = 500;
    public const int Notes = 10_000;
    public const int Name = 50; // Category and Tag names
}

public static class ItemEndpoints
{
    private static readonly HashSet<string> EditableFields = new(StringComparer.OrdinalIgnoreCase)
        { "text", "notes", "category", "tags", "dueDate", "important", "urgent" };

    private static IResult Bad(string title, string detail) =>
        Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status400BadRequest);

    /// <summary>A 400 naming the field and its limit if the value is longer; otherwise null.</summary>
    private static IResult? TooLong(string field, string? value, int max) =>
        value is not null && value.Length > max
            ? Bad($"{field} is too long.", $"{field} is too long: {max} characters at most.")
            : null;

    private static IResult NotFound(string what, int id) =>
        Results.Problem(
            title: $"{what} not found.", detail: $"There is no {what} with ID {id}.",
            statusCode: StatusCodes.Status404NotFound);

    public static void MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/items", async (CaptureRequest request, OrganizerDb db) =>
        {
            var text = request.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return Bad("Text is required.", "Capture needs a line of text that is not empty or whitespace.");
            }
            if (TooLong("Text", text, Limits.Text) is { } tooLong)
            {
                return tooLong;
            }
            var item = new Item { Text = text, CreatedAt = DateTime.UtcNow };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/api/items/{item.Id}", item.ToDto());
        })
        .WithSummary("Capture an Item from one line of text.")
        .Produces<ItemDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet("/items/{id:int}", async (int id, OrganizerDb db) =>
        {
            var item = await db.Items.Include(i => i.Tags).Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id);
            return item is null ? NotFound("Item", id) : Results.Ok(item.ToDto());
        })
        .WithSummary("Get one Item, in any state.")
        .Produces<ItemDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

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
                    if (TooLong("Text", t, Limits.Text) is { } tooLong)
                    {
                        return tooLong;
                    }
                    item.Text = t;
                }
                if (Has(body, "notes", out var notes))
                {
                    var n = notes?.GetValue<string>();
                    if (TooLong("Notes", n, Limits.Notes) is { } tooLong)
                    {
                        return tooLong;
                    }
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
                        var found = NameComparer.Find(
                            await db.Categories.OrderBy(c => c.Id).ToListAsync(), c => c.Name, name);
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
                        .DistinctBy(n => n, NameComparer.Equality)
                        .ToList();
                    if (names.Select(n => TooLong("Tag name", n, Limits.Name)).FirstOrDefault(r => r is not null) is { } tooLong)
                    {
                        return tooLong;
                    }
                    var existing = await db.Tags.OrderBy(t => t.Id).ToListAsync();
                    item.Tags = names
                        .Select(n => NameComparer.Find(existing, t => t.Name, n) ?? new Tag { Name = n })
                        .ToList();
                }
            }
            catch (Exception e) when (e is InvalidOperationException or FormatException)
            {
                return Bad("Invalid value.",
                    "A field has the wrong type or format. dueDate is yyyy-MM-dd; tags is an array of strings; important and urgent are true/false.");
            }

            await db.SaveChangesAsync();
            return Results.Ok(item.ToDto());
        })
        .WithSummary("Edit an Item.")
        .WithDescription("Send only the fields to change. An omitted field is unchanged; an explicit null clears it " +
                         "(text cannot be cleared). Length limits: text 500, notes 10,000, Tag names 50 characters; only the fields sent are checked. category must name an existing Category; tags replaces the Item's Tags; " +
                         "dueDate is yyyy-MM-dd.")
        .Produces<ItemDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/items/{id:int}/done", (int id, OrganizerDb db) => Transition(id, ItemState.Done, db))
            .WithSummary("Mark an Item done.").WithTransitionDocs();
        app.MapPost("/items/{id:int}/archive", (int id, OrganizerDb db) => Transition(id, ItemState.Archived, db))
            .WithSummary("Archive an Item.").WithTransitionDocs();
        app.MapPost("/items/{id:int}/restore", (int id, OrganizerDb db) => Transition(id, ItemState.Open, db))
            .WithSummary("Move an Item back to open.").WithTransitionDocs();

        app.MapGet("/done", async (OrganizerDb db) =>
        {
            var items = await db.Items.Where(i => i.State == ItemState.Done)
                .Include(i => i.Tags).Include(i => i.Category)
                .OrderByDescending(i => i.DoneAt).ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemMapping.ToDto));
        })
        .WithSummary("Done Items, most recently done first.")
        .Produces<ItemDto[]>();

        app.MapGet("/archive", async (OrganizerDb db) =>
        {
            var items = await db.Items.Where(i => i.State == ItemState.Archived)
                .Include(i => i.Tags).Include(i => i.Category)
                .OrderByDescending(i => i.ArchivedAt).ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemMapping.ToDto));
        })
        .WithSummary("Archived Items, most recently archived first.")
        .Produces<ItemDto[]>();

        // Substring match on text and notes, any state. Both sides are lowercased with the Unicode-aware
        // lower_u (Cyrillic etc.); SQLite's own LIKE only folds ASCII.
        app.MapGet("/search", async (OrganizerDb db, string? q) =>
        {
            var term = q?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                return Bad("Query is required.", "Search needs a query (?q=...) that is not empty or whitespace.");
            }
            var pattern = "%" + term.ToLowerInvariant().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var items = await db.Items
                .Where(i => EF.Functions.Like(OrganizerDb.LowerU(i.Text), pattern, "\\") ||
                            EF.Functions.Like(OrganizerDb.LowerU(i.Notes), pattern, "\\"))
                .Include(i => i.Tags).Include(i => i.Category)
                .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemMapping.ToDto));
        })
        .WithSummary("Find Items by text or notes, in any state.")
        .Produces<ItemDto[]>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost("/categories", async (CategoryRequest request, OrganizerDb db) =>
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Bad("Name is required.", "A Category needs a name that is not empty or whitespace.");
            }
            if (TooLong("Name", name, Limits.Name) is { } tooLong)
            {
                return tooLong;
            }
            if (NameComparer.Find(await db.Categories.ToListAsync(), c => c.Name, name) is not null)
            {
                return Results.Problem(
                    title: "Category exists.", detail: $"A Category named '{name}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            var category = new Category { Name = name };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/categories/{category.Id}", new CategoryDto(category.Id, category.Name));
        })
        .WithSummary("Create a Category.")
        .Produces<CategoryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapPatch("/categories/{id:int}", async (int id, CategoryRequest request, OrganizerDb db) =>
        {
            var category = await db.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound("Category", id);
            }
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return Bad("Name is required.", "A Category needs a name that is not empty or whitespace.");
            }
            if (TooLong("Name", name, Limits.Name) is { } tooLong)
            {
                return tooLong;
            }
            var others = (await db.Categories.Where(c => c.Id != id).ToListAsync());
            if (NameComparer.Find(others, c => c.Name, name) is not null)
            {
                return Results.Problem(
                    title: "Category exists.", detail: $"A Category named '{name}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            category.Name = name;
            await db.SaveChangesAsync();
            return Results.Ok(new CategoryDto(category.Id, category.Name));
        })
        .WithSummary("Rename a Category.")
        .WithDescription("Its Items keep the Category and show the new name. Name: at most 50 characters. " +
                         "Changing only the case of its own name is allowed.")
        .Produces<CategoryDto>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapGet("/tags", async (OrganizerDb db) =>
            Results.Ok((await db.Tags.Where(t => t.Items.Any()).Select(t => t.Name).ToListAsync())
                .Order(NameComparer.Order)))
            .WithSummary("Names of the Tags in use, by name.")
            .WithDescription("A Tag no Item carries (in any state) is left out.")
            .Produces<string[]>();

        app.MapGet("/categories", async (OrganizerDb db) =>
            Results.Ok((await db.Categories.Select(c => new CategoryDto(c.Id, c.Name)).ToListAsync())
                .OrderBy(c => c.Name, NameComparer.Order).ThenBy(c => c.Id)))
            .WithSummary("All Categories, by name.")
            .Produces<CategoryDto[]>();

        app.MapGet("/inbox", async (OrganizerDb db, string? category, string[] tag) =>
        {
            var query = db.Items.Where(i => i.State == ItemState.Open);
            if (!string.IsNullOrWhiteSpace(category))
            {
                var found = NameComparer.Find(await db.Categories.OrderBy(x => x.Id).ToListAsync(), x => x.Name, category.Trim());
                if (found is null)
                {
                    return Results.Ok(Array.Empty<ItemDto>());
                }
                var categoryId = found.Id;
                query = query.Where(i => i.CategoryId == categoryId);
            }
            var tagFilters = tag.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim())
                .Distinct(NameComparer.Equality).ToList();
            if (tagFilters.Count > 0)
            {
                var allTags = await db.Tags.OrderBy(t => t.Id).ToListAsync();
                foreach (var name in tagFilters)
                {
                    var found = NameComparer.Find(allTags, t => t.Name, name);
                    if (found is null)
                    {
                        return Results.Ok(Array.Empty<ItemDto>());
                    }
                    var tagId = found.Id;
                    query = query.Where(i => i.Tags.Any(t => t.Id == tagId));
                }
            }
            var items = await query
                .Include(i => i.Tags)
                .Include(i => i.Category)
                .OrderBy(i => i.DueDate == null)
                .ThenBy(i => i.DueDate)
                .ThenByDescending(i => i.CreatedAt)
                .ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemMapping.ToDto));
        })
        .WithSummary("Open Items, due soonest first.")
        .Produces<ItemDto[]>();
    }

    private static RouteHandlerBuilder WithTransitionDocs(this RouteHandlerBuilder builder) =>
        builder.WithDescription("Moving an Item to the state it is already in changes nothing and returns it.")
            .Produces<ItemDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

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
        return Results.Ok(item.ToDto());
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
