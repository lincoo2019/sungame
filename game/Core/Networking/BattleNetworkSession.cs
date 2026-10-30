using System;
using System.Collections.Generic;

public sealed class BattleNetworkSession : IDisposable
{
	private readonly List<INetworkReplicator> _replicators = new List<INetworkReplicator>();

	private readonly Dictionary<NetMessageType, INetworkCommandHandler> _commandHandlers = new Dictionary<NetMessageType, INetworkCommandHandler>();

	private bool _disposed;

	public IBattleNetworkContext Context { get; }

	public NetworkMessageRouter Router { get; }

	public NetworkEntityRegistry Entities { get; }

	public bool IsActive
	{
		get
		{
			if (!_disposed && Context != null)
			{
				return Context.IsMultiplayerActive;
			}
			return false;
		}
	}

	public bool IsHost
	{
		get
		{
			if (IsActive)
			{
				return Context.IsHost;
			}
			return false;
		}
	}

	public BattleNetworkSession(IBattleNetworkContext context, NetworkMessageRouter router, NetworkEntityRegistry entityRegistry)
	{
		Context = context;
		Router = router ?? new NetworkMessageRouter();
		Entities = entityRegistry ?? new NetworkEntityRegistry();
	}

	public void AddReplicator(INetworkReplicator replicator)
	{
		if (replicator != null && !_replicators.Contains(replicator))
		{
			replicator.Initialize(this);
			_replicators.Add(replicator);
		}
	}

	public void AddCommandHandler(INetworkCommandHandler handler)
	{
		if (handler != null)
		{
			_commandHandlers[handler.MessageType] = handler;
			Router.Register(handler.MessageType, HandleCommand);
		}
	}

	public void Process(double delta)
	{
		if (!IsActive)
		{
			return;
		}
		foreach (INetworkReplicator replicator in _replicators)
		{
			replicator.Process(delta);
		}
	}

	public void ApplyLocalPause()
	{
		Context?.ApplyPause(paused: true);
	}

	public void ApplyLocalResume()
	{
		Context?.ApplyPause(paused: false);
	}

	private void HandleCommand(NetMessageContext context)
	{
		if (context != null && _commandHandlers.TryGetValue(context.MessageType, out var value))
		{
			value.Handle(context, this);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (INetworkReplicator replicator in _replicators)
		{
			replicator.Dispose();
		}
		_replicators.Clear();
		_commandHandlers.Clear();
	}
}
