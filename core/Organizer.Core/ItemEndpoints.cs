using Microsoft.EntityFrameworkCore;

namespace Organizer.Core;

public sealed record CaptureRequest(string? Text);

public sealed record ItemDto(
    int Id, string Text, string? Notes, string? Category, string[] Tags,
    DateOnly? DueDate, bool Important, bool Urgent, string State, DateTime CreatedAt)
{
    public static ItemDto From(Item i) => new(
        i.Id, i.Text, i.Notes, i.Category, i.Tags.Select(t => t.Name).Order().ToArray(),
        i.DueDate, i.Important, i.Urgent, i.State.ToString().ToLowerInvariant(), i.CreatedAt);
}

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/items", async (CaptureRequest request, OrganizerDb db) =>
        {
            var text = request.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return Results.Problem(
                    title: "Text is required.",
                    detail: "Capture needs a line of text that is not empty or whitespace.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
            var item = new Item { Text = text, CreatedAt = DateTime.UtcNow };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            return Results.Created($"/items/{item.Id}", ItemDto.From(item));
        });

        app.MapGet("/inbox", async (OrganizerDb db, string? category, string[] tag) =>
        {
            var query = db.Items.Where(i => i.State == ItemState.Open);
            if (!string.IsNullOrWhiteSpace(category))
            {
                var c = category.Trim().ToLower();
                query = query.Where(i => i.Category != null && i.Category.ToLower() == c);
            }
            foreach (var name in tag.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLower()).Distinct())
            {
                var n = name;
                query = query.Where(i => i.Tags.Any(t => t.Name.ToLower() == n));
            }
            var items = await query
                .Include(i => i.Tags)
                .OrderBy(i => i.DueDate == null)
                .ThenBy(i => i.DueDate)
                .ThenByDescending(i => i.CreatedAt)
                .ThenByDescending(i => i.Id)
                .ToListAsync();
            return Results.Ok(items.Select(ItemDto.From));
        });
    }
}
