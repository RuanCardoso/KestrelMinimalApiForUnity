using System.Net;
using System.Net.Sockets;

namespace KestrelMinimalApiForUnity;

public class Kestrel
{
    private const int kPort = 60123;
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting Kestrel...");
        TcpListener tcpServer = new(IPAddress.Any, kPort);
        tcpServer.Start();

        TcpClient tcpClient = tcpServer.AcceptTcpClient();
        NetworkStream netStream = tcpClient.GetStream();

        KestrelProcessor kProcessor = new(args);
        KestrelReader kReader = new(netStream, kProcessor);
        KestrelWriter kWriter = new(netStream, kProcessor);

        Thread rTh = new((o) => kReader.Run())
        {
            Priority = ThreadPriority.Highest
        };

        Thread wTh = new((o) => kWriter.Run())
        {
            Priority = ThreadPriority.Highest
        };

        rTh.Start();
        wTh.Start();
    }
}