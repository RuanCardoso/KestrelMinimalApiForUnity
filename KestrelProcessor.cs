using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace KestrelMinimalApiForUnity;

public class KestrelProcessor(string[] args)
{
    private const int kDefaultTimeoutMs = 2500;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<KestrelResponse>> pendingRequests = new();
    public Func<KestrelRoute, HttpRequest, HttpResponse, Task<KestrelResponse>>? OnRequest;
    public void Init(List<KestrelRoute> kRoutes, KestrelOptions kOptions)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel((options) =>
        {
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(kOptions.KeepAliveTimeout);
            options.ListenAnyIP(kOptions.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                if (kOptions.UseHttps && !string.IsNullOrEmpty(kOptions.CertificateFile))
                {
                    if (File.Exists(kOptions.CertificateFile))
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(kOptions.CertificateFile))!;
                        string certName = dict["cert"];
                        string certPassword = dict["password"];
                        listenOptions.UseHttps(certName, certPassword);
                        Console.WriteLine($"Kestrel using HTTPS on port {kOptions.Port} with certificate {certName}");
                    }
                    else Console.WriteLine($"Certificate file not found: {kOptions.CertificateFile}");
                }
            });

            options.Limits.MaxConcurrentConnections = kOptions.MaxConnections;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(kOptions.RequestTimeout);
            Console.WriteLine($"Kestrel listening on port {kOptions.Port} with max connections {kOptions.MaxConnections} and request timeout {kOptions.RequestTimeout}s");
        });

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (kOptions.Domain == "*")
            {
                await next();
                return;
            }

            var host = context.Request.Host.Host;
            if (!host.Equals($"{kOptions.Domain}", StringComparison.OrdinalIgnoreCase) && !host.EndsWith($".{kOptions.Domain}", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Firewall: Prohibited");
                return;
            }

            await next();
        });

        foreach (var route in kRoutes)
        {
            app.MapMethods(route.Route!, [route.Method!], async (HttpRequest request, HttpResponse response) =>
            {
                try
                {
                    var kResponse = await OnRequest!.Invoke(route, request, response);
                    response.ContentLength = kResponse.ContentLength64;
                    response.ContentType = kResponse.ContentType;
                    response.StatusCode = kResponse.StatusCode;
                    response.Headers.Connection = kResponse.KeepAlive ? "keep-alive" : "close";
                    await response.BodyWriter.WriteAsync(kResponse.Data);
                }
                catch (Exception ex)
                {
                    response.StatusCode = StatusCodes.Status408RequestTimeout;
                    await response.WriteAsync($"Timeout: {ex.Message}");
                }
            });
        }

        app.RunAsync();
    }

    public Task<KestrelResponse> AddPendingRequest(string uniqueId)
    {
        var tcs = new TaskCompletionSource<KestrelResponse>();
        pendingRequests[uniqueId] = tcs;

        var effectiveTimeout = TimeSpan.FromMilliseconds(kDefaultTimeoutMs);
        _ = Task.Delay(effectiveTimeout).ContinueWith(task =>
        {
            if (tcs.TrySetException(new TimeoutException(
                $"Request {uniqueId} timed out after {effectiveTimeout.TotalSeconds} seconds.")))
            {
                pendingRequests.TryRemove(uniqueId, out _);
            }
        });

        return tcs.Task;
    }

    public void CompletePendingRequest(KestrelResponse response)
    {
        if (pendingRequests.TryRemove(response.UniqueId!, out var tcs))
        {
            tcs.SetResult(response);
        }
    }
}