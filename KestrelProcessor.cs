using System.Collections.Concurrent;
using Microsoft.AspNetCore.Server.Kestrel.Core;

public class KestrelProcessor(string[] args)
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<KestrelResponse>> pendingRequests = new();
    public Func<KestrelRoute, HttpRequest, HttpResponse, Task<KestrelResponse>>? OnRequest;
    public void Init(List<KestrelRoute> routes, KestrelOptions options)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel((opt) =>
        {
            opt.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(options.KeepAliveTimeout);
            opt.ListenAnyIP(options.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });

        var app = builder.Build();
        foreach (var route in routes)
        {
            app.MapMethods(route.Route, [route.Method], async (HttpRequest request, HttpResponse response) =>
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

        var effectiveTimeout = TimeSpan.FromMilliseconds(5000);
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