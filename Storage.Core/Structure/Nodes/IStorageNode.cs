namespace DhrMaes.Storage.Core.Structure.Nodes
{
    using System.Text.Json.Serialization;

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "NodeType")]
    [JsonDerivedType(typeof(FileNode), "File")]
    [JsonDerivedType(typeof(DirectoryNode), "Directory")]
    public interface IStorageNode : IEquatable<IStorageNode>
    {
        [JsonIgnore]
        public DirectoryNode? Parent { get; set; }

        public string Name { get; set; }

        public string GetFullPath();

        void Accept(INodeVisitor visitor);
    }
}
