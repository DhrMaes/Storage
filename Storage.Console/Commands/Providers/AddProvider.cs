namespace DhrMaes.Storage.Console.Commands.Providers
{
	using System;
	using System.Collections.Generic;
	using System.CommandLine;
	using System.CommandLine.Invocation;
	using System.Reflection;

	using DhrMaes.Storage.Core;
	using DhrMaes.Storage.Core.Providers;

	internal class AddProvider
	{
        internal static Command Create(Dmc dmc)
        {
            var command = new Command("add", "Add a new provider");
            command.AddAlias("a");

            foreach (var providerCommand in LoadProviderCommands(dmc))
            {
                command.AddCommand(providerCommand);
            }

            return command;
        }

        private static IEnumerable<Command> LoadProviderCommands(Dmc dmc)
        {
            var configTypes = dmc.PluginLoader.ConfigTypes;
            foreach (var kvp in configTypes)
            {
                var command = new Command(kvp.Key, $"Add a new {kvp.Key} provider");
                var options = new Dictionary<PropertyInfo, Option>();

                foreach (var prop in kvp.Value.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!IsSupportedType(prop.PropertyType))
                        continue;

                    var optionType = typeof(Option<>).MakeGenericType(prop.PropertyType);
                    var option = (Option)Activator.CreateInstance(optionType, $"--{prop.Name.ToLower()}", $"Sets {prop.Name}")!;
                    options[prop] = option;
                    command.AddOption(option);
                }

                command.SetHandler((InvocationContext ctx) =>
                {
                    var config = Activator.CreateInstance(kvp.Value)! as IStorageProviderConfig;

                    foreach (var (prop, opt) in options)
                    {
                        var value = ctx.ParseResult.GetValueForOption(opt);
                        if (value is not null)
                        {
                            prop.SetValue(config, value);
                        }
                    }

                    dmc.AddProvider(config);
                });

                yield return command;
            }
        }

        private static bool IsSupportedType(Type t)
        {
            if (t.IsPrimitive || t == typeof(string) || t == typeof(decimal))
                return true;

            if (t.IsEnum)
                return true;

            return false;
        }
    }
}
