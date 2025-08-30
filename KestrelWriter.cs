using System.Collections.Concurrent;
using System.Net;
using MemoryPack;

namespace KestrelMinimalApiForUnity;

public class KestrelWriter(Stream netStream, KestrelProcessor kProcessor)
{
    private readonly BlockingCollection<KestrelChannelMessage> channel = [];
    public void Run()
    {
        Initialize();
        while (true)
        {
            var msg = channel.Take();
            InternalSend(msg.MessageType, msg.Payload);
        }
    }

    private void Initialize()
    {
        kProcessor.OnRequest = (route, request, response) =>
        {
            IPAddress ipAddress = request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;
            int port = request.HttpContext.Connection.RemotePort;
            KestrelRequest routeRequest = new()
            {
                Route = route,
                UniqueId = $"{route.Route}_{Guid.NewGuid()}",
                RawUrl = request.Path + request.QueryString,
                ContentType = request.ContentType ?? "",
                HttpMethod = request.Method,
                IsSecureConnection = request.IsHttps,
                QueryString = request.QueryString.ToString(),
                RemoteEndPoint = new IPEndPoint(ipAddress, port).ToString()
            };

            KestrelChannelMessage channelMessage = new()
            {
                MessageType = KestrelMessageType.DispatchRequest,
                Payload = MemoryPackSerializer.Serialize(routeRequest)
            };

            var pendingTask = kProcessor.AddPendingRequest(routeRequest.UniqueId);
            channel.Add(channelMessage);
            return pendingTask;
        };
    }

    private void InternalSend(KestrelMessageType kestrelMessage, ReadOnlySpan<byte> payload)
    {
        // write the header and payload to the pipe
        // header is 4 bytes for length and 1 byte for message type
        Span<byte> header = stackalloc byte[Constants.headerSize];
        if (BitConverter.TryWriteBytes(header, payload.Length))
        {
            header[^1] = (byte)kestrelMessage;

            // combine header and payload into one array
            Span<byte> packet = new byte[header.Length + payload.Length];
            header.CopyTo(packet);
            payload.CopyTo(packet[header.Length..]);

            // write the packet to the pipe
            netStream.Write(packet);
        }
    }
}