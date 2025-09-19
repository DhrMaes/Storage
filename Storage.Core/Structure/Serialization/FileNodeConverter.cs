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

	internal class FileNodeConverter : JsonConverter<FileNode>
	{
		public override FileNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Just let the default serializer handle it
            var dir = JsonSerializer.Deserialize<FileNode>(ref reader, options);
            return dir;
        }
		
        public override void Write(Utf8JsonWriter writer, FileNode value, JsonSerializerOptions options)
		{
            writer.WriteStartObject();

            // Add discriminator first
            writer.WriteString("NodeType", "File");

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
