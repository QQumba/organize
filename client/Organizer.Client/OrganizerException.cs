namespace Organizer.Client;

/// <summary>The one error type of the client library. The message is ready to show to the user.</summary>
public sealed class OrganizerException(string message, Exception? inner = null) : Exception(message, inner);
