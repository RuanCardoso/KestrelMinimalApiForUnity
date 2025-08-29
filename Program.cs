// With regards to thread safety for reading and writing, up to a single reader and a single writer can use an instance concurrently.
using System.IO.Pipes;

NamedPipeServerStream pipeServer = new("HttpPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
KestrelProcessor processor = new(args);
KestrelReader reader = new(pipeServer, processor);
KestrelWriter writer = new(pipeServer, processor);

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

//processor.Init(new List<KestrelRoute>
//{
//    new KestrelRoute
//    {
//        Method = "GET",
//        Route = "/"
//    },
//    new KestrelRoute
//    {
//        Method = "POST",
//        Route = "/post"
//    }
//}, new KestrelOptions
//{
//    Port = 80,
//    KeepAliveTimeout = 120
//});


Console.ReadKey();