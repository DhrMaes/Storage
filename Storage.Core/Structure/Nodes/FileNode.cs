namespace DhrMaes.Storage.Core.Structure.Nodes
{
    using System.Text.Json.Serialization;

    using DhrMaes.Storage.Core.Providers;

    public class FileNode : IStorageNode
    {
        public string Name { get; set; }

        [JsonIgnore]
        public IStorageNode? Parent { get; set; }

        public string GetFullPath()
        {
            if (Parent == null)
            {
                return Name;
            }

            return Path.Combine(Parent.GetFullPath(), Name);
        }

        public void Accept(INodeVisitor visitor)
        {
            visitor.VisitFileNode(this);
        }

        public bool Equals(IStorageNode? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!(other is FileNode otherFile))
            {
                return false;
            }

            if(this.GetFullPath() == otherFile.GetFullPath())
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return $"File:{GetFullPath()}".GetHashCode();
        }
    }
}
