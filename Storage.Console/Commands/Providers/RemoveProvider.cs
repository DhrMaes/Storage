namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.Collections.Generic;
	using System.CommandLine;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using DhrMaes.Storage.Core;

	internal class RemoveProvider
	{
        internal static Command Create(Dmc dmc)
        {
            var idOption = new Option<string>(
                name: "--id",
                description: "The ID of the provider to remove")
            {
                IsRequired = true,
            };

            var command = new Command("remove", "Remove a provider");
            command.AddAlias("r");
            command.AddOption(idOption);
            command.SetHandler((id) =>
            {
                dmc.RemoveProvider(id);
            }, idOption);
            return command;
        }
    }
}
