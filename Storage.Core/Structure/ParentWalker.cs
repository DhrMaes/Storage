namespace DhrMaes.Storage.Core.Structure
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using DhrMaes.Storage.Core.Structure.Nodes;

	internal class ParentWalker : NodeWalker
	{
		public override void VisitDirectoryNode(DirectoryNode node)
		{
			foreach(var child in node.Children)
			{
				child.Parent = node;
			}

			base.VisitDirectoryNode(node);
        }
	}
}
