namespace DhrMaes.Storage.Core.Structure.Nodes
{
    using System.Text.Json.Serialization;

    public class DirectoryNode : IStorageNode
    {
        public DirectoryNode(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public ICollection<IStorageNode> Children { get; set; } = new List<IStorageNode>();

        [JsonIgnore]
        public IStorageNode? Parent { get; set; }

        public string GetFullPath()
        {
            if (Parent is null)
            {
                return String.Empty;
            }

            return Path.Combine(Parent.GetFullPath(), Name);
        }

        public void Accept(INodeVisitor visitor)
        {
            visitor.VisitDirectoryNode(this);
        }

        public bool Equals(IStorageNode? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!(other is DirectoryNode otherDir))
            {
                return false;
            }

            if (this.GetFullPath() == otherDir.GetFullPath())
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return $"Directory:{GetFullPath()}".GetHashCode();
        }
    }
}
