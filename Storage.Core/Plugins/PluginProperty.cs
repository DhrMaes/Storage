namespace DhrMaes.Storage.Core.Plugins
{
	using System;
	using System.Reflection;

    using DhrMaes.Storage.Core.Providers;

    public struct PluginProperty
	{
		public enum Type
		{
			String,
			Integer,
			Boolean,
			Double,
		}

		private readonly PropertyInfo _property;

        public PluginProperty(
			string name,
			string description,
			Type propertyType,
			PropertyInfo propInfo)
		{
			Name = name;
			Description = description;
			PropertyType = propertyType;
			_property = propInfo;
		}


		public string Name { get; }

		public string Description { get; }

		public Type PropertyType { get; }

		public void SetValue(IStorageProviderConfig config, object value)
		{
			if(!TryParseValue(this, value, out var parsedValue))
			{
				throw new ArgumentException($"value for property '{Name}' is not the correct type. Found value of type '{value.GetType().Name}', but expected '{PropertyType}'.");
			}

			_property.SetValue(config, parsedValue);
		}

		public object? GetValue(IStorageProviderConfig config)
		{
			return _property.GetValue(config);
		}

		public bool TryParseValue(object? value, out object parsedValue)
		{
			return TryParseValue(this, value, out parsedValue);
		}

		public static bool TryParseValue(PluginProperty property, object? value, out object parsedValue)
		{
			try
			{
				switch (property.PropertyType)
				{
					case Type.String:
						parsedValue = Convert.ToString(value) ?? string.Empty;
						return true;
					case Type.Integer:
						parsedValue = Convert.ToInt32(value);
						return true;
					case Type.Boolean:
						parsedValue = Convert.ToBoolean(value);
						return true;
					case Type.Double:
						parsedValue = Convert.ToDouble(value);
						return true;
					default:
						parsedValue = default!;
						return false;
				}
			}
			catch
			{
				parsedValue = default!;
				return false;
			}
		}

		public static bool TryGetType(PropertyInfo? property, out PluginProperty.Type type)
		{
			switch (property?.PropertyType)
			{
				case System.Type t when t == typeof(string):
					type = Type.String;
					return true;
				case System.Type t when t == typeof(int):
					type = Type.Integer;
					return true;
				case System.Type t when t == typeof(bool):
					type = Type.Boolean;
					return true;
				case System.Type t when t == typeof(double):
					type = Type.Double;
					return true;
				default:
					type = default;
					return false;
			}
		}
	}
}
