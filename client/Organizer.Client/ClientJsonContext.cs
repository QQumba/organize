using System.Text.Json;
using System.Text.Json.Serialization;
using Organizer.Contracts;

namespace Organizer.Client;

/// <summary>Source-generated JSON metadata (no reflection, Native AOT safe). Web defaults match the Core.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CaptureRequest))]
[JsonSerializable(typeof(ItemDto))]
[JsonSerializable(typeof(ItemDto[]))]
[JsonSerializable(typeof(ClientConfig))]
[JsonSerializable(typeof(ProblemBody))]
internal sealed partial class ClientJsonContext : JsonSerializerContext;

/// <summary>The fields of the Core's ProblemDetails error body that the client shows.</summary>
internal sealed record ProblemBody(string? Title, string? Detail);
