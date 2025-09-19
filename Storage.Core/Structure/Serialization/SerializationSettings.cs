namespace DhrMaes.Storage.Core.Structure.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal class SerializationSettings
    {
        public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
        {
            WriteIndented = true,
            ////Converters =
            ////{
            ////    //new NodeConverterFactory(),
            ////    new DirectoryNodeConverter(),
            ////    new FileNodeConverter(),
            ////    //new StorageNodeInterfaceConverter(),
            ////}
        };
    }
}
