using Organizer.Client;
using Organizer.Contracts;

namespace Organizer.Desktop;

/// <summary>What the popup needs from the Core.</summary>
public interface IInboxApi
{
    Task<ItemDto> CaptureAsync(string text, CancellationToken ct);
    Task<IReadOnlyList<ItemDto>> InboxAsync(CancellationToken ct);
    Task DoneAsync(int id, CancellationToken ct);
}

public sealed class ClientInboxApi(OrganizerClient client) : IInboxApi
{
    public Task<ItemDto> CaptureAsync(string text, CancellationToken ct) => client.CaptureAsync(text, ct);
    public Task<IReadOnlyList<ItemDto>> InboxAsync(CancellationToken ct) => client.InboxAsync(ct: ct);
    public Task DoneAsync(int id, CancellationToken ct) => client.DoneAsync(id, ct);
}

/// <summary>One line of the Inbox list: text, then the due date if set.</summary>
public sealed record ItemRow(int Id, string Text, DateOnly? DueDate)
{
    public string Line => DueDate is { } d ? $"{Text}  -  {d:yyyy-MM-dd}" : Text;
}

/// <summary>The non-UI state of the Capture popup. The window is a thin view over it. Holds no rules: the Core decides.</summary>
public sealed class CapturePopupModel(IInboxApi? api, string? configError)
{
    private bool _capturing;

    /// <summary>The typed text. Kept after a failed Capture and after dismiss; cleared by a successful Capture.</summary>
    public string Text { get; set; } = "";

    public string? Error { get; private set; }

    public IReadOnlyList<ItemRow> Rows { get; private set; } = [];

    public event Action? Changed;

    /// <summary>Loads the Inbox. With a config error, shows it instead of the list.</summary>
    public async Task OpenAsync(CancellationToken ct)
    {
        Error = null;
        Rows = [];
        if (api is null)
        {
            Error = configError;
            Changed?.Invoke();
            return;
        }
        Changed?.Invoke();
        try
        {
            var items = await api.InboxAsync(ct);
            if (ct.IsCancellationRequested) return;
            Rows = items.Select(i => new ItemRow(i.Id, i.Text, i.DueDate)).ToArray();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            if (ct.IsCancellationRequested) return;
            Error = e.Message;
        }
        Changed?.Invoke();
    }

    /// <summary>Captures the trimmed text. True on success (the view then closes). Empty text does nothing.</summary>
    public async Task<bool> CaptureAsync(CancellationToken ct)
    {
        var text = Text.Trim();
        if (text.Length == 0 || _capturing)
        {
            return false;
        }
        if (api is null)
        {
            Error = configError;
            Changed?.Invoke();
            return false;
        }
        _capturing = true;
        try
        {
            await api.CaptureAsync(text, ct);
            Text = "";
            Error = null;
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Error = e.Message;
            return false;
        }
        finally
        {
            _capturing = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Marks an Item done; on success it leaves the list.</summary>
    public async Task CompleteAsync(int id, CancellationToken ct)
    {
        if (api is null) return;
        try
        {
            await api.DoneAsync(id, ct);
            Rows = Rows.Where(r => r.Id != id).ToArray();
            Error = null;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Error = e.Message;
        }
        Changed?.Invoke();
    }

    /// <summary>The popup was dismissed: drop the list and the error to free memory. The typed text stays.</summary>
    public void Dismissed()
    {
        Rows = [];
        Error = null;
    }
}
