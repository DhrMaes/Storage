namespace DhrMaes.Storage.Core.Providers
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StoragePropertyAttribute : Attribute
    {
        public StoragePropertyAttribute()
        {
            Name = String.Empty;
        }

        public StoragePropertyAttribute(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "Name cannot be null or empty.");
            }

            Name = name;
        }

        public string Name { get; }
    }
}
