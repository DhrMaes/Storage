namespace DhrMaes.Storage.Core.FileSystem
{
	using System;

	public class FileSystem
	{
		public static string GetUserConfigDir()
		{
			string? baseDir;

			if (OperatingSystem.IsWindows())
			{
				baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			}
			else
			{
				baseDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
						  ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config";
			}

			return Path.Combine(baseDir, "DhrMaes", "Storage");
		}

		public static string GetPluginsDir()
		{
			return Path.Combine(
				GetUserConfigDir(),
				"Plugins");
		}

        public static string GetProvidersDir()
        {
            return Path.Combine(
                GetUserConfigDir(),
                "Providers");
        }
    }
}
