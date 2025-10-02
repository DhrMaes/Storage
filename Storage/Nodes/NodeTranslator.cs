namespace DhrMaes.Storage.Server.Nodes
{
    using DhrMaes.Storage.Messages;
    using DhrMaes.Storage.Core.Structure.Nodes;

    public class NodeTranslator : DhrMaes.Storage.Core.Structure.NodeWalker
    {
        private StorageNodeProto? _result;

        private NodeTranslator()
        {
            _result = null;
        }

        public static StorageNodeProto? Translate(IStorageNode node)
        {
            var translator = new NodeTranslator();
            translator.Visit(node);
            return translator._result;
        }

        public override void VisitFileNode(FileNode node)
        {
            _result = new StorageNodeProto
            {
                File = new FileNodeProto
                {
                    Name = node.Name,
                    Size = node.Size,
                },
            };
        }

        public override void VisitDirectoryNode(DirectoryNode node)
        {
            _result = new StorageNodeProto
            {
                Directory = new DirectoryNodeProto
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
