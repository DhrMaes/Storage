namespace Storage.Plugin.Contracts;

public interface IStorageLogger
{
    void Log(string message, Exception? exception = null);
}