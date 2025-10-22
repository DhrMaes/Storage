namespace DhrMaes.Storage.Core.Structure
{
	using System.Text.Json.Serialization;

	using DhrMaes.Storage.Core.Structure.File;
	using DhrMaes.Storage.Core.Structure.Directory;

	[JsonPolymorphic(TypeDiscriminatorPropertyName = "NodeType")]
    [JsonDerivedType(typeof(FileNode), "File")]
    [JsonDerivedType(typeof(DirectoryNode), "Directory")]
    public interface IStorageNode : IEquatable<IStorageNode>
    {
        [JsonIgnore]
        public IDirectoryNode? Parent { get; set; }

        public string Name { get; set; }

        public string GetFullPath();

        void Accept(INodeVisitor visitor);
    }
}
