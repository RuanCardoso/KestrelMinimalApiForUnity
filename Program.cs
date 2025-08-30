// With regards to thread safety for reading and writing, up to a single reader and a single writer can use an instance concurrently.
using System.Net;
using System.Net.Sockets;

Console.WriteLine("Starting Kestrel...");
TcpListener listener = new(IPAddress.Any, 7070);
listener.Start();

TcpClient client = listener.AcceptTcpClient();
NetworkStream stream = client.GetStream();

KestrelProcessor processor = new(args);
KestrelReader reader = new(stream, processor);
KestrelWriter writer = new(stream, processor);

Thread readerTh = new((o) =>
{
    reader.Run();
})
{
    Priority = ThreadPriority.Highest
};

Thread writerTh = new((o) =>
{
    writer.Run();
})
{
    Priority = ThreadPriority.Highest
};

readerTh.Start();
writerTh.Start();

Console.ReadKey();