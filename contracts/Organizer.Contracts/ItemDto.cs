namespace Organizer.Contracts;

/// <summary>An Item as the Core API returns it. State is "open", "done" or "archived".</summary>
public sealed record ItemDto(
    int Id, string Text, string? Notes, string? Category, string[] Tags,
    DateOnly? DueDate, bool Important, bool Urgent, string State, DateTime CreatedAt);
