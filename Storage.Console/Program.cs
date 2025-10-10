namespace DhrMaes.Storage.Console
{
    using System;
    using System.CommandLine;
    using System.Threading.Tasks;

    using Grpc.Net.Client;

    public class Program
    {
        enum ProviderType
        {
            FileSystem,
        }

        public static async Task<int> Main(string[] args)
        {
            //args = new[]
            //{
            //	"provider",
            //	"add",
            //	"FileSystem",  "--path", "C:\\Users\\ArneMA\\Documents\\DhrMaesCloud",
            //};

            //args = new[]
            //{
            //	"fs",
            //	"mkdir", "/Images",
            //};

            //args = new[]
            //{
            //    "fs",
            //    "rmdir", "/Images",
            //};

            //args = new[]
            //{
            //    "fs",
            //    "ls", "/",
            //};

            //args = new[]
            //{
            //    "fs",
            //    "upload", @"C:\Users\ArneMA\Downloads\Bellewaerde Maes Arne.pdf", "/Documents/Bellewaerde Maes Arne.pdf",
            //};

            //args = new[]
            //{
            //    "fs",
            //    "rm", "/Documents/Bellewaerde Maes Arne.pdf",
            //};

            //args = new[]
            //{
            //    "provider",
            //    "add", "GoogleDrive",
            //};

            //args = new[]
            //{
            //    "provider",
            //    "add", "--help",
            //};

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);// Add explicit channel options
            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan
                }
            };

            using var channel = GrpcChannel.ForAddress("http://localhost:32798", channelOptions);
            if (channel is null)
            {
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "Failed to create gRPC channel."));
            }

            var rootCommand = Commands.RootCommandFactory.Create(channel);
            return await rootCommand.InvokeAsync(args);


            //         using var dmc = new Dmc();
            //var rootCommand = Commands.RootCommandFactory.Create(dmc);
            //return await rootCommand.InvokeAsync(args);
        }
    }
}
