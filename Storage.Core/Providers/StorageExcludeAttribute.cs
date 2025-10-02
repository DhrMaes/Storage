namespace DhrMaes.Storage.Core.Providers
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StorageExcludeAttribute : Attribute
    {
    }
}
