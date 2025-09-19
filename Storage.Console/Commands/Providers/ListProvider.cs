namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.Collections.Generic;
	using System.CommandLine;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Threading.Tasks;

	using DhrMaes.Storage.Core;
	using DhrMaes.Storage.Core.Providers;

	internal class ListProvider
	{
		internal static Command Create(Dmc dmc)
		{
			var command = new Command("list", "List all configured providers");
			command.AddAlias("ls");
			command.SetHandler(() =>
			{
				var providers = dmc.GetProviders();
				foreach (var provider in providers)
				{
					var type = provider.GetType();
					var providerType = type.GetCustomAttribute<ProviderIdentifierAttribute>()?.Id ?? "Unknown";
                    Console.WriteLine($"[{providerType}]:\t{provider.Identifier}");
				}
			});
			return command;
        }
    }
}
