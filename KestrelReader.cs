#pragma warning disable CS7022
using MemoryPack;

namespace KestrelMinimalApiForUnity;

public class KestrelReader(Stream netStream, KestrelProcessor kProcessor)
{
    private readonly byte[] header = new byte[Constants.headerSize];
    private readonly byte[] data = new byte[4096 * 8 * 2]; // 64KB buffer

    public void Run()
    {
        KestrelOptions? kOptions = null;
        Console.WriteLine("Kestrel successfully connected.");

        while (true)
        {
            try
            {
                // read the header, exactly 4 bytes (int) + 1 (Kestrel Message)
                netStream.ReadExactly(header);
                int length = BitConverter.ToInt32(header);
                if (length > 0)
                {
                    KestrelMessageType kestrelMessage = (KestrelMessageType)header[^1];
                    // read the rest of the data
                    Span<byte> payload = data.AsSpan()[..length];
                    netStream.ReadExactly(payload);
                    switch (kestrelMessage)
                    {
                        case KestrelMessageType.Initialize:
                            {
                                kOptions = MemoryPackSerializer.Deserialize<KestrelOptions>(payload);
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

                                if (kOptions == null)
                                {
                                    Console.WriteLine("Failed to initialize Kestrel, no options provided.");
                                    break;
                                }

                                kProcessor.Init(routes, kOptions);
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

                                kProcessor.CompletePendingRequest(response);
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