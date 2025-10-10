namespace DhrMaes.Storage.Server
{
	using DhrMaes.Storage.Core;
	using DhrMaes.Storage.Server.Services;

	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

            // Setup web server to use HTTP/2 without TLS.
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(8080, listenOptions =>
                {
                    listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                });
            });

            // Add services to the container.
            builder.Services.AddGrpc();
			builder.Services.AddSingleton<IDmc>(new Dmc());

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			app.MapGrpcService<StorageService>();
			app.MapGrpcService<ProviderService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

			app.Run();
		}
	}
}