namespace DhrMaes.Storage.Core.UnitTests
{
	using DhrMaes.Storage.Core.Structure;
	using DhrMaes.Storage.Core.Structure.Nodes;

	[TestClass]
	public sealed class Test1
	{
		private static readonly IStorageNode _root = new DirectoryNode("root")
		{
			Children =
			[
				new FileNode { Name = "file1.txt" },
				new DirectoryNode("subdir1")
				{
					Children =
					[
						new FileNode { Name = "file2.txt" },
						new DirectoryNode("subsubdir1")
						{
							Children =
							[
								new FileNode { Name = "file3.txt" }
							]
						}
					]
				},
				new FileNode { Name = "file4.txt" }
			],
		};

		[TestMethod]
		public void Walker_Depth_1()
		{
			var walker = new NodeWalker();
			walker.Visit(_root);
			Assert.IsTrue(true);
		}
	}
}
