namespace DhrMaes.Storage.Console
{
	using System;
	using System.CommandLine;
	using System.Threading.Tasks;

	using DhrMaes.Storage.Core;

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

            args = new[]
            {
                "fs",
                "ls", "/",
            };

            using var dmc = new Dmc();
			var rootCommand = Commands.RootCommandFactory.Create(dmc);
			return await rootCommand.InvokeAsync(args);
		}

		private static async Task<int> HandleProviderCommand(ProviderType type)
		{
			Console.WriteLine($"Provider type: {type}");
			return 0;
		}
	}
}
