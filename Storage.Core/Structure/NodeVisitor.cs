namespace DhrMaes.Storage.Core.Structure
{
	using DhrMaes.Storage.Core.Structure.Nodes;

	public interface INodeVisitor
	{
		void VisitFileNode(FileNode node);
		void VisitDirectoryNode(DirectoryNode node);
	}

	public class NodeVisitor : INodeVisitor
	{
		public virtual void Visit(IStorageNode? node)
		{
			if (node is null)
			{
				return;
			}

			node.Accept(this);
		}

		public virtual void VisitDefault(IStorageNode node)
		{
		}

		public virtual void VisitFileNode(FileNode node)
		{
			VisitDefault(node);
		}

		public virtual void VisitDirectoryNode(DirectoryNode node)
		{
			VisitDefault(node);
		}
	}
}
