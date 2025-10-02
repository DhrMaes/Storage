namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.CommandLine;
	
    using DhrMaes.Storage.Messages;

	internal class RemoveProvider
	{
        internal static Command Create(ProviderService.ProviderServiceClient client)
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
                Console.WriteLine("This feature is not yet implemented.");
                //dmc.RemoveProvider(id);
            }, idOption);
            return command;
        }
    }
}
