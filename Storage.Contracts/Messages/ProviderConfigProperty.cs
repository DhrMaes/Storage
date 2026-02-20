namespace DhrMaes.Storage.Protobuf.Configuration.Providers.v1
{
    using System;

    public sealed partial class ProviderConfigProperty
    {
        public static ProviderConfigPropertyType GetPropertyType(Type type)
        {
            if (type == typeof(string))
            {
                return ProviderConfigPropertyType.String;
            }
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                return ProviderConfigPropertyType.Int;
            }
            if (type == typeof(bool))
            {
                return ProviderConfigPropertyType.Bool;
            }
            if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
            {
                return ProviderConfigPropertyType.Double;
            }
            if (type.IsEnum)
            {
                return ProviderConfigPropertyType.Int;
            }

            return ProviderConfigPropertyType.Unspecified;
        }

        public object GetValue()
        {
            switch (Type)
            {
                case ProviderConfigPropertyType.String:
                    return StringValue ?? string.Empty;
                case ProviderConfigPropertyType.Int:
                    return IntValue;
                case ProviderConfigPropertyType.Bool:
                    return BoolValue;
                case ProviderConfigPropertyType.Double:
                    return DoubleValue;
                default:
                    throw new InvalidOperationException($"Unsupported property type: {Type}");
            }
        }

        public T GetValue<T>()
        {
            switch (Type)
            {
                case ProviderConfigPropertyType.String:
                    return (T)Convert.ChangeType(StringValue, typeof(T));
                case ProviderConfigPropertyType.Int:
                    return (T)Convert.ChangeType(IntValue, typeof(T));
                case ProviderConfigPropertyType.Bool:
                    return (T)Convert.ChangeType(BoolValue, typeof(T));
                case ProviderConfigPropertyType.Double:
                    return (T)Convert.ChangeType(DoubleValue, typeof(T));
                default:
                    throw new InvalidOperationException($"Unsupported property type: {Type}");
            }
        }

        public void SetValue(object value)
        {
            var type = value.GetType();
            var propertyType = GetPropertyType(type);
            switch (propertyType)
            {
                case ProviderConfigPropertyType.String:
                    StringValue = Convert.ToString(value);
                    break;
                case ProviderConfigPropertyType.Int:
                    IntValue = Convert.ToInt32(value);
                    break;
                case ProviderConfigPropertyType.Bool:
                    BoolValue = Convert.ToBoolean(value);
                    break;
                case ProviderConfigPropertyType.Double:
                    DoubleValue = Convert.ToDouble(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported property type: {Type}");
            }
        }
    }
}
