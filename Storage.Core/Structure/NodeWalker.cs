namespace DhrMaes.Storage.Core.Structure
{
	using DhrMaes.Storage.Core.Structure.Nodes;

	public class NodeWalker : NodeVisitor
	{
		public override void VisitDirectoryNode(DirectoryNode node)
		{
			foreach (var child in node.Children)
			{
				Visit(child);
			}
		}
	}
}
