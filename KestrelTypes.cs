using System.Net;
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
public partial class SerializableCookie
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public DateTime Expires { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
    public int Version { get; set; }
    public string? Comment { get; set; }
    public Uri? CommentUri { get; set; }
    public bool Discard { get; set; }

    [MemoryPackConstructor]
    public SerializableCookie() { }

    public SerializableCookie(Cookie cookie)
    {
        Name = cookie.Name;
        Value = cookie.Value;
        Domain = cookie.Domain;
        Path = cookie.Path;
        Expires = cookie.Expires;
        Secure = cookie.Secure;
        HttpOnly = cookie.HttpOnly;
        Version = cookie.Version;
        Comment = cookie.Comment;
        CommentUri = cookie.CommentUri;
        Discard = cookie.Discard;
    }

    public SerializableCookie(string key, string value)
    {
        Name = key;
        Value = value;
        Path = "/";
        Domain = string.Empty;
        Expires = DateTime.MinValue;
        Secure = false;
        HttpOnly = false;
        Version = 0;
    }

    public Cookie ToCookie()
    {
        var c = new Cookie(Name, Value, Path, Domain)
        {
            Expires = Expires,
            Secure = Secure,
            HttpOnly = HttpOnly,
            Version = Version,
            Comment = Comment,
            CommentUri = CommentUri,
            Discard = Discard
        };
        return c;
    }
}

[MemoryPackable]
public partial class SerializableHeader
{
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = [];

    [MemoryPackConstructor]
    public SerializableHeader() { }

    public SerializableHeader(string name, IEnumerable<string> values)
    {
        Name = name;
        Values = [.. values];
    }
}

[MemoryPackable]
public partial class KestrelRoute
{
    public string? Route { get; set; }
    public string? Method { get; set; }
}

[MemoryPackable]
public partial class KestrelOptions
{
    public bool UseHttps { get; set; } = false;
    public string? CertificateFile { get; set; }
    public string Domain { get; set; } = "*";
    public int Port { get; set; } = 80;
    public int KeepAliveTimeout { get; set; } = 130;
    public int MaxConnections { get; set; } = 2000;
    public int RequestTimeout { get; set; } = 30;
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
    public required List<SerializableCookie> Cookies { get; set; }
    public required List<SerializableHeader> Headers { get; set; }
    public required byte[]? InputStream { get; set; }
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
    public List<SerializableCookie>? Cookies { get; set; }
    public List<SerializableHeader>? Headers { get; set; }
}

#endregion