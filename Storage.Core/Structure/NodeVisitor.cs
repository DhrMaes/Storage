namespace DhrMaes.Storage.Core.Structure
{
	using DhrMaes.Storage.Core.Structure.File;
	using DhrMaes.Storage.Core.Structure.Directory;

	public interface INodeVisitor
	{
		void VisitFileNode(FileNode node);
		void VisitFileNodeReference(FileNodeReference node);
		void VisitDirectoryNode(DirectoryNode node);
		void VisitDirectoryNodeReference(DirectoryNodeReference node);
    }

	public class NodeVisitor : INodeVisitor
	{
		public virtual void Visit(IBaseNode? node)
		{
			if (node is null)
			{
				return;
			}

			node.Accept(this);
		}

		public virtual void VisitDefault(IBaseNode node)
		{
		}

		public virtual void VisitFileNode(FileNode node)
		{
			VisitDefault(node);
		}

		public virtual void VisitFileNodeReference(FileNodeReference node)
		{
			VisitDefault(node);
        }

        public virtual void VisitDirectoryNode(DirectoryNode node)
		{
			VisitDefault(node);
		}

		public virtual void VisitDirectoryNodeReference(DirectoryNodeReference node)
		{
			VisitDefault(node);
        }
    }
}
