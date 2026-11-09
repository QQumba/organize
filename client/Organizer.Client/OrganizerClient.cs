using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Organizer.Contracts;

namespace Organizer.Client;

/// <summary>Typed wrapper over the Core API. No business logic, storage, retries or caching.</summary>
public sealed class OrganizerClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    /// <summary>Creates a client for a Core at <paramref name="config"/>.Address.</summary>
    public OrganizerClient(ClientConfig config)
    {
        _http = new HttpClient { BaseAddress = new Uri(config.Address!), Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Secret);
        _ownsHttp = true;
    }

    /// <summary>Uses an existing HttpClient that already has BaseAddress and authorization set (tests).</summary>
    public OrganizerClient(HttpClient http) => _http = http;

    /// <summary>Capture: creates an open Item from one line of text.</summary>
    public Task<ItemDto> CaptureAsync(string text, CancellationToken ct = default) =>
        SendAsync(() => _http.PostAsJsonAsync("/items", new CaptureRequest(text), ClientJsonContext.Default.CaptureRequest, ct),
            ClientJsonContext.Default.ItemDto, ct);

    /// <summary>The Inbox (open Items), optionally filtered by Category and Tags.</summary>
    public async Task<IReadOnlyList<ItemDto>> InboxAsync(
        string? category = null, IEnumerable<string>? tags = null, CancellationToken ct = default)
    {
        var url = new StringBuilder("/inbox");
        var sep = '?';
        if (!string.IsNullOrWhiteSpace(category))
        {
            url.Append(sep).Append("category=").Append(Uri.EscapeDataString(category));
            sep = '&';
        }
        foreach (var tag in tags ?? [])
        {
            url.Append(sep).Append("tag=").Append(Uri.EscapeDataString(tag));
            sep = '&';
        }
        var target = url.ToString();
        return await SendAsync(() => _http.GetAsync(target, ct), ClientJsonContext.Default.ItemDtoArray, ct);
    }

    /// <summary>Marks an Item done. Already done is not an error.</summary>
    public Task<ItemDto> DoneAsync(int id, CancellationToken ct = default) =>
        SendAsync(() => _http.PostAsync($"/items/{id}/done", content: null, ct), ClientJsonContext.Default.ItemDto, ct);

    private async Task<T> SendAsync<T>(
        Func<Task<HttpResponseMessage>> send, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            if (!response.IsSuccessStatusCode)
            {
                throw await ErrorFor(response, ct);
            }
            return await response.Content.ReadFromJsonAsync(info, ct)
                   ?? throw new OrganizerException("The Core returned an empty response.");
        }
        catch (HttpRequestException e)
        {
            throw new OrganizerException(
                $"Cannot reach the Core at {_http.BaseAddress}. Is it running? ({e.InnerException?.Message ?? e.Message})", e);
        }
        catch (TaskCanceledException e) when (!ct.IsCancellationRequested)
        {
            throw new OrganizerException($"The Core at {_http.BaseAddress} did not answer in time.", e);
        }
        catch (JsonException e)
        {
            throw new OrganizerException("The Core returned a response this client does not understand. Are the Core and the client the same version?", e);
        }
    }

    private static async Task<OrganizerException> ErrorFor(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new OrganizerException("The Core rejected the secret. Check \"Secret\" in the config file against the Core's core.json.");
        }
        ProblemBody? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync(ClientJsonContext.Default.ProblemBody, ct);
        }
        catch (JsonException) { }
        return new OrganizerException(problem?.Detail ?? problem?.Title
            ?? $"The Core returned {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }
}
