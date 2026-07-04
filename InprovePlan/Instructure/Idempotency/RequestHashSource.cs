namespace Instructure.Idempotency;

/// <summary>
/// 
/// </summary>
public sealed class RequestHashSource
{
    public required string Method { get; init; }

    public required string Path { get; init; }

    public required string QueryString { get; init; }

    public required long UserId { get; init; }

    public required string Body { get; init; }
}
