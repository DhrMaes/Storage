namespace Storage.Plugin.Loader;

using System.Collections.Concurrent;
using Storage.Plugin.Contracts;

public sealed class StorageConnectionTracker
{
	private readonly ConcurrentDictionary<string, ConnectionSet> _connectionsByPluginId = new(StringComparer.OrdinalIgnoreCase);

	public void TrackConnection(string pluginId, IStorageConnection connection)
	{
		if (string.IsNullOrWhiteSpace(pluginId))
			throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(pluginId));

		if (connection is null)
			throw new ArgumentNullException(nameof(connection));

		var connectionSet = _connectionsByPluginId.GetOrAdd(pluginId, _ => new ConnectionSet());
		connectionSet.Add(connection);
	}

	public bool HasActiveConnections(string pluginId)
	{
		if (!_connectionsByPluginId.TryGetValue(pluginId, out var connectionSet))
			return false;

		return connectionSet.HasActiveConnections();
	}

	public int GetActiveConnectionCount(string pluginId)
	{
		if (!_connectionsByPluginId.TryGetValue(pluginId, out var connectionSet))
			return 0;

		return connectionSet.GetActiveCount();
	}

	public async Task<int> DisposeAllConnectionsAsync(string pluginId, CancellationToken cancellationToken = default)
	{
		if (!_connectionsByPluginId.TryGetValue(pluginId, out var connectionSet))
			return 0;

		return await connectionSet.DisposeAllAsync(cancellationToken);
	}

	public void CleanupDeadReferences()
	{
		foreach (var connectionSet in _connectionsByPluginId.Values)
		{
			connectionSet.CleanupDeadReferences();
		}
	}

	public void Untrack(string pluginId)
	{
		_connectionsByPluginId.TryRemove(pluginId, out _);
	}

	public Dictionary<string, int> GetConnectionCountsByPlugin()
	{
		var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var kvp in _connectionsByPluginId)
		{
			var count = kvp.Value.GetActiveCount();
			if (count > 0)
			{
				result[kvp.Key] = count;
			}
		}

		return result;
	}

	private sealed class ConnectionSet
	{
		private readonly List<WeakReference<IStorageConnection>> _connections = new();
		private readonly object _lock = new();

		public void Add(IStorageConnection connection)
		{
			lock (_lock)
			{
				_connections.Add(new WeakReference<IStorageConnection>(connection));
			}
		}

		public bool HasActiveConnections()
		{
			lock (_lock)
			{
				CleanupDeadReferencesInternal();
				return _connections.Count > 0;
			}
		}

		public int GetActiveCount()
		{
			lock (_lock)
			{
				CleanupDeadReferencesInternal();
				return _connections.Count;
			}
		}

		public async Task<int> DisposeAllAsync(CancellationToken cancellationToken)
		{
			List<IStorageConnection> activeConnections;

			lock (_lock)
			{
				activeConnections = new List<IStorageConnection>(_connections.Count);

				foreach (var weakRef in _connections)
				{
					if (weakRef.TryGetTarget(out var connection))
					{
						activeConnections.Add(connection);
					}
				}

				_connections.Clear();
			}

			int disposedCount = 0;
			foreach (var connection in activeConnections)
			{
				try
				{
					await connection.DisposeAsync();
					disposedCount++;
				}
				catch
				{
					// Ignore disposal errors - connection might already be disposed
				}
			}

			return disposedCount;
		}

		public void CleanupDeadReferences()
		{
			lock (_lock)
			{
				CleanupDeadReferencesInternal();
			}
		}

		private void CleanupDeadReferencesInternal()
		{
			_connections.RemoveAll(weakRef => !weakRef.TryGetTarget(out _));
		}
	}
}
