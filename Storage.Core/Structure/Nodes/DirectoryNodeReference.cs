namespace DhrMaes.Storage.Core.Structure.Nodes
{
    using System.Text.Json.Serialization;

    public struct DirectoryNodeReference : IDirectoryNode
    {
        public DirectoryNodeReference(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        [JsonIgnore]
        public DirectoryNode? Parent { get; set; }

        public static DirectoryNodeReference FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (!path.StartsWith('/'))
            {
                throw new ArgumentException("Only supports absolute paths.");
            }

            // Normalize and split the path
            var segments = path.Trim().Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            DirectoryNode current = new DirectoryNode("Root");
            foreach (var segment in segments)
            {
                // Try to find an existing child directory node
                DirectoryNode? next = null;
                foreach (var child in current.Children)
                {
                    if (child is DirectoryNode dir && dir.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                    {
                        next = dir;
                    }
                }

                // If not found, create a new DirectoryNode
                if (next == null)
                {
                    next = new DirectoryNode(segment)
                    {
                        Parent = current
                    };

                    current.Children.Add(next);
                }

                current = next;
            }

            return current;
        }

        public DirectoryNode GetFullNode()
        {
            
        }

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
