namespace DhrMaes.Storage.Core.Structure.Nodes
{
    using System.Text.Json.Serialization;

	using DhrMaes.Storage.Core.FileSystem;

	public class FileNode : IStorageNode
    {
        public FileNode(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        [JsonIgnore]
        public FileSize Size { get; set; } = FileSize.Unknown;

        [JsonIgnore]
        public DirectoryNode? Parent { get; set; }

        public static FileNode FromPath(string path)
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
            foreach (var segment in segments.Take(segments.Length - 1))
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

            var fileNode = new FileNode(segments.Last())
            {
                Parent = current
            };
            current.Children.Add(fileNode);
            return fileNode;
        }

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

            if (this.GetFullPath() == otherFile.GetFullPath())
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
