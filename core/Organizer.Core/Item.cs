namespace Organizer.Core;

public enum ItemState { Open, Done, Archived }

public sealed class Item
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string? Notes { get; set; }
    public string? Category { get; set; }
    public List<Tag> Tags { get; set; } = [];
    public DateOnly? DueDate { get; set; }
    public bool Important { get; set; }
    public bool Urgent { get; set; }
    public ItemState State { get; set; } = ItemState.Open;
    public DateTime CreatedAt { get; set; }
}

public sealed class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Item> Items { get; set; } = [];
}
