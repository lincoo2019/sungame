using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class ZombieStateReplicator : NetworkReplicatorBase
{
	private readonly record struct ZombiePriorityState(string Clip, bool Loop, bool NearDie, bool Die, int TimeScale, int WalkSpeedScale, int GridY, int SpecialState);

	private const int ZombieSyncBatchSize = 40;

	private const int ZombieRosterSyncEveryBatches = 10;

	private const double ZombieSyncInterval = 0.05;

	private const double ZombiePrioritySyncInterval = 0.03;

	private readonly IZombieStateNetworkContext _zombieContext;

	private Array _zombieSyncKeys = new Array();

	private readonly System.Collections.Generic.Dictionary<int, ZombiePriorityState> _zombiePriorityStates = new System.Collections.Generic.Dictionary<int, ZombiePriorityState>();

	private int _zombieSyncBatchIndex;

	private int _zombieRosterSyncCounter;

	private int _lastZombieRosterCount = -1;

	private double _syncTimer;

	private double _prioritySyncTimer;

	public ZombieStateReplicator()
	{
	}

	public ZombieStateReplicator(IZombieStateNetworkContext zombieContext)
	{
		_zombieContext = zombieContext;
	}

	public override void Process(double delta)
	{
		if (base.Context == null || !base.Context.IsMultiplayerActive || _zombieContext == null)
		{
			return;
		}
		if (base.Context.IsHost)
		{
			_syncTimer += delta;
			if (_syncTimer >= 0.05)
			{
				_syncTimer = 0.0;
				BroadcastState();
			}
			_prioritySyncTimer += delta;
			if (_prioritySyncTimer >= 0.03)
			{
				_prioritySyncTimer = 0.0;
				BroadcastPriorityState();
			}
		}
		else
		{
			InterpolatePositions(delta);
		}
	}

	public void BroadcastState()
	{
		if (base.Context == null || !base.Context.IsMultiplayerActive || _zombieContext == null)
		{
			return;
		}
		Array array = new Array();
		Array array2 = new Array();
		List<int> list = new List<int>();
		foreach (int key2 in _zombieContext.SyncCharacters.Keys)
		{
			TowerDefenseCharacter towerDefenseCharacter = _zombieContext.SyncCharacters[key2];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				list.Add(key2);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 != null && !towerDefenseCharacter2.isDestroy && towerDefenseCharacter2 is TowerDefenseZombie)
			{
				Vector2 logicalGlobalPosition = towerDefenseCharacter2.GetLogicalGlobalPosition();
				array.Add(key2);
				Dictionary dictionary = new Dictionary { ["i"] = key2 };
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2.config))
				{
					dictionary["n"] = towerDefenseCharacter2.config.name;
				}
				dictionary["x"] = Mathf.Snapped(logicalGlobalPosition.X, 1f);
				dictionary["y"] = Mathf.Snapped(logicalGlobalPosition.Y, 1f);
				dictionary["g"] = towerDefenseCharacter2.gridPos.Y;
				array2.Add(dictionary);
			}
		}
		foreach (int item in list)
		{
			_zombieContext.SyncCharacters.Remove(item);
			_zombieContext.RemoveZombieSyncState(item);
		}
		Array array3 = new Array();
		foreach (Variant item2 in array)
		{
			TowerDefenseCharacter towerDefenseCharacter3 = _zombieContext.SyncCharacters[item2.AsInt32()];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter3))
			{
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter4 = towerDefenseCharacter3;
			if (towerDefenseCharacter4 != null)
			{
				Dictionary dictionary2 = _zombieContext.ZombieLastSyncState.GetValueOrDefault(item2, new Dictionary()).AsGodotDictionary();
				bool num = GodotObject.IsInstanceValid(towerDefenseCharacter4.instance) && towerDefenseCharacter4.instance.die;
				bool flag = GodotObject.IsInstanceValid(towerDefenseCharacter4.instance) && towerDefenseCharacter4.instance.nearDie;
				if (num != dictionary2.GetValueOrDefault("d", false).AsBool() || flag != dictionary2.GetValueOrDefault("nd", false).AsBool())
				{
					array3.Add(item2);
				}
				else if (GodotObject.IsInstanceValid(towerDefenseCharacter4.sprite) && towerDefenseCharacter4.sprite.clip != dictionary2.GetValueOrDefault("c", "").AsString())
				{
					array3.Add(item2);
				}
				else if (Mathf.Abs((float)((GodotObject.IsInstanceValid(towerDefenseCharacter4.instance) ? towerDefenseCharacter4.instance.hitpoints : 0.0) - dictionary2.GetValueOrDefault("h", 0.0).AsDouble())) > 10f)
				{
					array3.Add(item2);
				}
			}
		}
		Array batchKeys = GetBatchKeys(array);
		Dictionary dictionary3 = new Dictionary();
		foreach (Variant item3 in array3)
		{
			dictionary3[item3] = true;
		}
		foreach (Variant item4 in batchKeys)
		{
			dictionary3[item4] = true;
		}
		Array array4 = new Array();
		foreach (Variant key3 in dictionary3.Keys)
		{
			if (!_zombieContext.SyncCharacters.ContainsKey(key3.AsInt32()))
			{
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter5 = _zombieContext.SyncCharacters[key3.AsInt32()];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter5))
			{
				TowerDefenseCharacter towerDefenseCharacter6 = towerDefenseCharacter5;
				if (towerDefenseCharacter6 != null && !towerDefenseCharacter6.isDestroy && towerDefenseCharacter6 is TowerDefenseZombie zombieSync)
				{
					Dictionary dictionary4 = BuildZombieDelta(key3, zombieSync, array3.Contains(key3));
					_zombieContext.ZombieLastSyncState[key3] = BuildLastZombieState(dictionary4, zombieSync);
					_zombiePriorityStates[key3.AsInt32()] = BuildZombiePriorityState(zombieSync);
					array4.Add(dictionary4);
				}
			}
		}
		_zombieRosterSyncCounter++;
		bool flag2 = _zombieRosterSyncCounter >= 10 || array.Count != _lastZombieRosterCount;
		if (flag2)
		{
			_zombieRosterSyncCounter = 0;
			_lastZombieRosterCount = array.Count;
		}
		if (array4.Count > 0 || flag2)
		{
			Dictionary dictionary5 = new Dictionary { ["z"] = array4 };
			if (flag2)
			{
				dictionary5["a"] = array2;
			}
			_zombieContext.SendZombieState(Json.Stringify(dictionary5));
		}
	}

	public void BroadcastPriorityState()
	{
		if (base.Context == null || !base.Context.IsMultiplayerActive || _zombieContext == null)
		{
			return;
		}
		Array array = new Array();
		List<int> list = new List<int>();
		foreach (int key2 in _zombieContext.SyncCharacters.Keys)
		{
			TowerDefenseCharacter towerDefenseCharacter = _zombieContext.SyncCharacters[key2];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				list.Add(key2);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 != null && !towerDefenseCharacter2.isDestroy && towerDefenseCharacter2 is TowerDefenseZombie zombieSync && NeedsPriorityZombieSync(key2, zombieSync))
			{
				array.Add(BuildZombieDelta(key2, zombieSync, isPriority: true));
			}
		}
		if (array.Count > 0)
		{
			_zombieContext.SendZombieState(Json.Stringify(new Dictionary { ["z"] = array }));
		}
		foreach (int item in list)
		{
			_zombieContext.SyncCharacters.Remove(item);
			_zombieContext.RemoveZombieSyncState(item);
			_zombiePriorityStates.Remove(item);
		}
	}

	public Dictionary BuildFullState()
	{
		Array array = new Array();
		Array array2 = new Array();
		if (_zombieContext == null)
		{
			return new Dictionary
			{
				["z"] = array,
				["a"] = array2
			};
		}
		foreach (KeyValuePair<int, TowerDefenseCharacter> syncCharacter in _zombieContext.SyncCharacters)
		{
			if (syncCharacter.Value is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie) && !towerDefenseZombie.isDestroy)
			{
				Vector2 logicalGlobalPosition = towerDefenseZombie.GetLogicalGlobalPosition();
				Dictionary dictionary = new Dictionary
				{
					["i"] = syncCharacter.Key,
					["n"] = (GodotObject.IsInstanceValid(towerDefenseZombie.config) ? towerDefenseZombie.config.name : ""),
					["x"] = Mathf.Snapped(logicalGlobalPosition.X, 0.1f),
					["y"] = Mathf.Snapped(logicalGlobalPosition.Y, 0.1f),
					["g"] = towerDefenseZombie.gridPos.Y
				};
				array2.Add(dictionary);
				array.Add(BuildZombieDelta(syncCharacter.Key, towerDefenseZombie, isPriority: true));
			}
		}
		return new Dictionary
		{
			["z"] = array,
			["a"] = array2
		};
	}

	private bool NeedsPriorityZombieSync(int syncIdVal, TowerDefenseZombie zombieSync)
	{
		ZombiePriorityState zombiePriorityState = BuildZombiePriorityState(zombieSync);
		if (!_zombiePriorityStates.TryGetValue(syncIdVal, out var value))
		{
			Dictionary dictionary = _zombieContext.ZombieLastSyncState.GetValueOrDefault(syncIdVal, new Dictionary()).AsGodotDictionary();
			if (dictionary.Count <= 0)
			{
				_zombiePriorityStates[syncIdVal] = zombiePriorityState;
				return false;
			}
			value = BuildZombiePriorityState(dictionary);
		}
		if (zombiePriorityState == value)
		{
			return false;
		}
		_zombiePriorityStates[syncIdVal] = zombiePriorityState;
		return true;
	}

	private Array GetBatchKeys(Array allZombieKeys)
	{
		if (allZombieKeys.Count <= 40)
		{
			return allZombieKeys;
		}
		if (_zombieSyncBatchIndex == 0 || _zombieSyncKeys.Count == 0)
		{
			_zombieSyncKeys = allZombieKeys.Duplicate();
			_zombieSyncKeys.Shuffle();
			_zombieSyncBatchIndex = 0;
		}
		int num = _zombieSyncBatchIndex * 40;
		int num2 = Mathf.Min(num + 40, _zombieSyncKeys.Count);
		if (num >= _zombieSyncKeys.Count)
		{
			_zombieSyncBatchIndex = 0;
			_zombieSyncKeys = allZombieKeys.Duplicate();
			_zombieSyncKeys.Shuffle();
			num = 0;
			num2 = Mathf.Min(40, _zombieSyncKeys.Count);
		}
		Array result = _zombieSyncKeys.Slice(num, num2 - 1);
		_zombieSyncBatchIndex++;
		if (_zombieSyncBatchIndex * 40 >= _zombieSyncKeys.Count)
		{
			_zombieSyncBatchIndex = 0;
		}
		return result;
	}

	private Dictionary BuildZombieDelta(Variant syncIdVal, TowerDefenseZombie zombieSync, bool isPriority)
	{
		Vector2 logicalGlobalPosition = zombieSync.GetLogicalGlobalPosition();
		float num = Mathf.Snapped(logicalGlobalPosition.X, 0.1f);
		float num2 = Mathf.Snapped(logicalGlobalPosition.Y, 0.1f);
		double num3 = (GodotObject.IsInstanceValid(zombieSync.instance) ? zombieSync.instance.hitpoints : 0.0);
		bool num4 = GodotObject.IsInstanceValid(zombieSync.instance) && zombieSync.instance.nearDie;
		bool flag = GodotObject.IsInstanceValid(zombieSync.instance) && zombieSync.instance.die;
		int y = zombieSync.gridPos.Y;
		double timeScale = zombieSync.timeScale;
		double walkSpeedScale = zombieSync.walkSpeedScale;
		string text = "";
		bool flag2 = true;
		double num5 = 0.0;
		int num6 = 0;
		if (GodotObject.IsInstanceValid(zombieSync.sprite))
		{
			text = zombieSync.sprite.clip;
			flag2 = zombieSync.sprite.loop;
			num5 = zombieSync.sprite.blendTime;
			num6 = zombieSync.sprite.frameIndex;
		}
		Dictionary dictionary = _zombieContext.ZombieLastSyncState.GetValueOrDefault(syncIdVal, new Dictionary()).AsGodotDictionary();
		Dictionary dictionary2 = new Dictionary { ["i"] = syncIdVal };
		float num7 = (float)dictionary.GetValueOrDefault("x", num).AsDouble();
		float num8 = (float)dictionary.GetValueOrDefault("y", num2).AsDouble();
		bool flag3 = dictionary.Count > 0 && new Vector2(num - num7, num2 - num8).LengthSquared() > 2304f;
		if (isPriority || flag3 || Mathf.Abs(num - num7) > 0.5f)
		{
			dictionary2["x"] = num;
		}
		if (isPriority || flag3 || Mathf.Abs(num2 - num8) > 0.5f)
		{
			dictionary2["y"] = num2;
		}
		if (flag3)
		{
			dictionary2["tp"] = true;
		}
		if (isPriority || y != dictionary.GetValueOrDefault("g", y).AsInt32())
		{
			dictionary2["g"] = y;
		}
		if (isPriority || Mathf.Abs((float)(num3 - dictionary.GetValueOrDefault("h", 0.0).AsDouble())) > 0.5f)
		{
			dictionary2["h"] = num3;
		}
		if (num4)
		{
			dictionary2["nd"] = true;
		}
		if (flag)
		{
			dictionary2["d"] = true;
		}
		if (isPriority || text != dictionary.GetValueOrDefault("c", "").AsString())
		{
			dictionary2["c"] = text;
			if (!flag2)
			{
				dictionary2["l"] = false;
			}
			if (num5 > 0.01)
			{
				dictionary2["b"] = Mathf.Snapped((float)num5, 0.01f);
			}
			dictionary2["fi"] = num6;
		}
		if (isPriority || Mathf.Abs((float)(timeScale - dictionary.GetValueOrDefault("ts", 1.0).AsDouble())) > 0.05f)
		{
			dictionary2["ts"] = Mathf.Snapped((float)timeScale, 0.01f);
		}
		if (isPriority || Mathf.Abs((float)(walkSpeedScale - dictionary.GetValueOrDefault("ws", 1.0).AsDouble())) > 0.05f)
		{
			dictionary2["ws"] = Mathf.Snapped((float)walkSpeedScale, 0.01f);
		}
		if (GodotObject.IsInstanceValid(zombieSync.instance) && GodotObject.IsInstanceValid(zombieSync.instance.damagePointData))
		{
			int damagePointIndex = zombieSync.instance.damagePointIndex;
			if (isPriority || damagePointIndex != dictionary.GetValueOrDefault("dpi", 0).AsInt32())
			{
				dictionary2["dpi"] = damagePointIndex;
			}
		}
		NetworkCharacterStateSnapshots.AppendArmorSnapshot(dictionary2, zombieSync, dictionary, isPriority);
		AppendBuffDelta(dictionary2, zombieSync, dictionary, isPriority);
		AppendSpecialZombieDelta(dictionary2, zombieSync, dictionary, isPriority);
		return dictionary2;
	}

	private static Dictionary BuildLastZombieState(Dictionary zombieData, TowerDefenseZombie zombieSync)
	{
		Vector2 logicalGlobalPosition = zombieSync.GetLogicalGlobalPosition();
		return new Dictionary
		{
			["x"] = Mathf.Snapped(logicalGlobalPosition.X, 0.1f),
			["y"] = Mathf.Snapped(logicalGlobalPosition.Y, 0.1f),
			["h"] = (GodotObject.IsInstanceValid(zombieSync.instance) ? zombieSync.instance.hitpoints : 0.0),
			["nd"] = GodotObject.IsInstanceValid(zombieSync.instance) && zombieSync.instance.nearDie,
			["d"] = GodotObject.IsInstanceValid(zombieSync.instance) && zombieSync.instance.die,
			["g"] = zombieSync.gridPos.Y,
			["ts"] = zombieSync.timeScale,
			["ws"] = zombieSync.walkSpeedScale,
			["c"] = (GodotObject.IsInstanceValid(zombieSync.sprite) ? zombieSync.sprite.clip : ""),
			["l"] = !GodotObject.IsInstanceValid(zombieSync.sprite) || zombieSync.sprite.loop,
			["b"] = (GodotObject.IsInstanceValid(zombieSync.sprite) ? zombieSync.sprite.blendTime : 0.0),
			["dpi"] = zombieData.GetValueOrDefault("dpi", 0),
			["ar"] = NetworkCharacterStateSnapshots.BuildArmorSnapshot(zombieSync),
			["bf"] = zombieData.GetValueOrDefault("bf", new Array()),
			["sp"] = BuildSpecialZombieState(zombieSync)
		};
	}

	private static void AppendSpecialZombieDelta(Dictionary zombieData, TowerDefenseZombie zombieSync, Dictionary last, bool isPriority)
	{
		Dictionary dictionary = BuildSpecialZombieState(zombieSync);
		if (dictionary.Count > 0)
		{
			Dictionary b = last.GetValueOrDefault("sp", new Dictionary()).AsGodotDictionary();
			if (isPriority || !NetworkVariantComparer.DictionaryApproxEquals(dictionary, b))
			{
				zombieData["sp"] = dictionary;
			}
		}
	}

	private static Dictionary BuildSpecialZombieState(TowerDefenseZombie zombieSync)
	{
		return zombieSync.ExportNetworkSpecialState();
	}

	private static ZombiePriorityState BuildZombiePriorityState(TowerDefenseZombie zombieSync)
	{
		string clip = (GodotObject.IsInstanceValid(zombieSync.sprite) ? zombieSync.sprite.clip : "");
		bool loop = !GodotObject.IsInstanceValid(zombieSync.sprite) || zombieSync.sprite.loop;
		bool nearDie = GodotObject.IsInstanceValid(zombieSync.instance) && zombieSync.instance.nearDie;
		bool die = GodotObject.IsInstanceValid(zombieSync.instance) && zombieSync.instance.die;
		return new ZombiePriorityState(clip, loop, nearDie, die, QuantizePriorityScale(zombieSync.timeScale), QuantizePriorityScale(zombieSync.walkSpeedScale), zombieSync.gridPos.Y, zombieSync.GetNetworkSpecialStateRevision());
	}

	private static ZombiePriorityState BuildZombiePriorityState(Dictionary lastState)
	{
		return new ZombiePriorityState(lastState.GetValueOrDefault("c", "").AsString(), lastState.GetValueOrDefault("l", true).AsBool(), lastState.GetValueOrDefault("nd", false).AsBool(), lastState.GetValueOrDefault("d", false).AsBool(), SpecialState: lastState.GetValueOrDefault("sp", new Dictionary()).AsGodotDictionary().GetValueOrDefault("rev", 0)
			.AsInt32(), TimeScale: QuantizePriorityScale(lastState.GetValueOrDefault("ts", 1.0).AsDouble()), WalkSpeedScale: QuantizePriorityScale(lastState.GetValueOrDefault("ws", 1.0).AsDouble()), GridY: lastState.GetValueOrDefault("g", 0).AsInt32());
	}

	private static int QuantizePriorityScale(double value)
	{
		return Mathf.RoundToInt((float)value * 100f);
	}

	private static void AppendBuffDelta(Dictionary zombieData, TowerDefenseZombie zombieSync, Dictionary last, bool isPriority)
	{
		BuffComponent buff = zombieSync.buff;
		if (buff == null || buff.IsReleased)
		{
			return;
		}
		Array array = new Array();
		foreach (string key2 in zombieSync.buff.buffDictionary.Keys)
		{
			TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = zombieSync.buff.buffDictionary[key2];
			Dictionary dictionary = new Dictionary { ["k"] = key2 };
			if (GodotObject.IsInstanceValid(towerDefenseCharacterBuffConfig))
			{
				Variant variant = towerDefenseCharacterBuffConfig.Get("time");
				if (variant.VariantType != Variant.Type.Nil)
				{
					dictionary["t"] = Mathf.Snapped((float)variant.AsDouble(), 0.01f);
				}
				Variant variant2 = towerDefenseCharacterBuffConfig.Get("currentTime");
				if (variant2.VariantType != Variant.Type.Nil)
				{
					dictionary["ct"] = Mathf.Snapped((float)variant2.AsDouble(), 0.01f);
				}
			}
			array.Add(dictionary);
		}
		Array array2 = last.GetValueOrDefault("bf", new Array()).AsGodotArray();
		if (isPriority || array.Count > 0 || array2.Count > 0)
		{
			zombieData["bf"] = array;
		}
	}

	public void ApplyState(Variant syncDataVariant)
	{
		if (_zombieContext == null)
		{
			return;
		}
		Variant variant = syncDataVariant;
		Dictionary dictionary = new Dictionary();
		Dictionary dictionary2 = new Dictionary();
		bool hasRoster = false;
		if (syncDataVariant.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary3 = syncDataVariant.AsGodotDictionary();
			variant = dictionary3.GetValueOrDefault("z", new Array());
			hasRoster = dictionary3.ContainsKey("a");
			foreach (Variant item in dictionary3.GetValueOrDefault("a", new Array()).AsGodotArray())
			{
				if (item.VariantType == Variant.Type.Dictionary)
				{
					int num = item.AsGodotDictionary().GetValueOrDefault("i", -1).AsInt32();
					if (num >= 0)
					{
						dictionary[num] = true;
						dictionary2[num] = item;
					}
				}
				else
				{
					dictionary[item] = true;
				}
			}
		}
		if (variant.VariantType != Variant.Type.Array)
		{
			return;
		}
		Array array = variant.AsGodotArray();
		Dictionary dictionary4 = new Dictionary();
		foreach (Variant item2 in array)
		{
			if (item2.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary5 = item2.AsGodotDictionary();
			int num2 = dictionary5.GetValueOrDefault("i", dictionary5.GetValueOrDefault("sync_id", -1)).AsInt32();
			if (num2 < 0 || !_zombieContext.SyncCharacters.ContainsKey(num2))
			{
				continue;
			}
			dictionary4[num2] = true;
			TowerDefenseCharacter towerDefenseCharacter = _zombieContext.SyncCharacters[num2];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
				if (towerDefenseCharacter2 != null && !towerDefenseCharacter2.isDestroy && towerDefenseCharacter2 is TowerDefenseZombie zombie)
				{
					ApplyZombieDelta(num2, zombie, dictionary5);
				}
			}
		}
		HandleRosterCorrection(hasRoster, dictionary, dictionary2, dictionary4);
	}

	private void ApplyZombieDelta(int syncIdVal, TowerDefenseZombie zombie, Dictionary zombieData)
	{
		if (zombieData.ContainsKey("x") || zombieData.ContainsKey("y"))
		{
			Vector2 logicalGlobalPosition = zombie.GetLogicalGlobalPosition();
			float x = (float)zombieData.GetValueOrDefault("x", logicalGlobalPosition.X).AsDouble();
			float y = (float)zombieData.GetValueOrDefault("y", logicalGlobalPosition.Y).AsDouble();
			Vector2 vector = new Vector2(x, y);
			if (zombieData.GetValueOrDefault("tp", false).AsBool())
			{
				zombie.SetLogicalGlobalPosition(vector);
				_zombieContext.ZombieSyncVelocities.Remove(syncIdVal);
			}
			else if (_zombieContext.ZombieTargetPositions.ContainsKey(syncIdVal))
			{
				Vector2 vector2 = _zombieContext.ZombieTargetPositions[syncIdVal];
				double value;
				double num = (_zombieContext.ZombieLastSyncTime.TryGetValue(syncIdVal, out value) ? value : _zombieContext.GameTime);
				double num2 = _zombieContext.GameTime - num;
				if (num2 > 0.01)
				{
					Vector2 value2 = (vector - vector2) / (float)num2;
					if (value2.IsFinite() && value2.LengthSquared() <= 1440000f)
					{
						_zombieContext.ZombieSyncVelocities[syncIdVal] = value2;
					}
					else
					{
						_zombieContext.ZombieSyncVelocities.Remove(syncIdVal);
					}
				}
			}
			_zombieContext.ZombieTargetPositions[syncIdVal] = vector;
			_zombieContext.ZombieLastSyncTime[syncIdVal] = _zombieContext.GameTime;
		}
		if (zombieData.ContainsKey("g"))
		{
			zombie.gridPos = new Vector2I(zombie.gridPos.X, zombieData["g"].AsInt32());
		}
		if (GodotObject.IsInstanceValid(zombie.instance))
		{
			if (zombieData.ContainsKey("h"))
			{
				zombie.instance.hitpoints = zombieData["h"].AsDouble();
			}
			if (zombieData.GetValueOrDefault("nd", false).AsBool() && !zombie.instance.nearDie)
			{
				zombie.instance.nearDie = true;
				zombie.instance.EmitHitpointsNearDie();
			}
			bool num3 = zombieData.GetValueOrDefault("d", false).AsBool();
			if (num3 && !zombie.instance.die)
			{
				zombie.instance.die = true;
				zombie.instance.EmitHitpointsEmpty();
			}
			if (!num3 && zombie.instance.hitpoints <= 0.0 && !zombie.isDestroy)
			{
				zombie.instance.hitpoints = 1.0;
			}
		}
		string text = zombieData.GetValueOrDefault("c", "").AsString();
		if (text != "" && GodotObject.IsInstanceValid(zombie.sprite) && zombie.sprite.clip != text)
		{
			bool loopAnim = zombieData.GetValueOrDefault("l", true).AsBool();
			double num4 = zombieData.GetValueOrDefault("b", 0.0).AsDouble();
			zombie.OnRemoteNetworkAnimationStart(text);
			zombie.SyncAnimation(text, loopAnim, (float)num4);
			if (zombieData.ContainsKey("fi"))
			{
				zombie.sprite.frameIndex = zombieData["fi"].AsInt32();
				zombie.sprite.elapsedTimer = 0.0;
			}
		}
		if (zombieData.ContainsKey("ts"))
		{
			zombie.timeScale = zombieData["ts"].AsDouble();
		}
		if (zombieData.ContainsKey("ws"))
		{
			zombie.walkSpeedScale = zombieData["ws"].AsDouble();
		}
		ApplyDamagePointState(zombie, zombieData);
		if (zombieData.ContainsKey("ar") && GodotObject.IsInstanceValid(zombie.instance))
		{
			NetworkCharacterStateSnapshots.ApplyArmorSnapshot(zombie, zombieData["ar"].AsGodotArray(), zombie is TowerDefenseZombieBossDave);
		}
		if (zombieData.ContainsKey("bf"))
		{
			BuffComponent buff = zombie.buff;
			if (buff != null && !buff.IsReleased)
			{
				ApplyZombieBuffState(zombie, zombieData["bf"].AsGodotArray());
			}
		}
		if (zombieData.ContainsKey("sp"))
		{
			zombie.ImportNetworkSpecialState(zombieData["sp"].AsGodotDictionary());
		}
	}

	private static void ApplyDamagePointState(TowerDefenseZombie zombie, Dictionary zombieData)
	{
		if (!zombieData.ContainsKey("dpi") || !GodotObject.IsInstanceValid(zombie.instance) || !GodotObject.IsInstanceValid(zombie.instance.damagePointData))
		{
			return;
		}
		int num = zombieData["dpi"].AsInt32();
		if (num <= zombie.instance.damagePointIndex)
		{
			return;
		}
		for (int i = zombie.instance.damagePointIndex; i < num; i++)
		{
			if (i >= zombie.instance.damagePoints.Count)
			{
				continue;
			}
			string damagePointName = (string)zombie.instance.damagePoints[i]["Name"];
			zombie.instance.damagePointData.SetDamagePointFliters(zombie.sprite, damagePointName);
			if (GodotObject.IsInstanceValid(zombie.config.customData))
			{
				foreach (string item in zombie.currentCustom)
				{
					zombie.config.customData.SetDamagePoint(zombie.sprite, item, i);
				}
			}
			zombie.DamagePointReach(damagePointName);
		}
		zombie.instance.damagePointIndex = num;
	}

	private static void ApplyZombieBuffState(TowerDefenseZombie zombie, Array buffsData)
	{
		zombie.buff.ApplyLegacyNetworkSnapshot(buffsData);
	}

	private void HandleRosterCorrection(bool hasRoster, Dictionary hostAllZombieIds, Dictionary hostZombieInfos, Dictionary syncedZombieIds)
	{
		if ((base.Context != null && base.Context.IsHost) || !hasRoster)
		{
			return;
		}
		foreach (Variant key in hostZombieInfos.Keys)
		{
			int num = key.AsInt32();
			if (!_zombieContext.SyncCharacters.ContainsKey(num))
			{
				_zombieContext.CreateMissingZombieFromRoster(num, hostZombieInfos[key].AsGodotDictionary());
			}
		}
		foreach (int item in new List<int>(_zombieContext.SyncCharacters.Keys))
		{
			if (!_zombieContext.SyncCharacters.ContainsKey(item))
			{
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter = _zombieContext.SyncCharacters[item];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				_zombieContext.ZombieSyncMissCount.Remove(item);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2.isDestroy || !(towerDefenseCharacter2 is TowerDefenseZombie))
			{
				_zombieContext.ZombieSyncMissCount.Remove(item);
				continue;
			}
			if (hostAllZombieIds.ContainsKey(item) || syncedZombieIds.ContainsKey(item))
			{
				_zombieContext.ZombieSyncMissCount.Remove(item);
				continue;
			}
			if (!_zombieContext.ZombieSyncMissCount.ContainsKey(item))
			{
				_zombieContext.ZombieSyncMissCount[item] = 0;
			}
			_zombieContext.ZombieSyncMissCount[item] = _zombieContext.ZombieSyncMissCount[item] + 1;
			if (_zombieContext.ZombieSyncMissCount[item] > 10)
			{
				_zombieContext.DestroyRemoteZombie(item, towerDefenseCharacter2);
			}
		}
	}

	public void InterpolatePositions(double delta)
	{
		if (_zombieContext == null)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (int key in _zombieContext.ZombieTargetPositions.Keys)
		{
			if (!_zombieContext.SyncCharacters.ContainsKey(key))
			{
				list.Add(key);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter = _zombieContext.SyncCharacters[key];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				list.Add(key);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 == null || towerDefenseCharacter2.isDestroy)
			{
				list.Add(key);
			}
			else
			{
				if (!(towerDefenseCharacter2 is TowerDefenseZombie zombie) || towerDefenseCharacter2.IsNetworkSpecialMovementActive())
				{
					continue;
				}
				Vector2 vector = _zombieContext.ZombieTargetPositions[key];
				Vector2 vector2 = vector;
				if (CanExtrapolateZombiePosition(zombie) && _zombieContext.ZombieSyncVelocities.ContainsKey(key) && _zombieContext.ZombieLastSyncTime.ContainsKey(key))
				{
					Vector2 vector3 = _zombieContext.ZombieSyncVelocities[key];
					double num = _zombieContext.GameTime - _zombieContext.ZombieLastSyncTime[key];
					if (num > 0.0 && num < 1.5)
					{
						vector2 = vector + vector3 * (float)num;
					}
				}
				Vector2 logicalGlobalPosition = towerDefenseCharacter2.GetLogicalGlobalPosition();
				float num2 = (vector2 - logicalGlobalPosition).LengthSquared();
				if (num2 > 14400f)
				{
					towerDefenseCharacter2.SetLogicalGlobalPosition(vector2);
				}
				else if (num2 > 0.0625f)
				{
					float weight = 1f - Mathf.Exp(-12f * Mathf.Max(0f, (float)delta));
					towerDefenseCharacter2.SetLogicalGlobalPosition(logicalGlobalPosition.Lerp(vector2, weight));
				}
			}
		}
		foreach (int item in list)
		{
			_zombieContext.ZombieTargetPositions.Remove(item);
			_zombieContext.ZombieSyncVelocities.Remove(item);
			_zombieContext.ZombieLastSyncTime.Remove(item);
		}
	}

	private static bool CanExtrapolateZombiePosition(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie) || zombie.die || zombie.nearDie || zombie.timeScale <= 0.0 || !GodotObject.IsInstanceValid(zombie.sprite) || zombie.sprite.pause)
		{
			return false;
		}
		string clip = zombie.sprite.clip;
		if (!MatchesConfiguredClip(clip, zombie.attackAnimeClip) && !MatchesConfiguredClip(clip, zombie.attackWaterAnimeClip) && !MatchesConfiguredClip(clip, zombie.dieAnimeClip))
		{
			return !MatchesConfiguredClip(clip, zombie.dieWaterAnimeClip);
		}
		return false;
	}

	private static bool MatchesConfiguredClip(string currentClip, string configuredClips)
	{
		if (string.IsNullOrEmpty(currentClip) || string.IsNullOrEmpty(configuredClips))
		{
			return false;
		}
		int num = 0;
		while (num <= configuredClips.Length)
		{
			int num2 = configuredClips.IndexOf('&', num);
			int num3 = ((num2 >= 0) ? (num2 - num) : (configuredClips.Length - num));
			if (num3 == currentClip.Length && string.CompareOrdinal(configuredClips, num, currentClip, 0, num3) == 0)
			{
				return true;
			}
			if (num2 < 0)
			{
				break;
			}
			num = num2 + 1;
		}
		return false;
	}
}
