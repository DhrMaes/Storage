namespace DhrMaes.Storage.Server.Nodes
{
	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.File;
	using DhrMaes.Storage.Protobuf.Structure.v1;

    public class NodeTranslator : DhrMaes.Storage.Core.Structure.NodeWalker
    {
        private StorageNode? _result;

        private NodeTranslator()
        {
            _result = null;
        }

        public static StorageNode? Translate(IStorageNode node)
        {
            var translator = new NodeTranslator();
            translator.Visit(node);
            return translator._result;
        }

        public override void VisitFileNode(Core.Structure.File.FileNode node)
        {
            _result = new StorageNode
            {
                File = new FileNode
                {
                    Name = node.Name,
                    Size = node.Size,
                },
            };
        }

        public override void VisitDirectoryNode(DhrMaes.Storage.Core.Structure.Nodes.DirectoryNode node)
        {
            _result = new StorageNode
            {
                Directory = new DirectoryNode
                {
                    Name = node.Name,
                    Children =
                    {
                        node.Children
                            .Select(child => Translate(child))
                            .Where(child => child is not null)
                            .ToList()!,
                    }
                },
            };
        }
    }
}
