namespace KestrelMinimalApiForUnity;

public static class CookieExtensions
{
    public static List<SerializableCookie> ToSerializable(this IRequestCookieCollection cookies)
    {
        if (cookies.Count <= 0)
            return [];

        var list = new List<SerializableCookie>();
        foreach (var kvp in cookies)
            list.Add(new SerializableCookie(kvp.Key, kvp.Value));

        return list;
    }

    public static void ApplyTo(this IEnumerable<SerializableCookie> cookies, IResponseCookies response)
    {
        foreach (var sc in cookies)
        {
            var options = new CookieOptions
            {
                Path = sc.Path,
                Domain = string.IsNullOrWhiteSpace(sc.Domain) ? null : sc.Domain,
                Expires = sc.Expires != DateTime.MinValue ? sc.Expires : null,
                Secure = sc.Secure,
                HttpOnly = sc.HttpOnly,
                IsEssential = true // opcional, garante que será escrito mesmo com consentimento de cookies
            };

            response.Append(sc.Name, sc.Value, options);
        }
    }
}