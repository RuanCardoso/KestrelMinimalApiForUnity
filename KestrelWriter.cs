using System.IO.Pipes;
using System.Net;
using System.Threading.Channels;
using MemoryPack;

public class KestrelWriter(NamedPipeServerStream pipeServer, KestrelProcessor processor)
{
    private readonly Channel<KestrelChannelMessage> messageChannel = Channel.CreateUnbounded<KestrelChannelMessage>();
    public async void Run()
    {
        InitializeChannel();
        await foreach (var msg in messageChannel.Reader.ReadAllAsync())
        {
            InternalSend(msg.MessageType, msg.Payload);
        }
    }

    private void InitializeChannel()
    {
        processor.OnRequest = (route, request, response) =>
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

            KestrelChannelMessage message = new()
            {
                MessageType = KestrelMessageType.DispatchRequest,
                Payload = MemoryPackSerializer.Serialize(routeRequest)
            };

            var writer = messageChannel.Writer;
            writer.TryWrite(message);
            return processor.AddPendingRequest(routeRequest.UniqueId);
        };
    }

    private void InternalSend(KestrelMessageType kestrelMessage, ReadOnlySpan<byte> payload)
    {
        if (pipeServer == null || !pipeServer.IsConnected)
            return;

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
            pipeServer.Write(packet);
        }
    }
}