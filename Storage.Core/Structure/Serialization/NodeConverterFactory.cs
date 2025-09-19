namespace DhrMaes.Storage.Core.Structure.Serialization
{
    using System;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Xml.Linq;

    using DhrMaes.Storage.Core.Structure.Nodes;

    internal class NodeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IStorageNode).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if( typeToConvert == typeof(DirectoryNode))
                return new DirectoryNodeConverter();

            if (typeToConvert == typeof(FileNode))
                return new FileNodeConverter();

            return new StorageNodeInterfaceConverter(this);
        }

        private sealed class StorageNodeInterfaceConverter : JsonConverter<IStorageNode>
        {
            private readonly NodeConverterFactory _parentConverter;

            public StorageNodeInterfaceConverter(
                NodeConverterFactory parentConverter)
            {
                _parentConverter = parentConverter;
            }

            public override IStorageNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                if (!root.TryGetProperty("NodeType", out var typeProp))
                    throw new JsonException("Missing NodeType discriminator.");

                var nodeType = typeProp.GetString();
                return nodeType switch
                {
                    "Directory" => JsonSerializer.Deserialize<DirectoryNode>(root.GetRawText(), options),
                    "File" => JsonSerializer.Deserialize<FileNode>(root.GetRawText(), options),
                    _ => throw new JsonException($"Unknown NodeType: {nodeType}")
                };
            }

            public override void Write(Utf8JsonWriter writer, IStorageNode value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                // Always inject NodeType first
                var nodeType = value switch
                {
                    FileNode => "File",
                    DirectoryNode => "Directory",
                    _ => throw new JsonException($"Unknown node type: {value.GetType().Name}")
                };
                writer.WriteString("NodeType", nodeType);

                // Serialize the object normally, but replay its properties (excluding NodeType itself)
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, value.GetType(), options));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("NodeType")) continue; // avoid duplicate
                    prop.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
        }
    }
}
