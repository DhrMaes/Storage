namespace Storage.Plugin.Contracts;

[Flags]
public enum StorageCapability
{
    None = 0,

    List = 1 << 0,
    Read = 1 << 1,
    Write = 1 << 2,
    Delete = 1 << 3,
    Move = 1 << 4,
}