#pragma warning disable CS7022
using MemoryPack;

public class KestrelReader(Stream stream, KestrelProcessor processor)
{
    private readonly byte[] header = new byte[Constants.headerSize];
    private readonly byte[] data = new byte[4096 * 8]; // 32KB buffer

    public void Run()
    {
        KestrelOptions? options = null;
        Console.WriteLine("Kestrel successfully connected.");

        while (true)
        {
            try
            {
                // read the header, exactly 4 bytes (int) + 1 (Kestrel Message)
                stream.ReadExactly(header);
                int length = BitConverter.ToInt32(header);
                if (length > 0)
                {
                    KestrelMessageType kestrelMessage = (KestrelMessageType)header[^1];
                    // read the rest of the data
                    Span<byte> payload = data.AsSpan()[..length];
                    stream.ReadExactly(payload);
                    switch (kestrelMessage)
                    {
                        case KestrelMessageType.Initialize:
                            {
                                options = MemoryPackSerializer.Deserialize<KestrelOptions>(payload);
                                break;
                            }
                        case KestrelMessageType.AddRoutes:
                            {
                                List<KestrelRoute>? routes = MemoryPackSerializer.Deserialize<List<KestrelRoute>>(payload);
                                if (routes == null)
                                {
                                    Console.WriteLine("Failed to deserialize routes.");
                                    break;
                                }

                                if (options == null)
                                {
                                    Console.WriteLine("Failed to initialize Kestrel, no options provided.");
                                    break;
                                }

                                processor.Init(routes, options);
                                break;
                            }
                        case KestrelMessageType.DispatchResponse:
                            {
                                KestrelResponse? response = MemoryPackSerializer.Deserialize<KestrelResponse>(payload);
                                if (response == null)
                                {
                                    Console.WriteLine("Failed to deserialize response.");
                                    break;
                                }

                                processor.CompletePendingRequest(response);
                                break;
                            }
                        default:
                            Console.WriteLine($"Unknown Kestrel message type: {kestrelMessage}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                break;
            }
        }
    }
}