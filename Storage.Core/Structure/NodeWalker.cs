namespace DhrMaes.Storage.Core.Structure
{
	using DhrMaes.Storage.Core.Structure.Directory;

	public class NodeWalker : NodeVisitor
	{
		public override void VisitDirectoryNode(DirectoryNode node)
		{
			foreach (var child in node.Directories)
			{
				Visit(child);
			}

			foreach(var child in node.Files)
			{
				Visit(child);
            }
        }
	}
}
