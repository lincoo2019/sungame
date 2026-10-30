using Godot;
using Godot.Collections;

public sealed class GameStateReplicator : NetworkReplicatorBase
{
	private const double SyncInterval = 0.5;

	private readonly IGameStateNetworkContext _gameContext;

	private double _syncTimer;

	public GameStateReplicator()
	{
	}

	public GameStateReplicator(IGameStateNetworkContext gameContext)
	{
		_gameContext = gameContext;
	}

	public override void Process(double delta)
	{
		if (base.Context != null && base.Context.IsMultiplayerActive && _gameContext != null && base.Context.IsHost)
		{
			_syncTimer += delta;
			if (_syncTimer >= 0.5)
			{
				_syncTimer = 0.0;
				BroadcastState();
			}
		}
	}

	public void BroadcastState()
	{
		if (base.Context == null || !base.Context.IsMultiplayerActive || _gameContext == null)
		{
			return;
		}
		Dictionary dictionary = new Dictionary();
		foreach (StringName key in _gameContext.Features.Keys)
		{
			Dictionary dictionary2 = _gameContext.Features[key].SyncSerialize();
			if (dictionary2.Count > 0)
			{
				Dictionary b = _gameContext.GameStateLastSync.GetValueOrDefault(key, new Dictionary()).AsGodotDictionary();
				if (!NetworkVariantComparer.DictionaryApproxEquals(dictionary2, b))
				{
					dictionary[key] = dictionary2;
					_gameContext.GameStateLastSync[key] = dictionary2.Duplicate(deep: true);
				}
			}
		}
		if (_gameContext.Process != null)
		{
			Dictionary dictionary3 = _gameContext.Process.SyncSerialize();
			if (dictionary3.Count > 0)
			{
				Dictionary b2 = _gameContext.GameStateLastSync.GetValueOrDefault("process", new Dictionary()).AsGodotDictionary();
				if (!NetworkVariantComparer.DictionaryApproxEquals(dictionary3, b2))
				{
					dictionary["process"] = dictionary3;
					_gameContext.GameStateLastSync["process"] = dictionary3.Duplicate(deep: true);
				}
			}
		}
		if (dictionary.Count > 0)
		{
			_gameContext.SendGameState(Json.Stringify(dictionary));
		}
	}

	public void ApplyState(Dictionary state)
	{
		if (_gameContext == null || state == null)
		{
			return;
		}
		foreach (Variant key in state.Keys)
		{
			StringName stringName = key.AsStringName();
			if (key.AsString() == "process")
			{
				_gameContext.Process?.SyncDeserialize(state[key].AsGodotDictionary());
				continue;
			}
			if (!_gameContext.Features.ContainsKey(stringName))
			{
				_gameContext.EnsureFeature(stringName);
			}
			if (_gameContext.Features.ContainsKey(stringName))
			{
				_gameContext.Features[stringName].SyncDeserialize(state[key].AsGodotDictionary());
			}
		}
	}

	public Dictionary BuildFullState()
	{
		Dictionary dictionary = new Dictionary();
		if (_gameContext == null)
		{
			return dictionary;
		}
		foreach (StringName key in _gameContext.Features.Keys)
		{
			TowerDefenseBattleFeature towerDefenseBattleFeature = _gameContext.Features[key];
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeature))
			{
				Dictionary dictionary2 = towerDefenseBattleFeature.SyncSerialize();
				if (dictionary2.Count > 0)
				{
					dictionary[key] = dictionary2;
				}
			}
		}
		if (_gameContext.Process != null)
		{
			Dictionary dictionary3 = _gameContext.Process.SyncSerialize();
			if (dictionary3.Count > 0)
			{
				dictionary["process"] = dictionary3;
			}
		}
		return dictionary;
	}
}
