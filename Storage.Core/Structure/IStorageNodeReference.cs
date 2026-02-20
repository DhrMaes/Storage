namespace DhrMaes.Storage.Core.Structure
{
    public interface IStorageNodeReference<out T> : IBaseNode
        where T : IStorageNode
    {
        T ToStorageNode(IStorage storage);
    }
}
