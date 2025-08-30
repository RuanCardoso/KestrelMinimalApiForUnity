using MemoryPack;

namespace KestrelMinimalApiForUnity;

#region All

public enum KestrelMessageType : byte
{
    Initialize,
    AddRoutes,
    DispatchRequest,
    DispatchResponse,
}

public static class Constants
{
    public const int headerSize = 5;
}

public class KestrelChannelMessage
{
    public KestrelMessageType MessageType;
    public byte[]? Payload;
}

#endregion

#region MemoryPackable Classes

[MemoryPackable]
public partial class KestrelRoute
{
    public string? Route { get; set; }
    public string? Method { get; set; }
}

[MemoryPackable]
public partial class KestrelOptions
{
    public int KeepAliveTimeout { get; set; } = 130;
    public int Port { get; set; } = 80;
}

[MemoryPackable]
public partial class KestrelRequest
{
    public required string? UniqueId { get; set; }
    public required KestrelRoute? Route { get; set; }
    public required string? RawUrl { get; set; }
    public required string? HttpMethod { get; set; }
    public required string? ContentType { get; set; }
    public required bool IsSecureConnection { get; set; }
    public required string? QueryString { get; set; }
    public required string? RemoteEndPoint { get; set; }
}

[MemoryPackable]
public partial class KestrelResponse
{
    public string? UniqueId { get; set; }
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public bool KeepAlive { get; set; }
    public long ContentLength64 { get; set; }
    public byte[]? Data { get; set; }
}

#endregion