namespace DhrMaes.Storage.Core.Structure.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    using DhrMaes.Storage.Core.Structure.Nodes;

    internal class DirectoryNodeConverter : JsonConverter<DirectoryNode>
    {
        public override DirectoryNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Just let the default serializer handle it
            var dir = JsonSerializer.Deserialize<DirectoryNode>(ref reader, options);

            // Restore Parent pointers
            if (dir != null)
            {
                foreach (var child in dir.Children)
                {
                    child.Parent = dir;
                }
            }

            return dir;
        }

        public override void Write(Utf8JsonWriter writer, DirectoryNode value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Add discriminator first
            writer.WriteString("NodeType", "Directory");

            var innerOptions = new JsonSerializerOptions(options);
            for (int i = innerOptions.Converters.Count - 1; i >= 0; i--)
            {
                if (innerOptions.Converters[i] is DirectoryNodeConverter)
                {
                    innerOptions.Converters.RemoveAt(i);
                }
            }

            // Serialize the DirectoryNode normally and replay its properties
            using (var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, options)))
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals("NodeType")) continue; // avoid double write
                    prop.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }
    }
}
