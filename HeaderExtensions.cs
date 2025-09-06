using Microsoft.Extensions.Primitives;

namespace KestrelMinimalApiForUnity;

public static class HeaderExtensions
{

    /// <summary>
    /// Serializa um IHeaderDictionary em uma lista de SerializableHeader
    /// </summary>
    public static List<SerializableHeader> ToSerializable(this IHeaderDictionary headers)
    {
        var list = new List<SerializableHeader>();
        foreach (var kvp in headers)
        {
            list.Add(new SerializableHeader(kvp.Key, kvp.Value));
        }
        return list;
    }

    public static void ApplyTo(this IEnumerable<SerializableHeader> headers, IHeaderDictionary responseHeaders)
    {
        foreach (var header in headers)
        {
            if (header.Values == null || header.Values.Count == 0)
                continue;

            if (header.Values.Count == 1)
                responseHeaders[header.Name] = header.Values[0];
            else
                responseHeaders[header.Name] = new StringValues([.. header.Values]);
        }
    }
}