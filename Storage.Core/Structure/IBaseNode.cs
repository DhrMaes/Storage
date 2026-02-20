namespace DhrMaes.Storage.Core.Structure
{
	using DhrMaes.Storage.Core.Structure.Directory;

	public interface IBaseNode
    {
        public string Name { get; }

        public IDirectoryNodeReference? Parent { get; }

        public string GetFullPath();

        void Accept(INodeVisitor visitor);
    }
}
