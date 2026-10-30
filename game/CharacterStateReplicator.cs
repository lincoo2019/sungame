using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class CharacterStateReplicator : NetworkReplicatorBase
{
	private sealed class PendingComponentOperationDestroy
	{
		public bool IsExplode;

		public bool IsSmash;

		public double Elapsed;

		public TowerDefenseCharacter Target;

		public bool HasBoundTarget;
	}

	private const double CharacterSyncInterval = 0.08;

	private const double PendingDestroyCleanupInterval = 10.0;

	private const double PendingDestroyRetentionSeconds = 30.0;

	private const double PendingComponentOperationDestroyTimeout = 5.1;

	private readonly ICharacterStateNetworkContext _characterContext;

	private readonly System.Collections.Generic.Dictionary<int, long> _outgoingOwnerSequences = new System.Collections.Generic.Dictionary<int, long>();

	private readonly System.Collections.Generic.Dictionary<int, long> _lastAppliedOwnerSequences = new System.Collections.Generic.Dictionary<int, long>();

	private readonly System.Collections.Generic.Dictionary<int, Dictionary> _pendingOwnerStates = new System.Collections.Generic.Dictionary<int, Dictionary>();

	private readonly System.Collections.Generic.Dictionary<int, PendingComponentOperationDestroy> _pendingComponentOperationDestroys = new System.Collections.Generic.Dictionary<int, PendingComponentOperationDestroy>();

	private CharacterComponentOperationDestroyBarrier _componentOperationDestroyBarrier;

	private double _syncTimer;

	private double _pendingDestroyCleanupTimer;

	public CharacterStateReplicator()
	{
	}

	public CharacterStateReplicator(ICharacterStateNetworkContext characterContext)
	{
		_characterContext = characterContext;
	}

	public override void Initialize(BattleNetworkSession session)
	{
		base.Initialize(session);
		_componentOperationDestroyBarrier = CharacterComponentOperationDestroyBarrier.ForSession(session);
	}

	private long NextOutgoingOwnerSequence(int syncId)
	{
		long num = _outgoingOwnerSequences.GetValueOrDefault(syncId, 0L);
		if (num < 9223372036854775807L)
		{
			num++;
		}
		_outgoingOwnerSequences[syncId] = num;
		return num;
	}

	private bool TryAcceptOwnerSequence(int syncId, Dictionary data)
	{
		long num = data.GetValueOrDefault("seq", 0).AsInt64();
		if (num <= 0)
		{
			return !_lastAppliedOwnerSequences.ContainsKey(syncId);
		}
		if (_lastAppliedOwnerSequences.TryGetValue(syncId, out var value) && num <= value)
		{
			return false;
		}
		_lastAppliedOwnerSequences[syncId] = num;
		return true;
	}

	private void QueuePendingOwnerState(int syncId, Dictionary data)
	{
		long num = data.GetValueOrDefault("seq", 0).AsInt64();
		if (_pendingOwnerStates.TryGetValue(syncId, out var value))
		{
			long num2 = value.GetValueOrDefault("seq", 0).AsInt64();
			if (num > 0 && num2 > num)
			{
				return;
			}
			Dictionary dictionary = value.Duplicate(deep: true);
			foreach (Variant key in data.Keys)
			{
				if (key.AsString() != "components" || data[key].VariantType != Variant.Type.Dictionary)
				{
					dictionary[key] = data[key];
					continue;
				}
				Dictionary dictionary2 = dictionary.GetValueOrDefault("components", new Dictionary()).AsGodotDictionary();
				Dictionary dictionary3 = data[key].AsGodotDictionary();
				foreach (Variant key2 in dictionary3.Keys)
				{
					Dictionary dictionary4 = dictionary3[key2].AsGodotDictionary();
					if (!dictionary4.ContainsKey("sm") && dictionary2.ContainsKey(key2))
					{
						Dictionary dictionary5 = dictionary2[key2].AsGodotDictionary();
						if (dictionary5.ContainsKey("sm"))
						{
							dictionary4["sm"] = dictionary5["sm"];
						}
					}
					dictionary2[key2] = dictionary4;
				}
				dictionary["components"] = dictionary2;
			}
			_pendingOwnerStates[syncId] = dictionary;
		}
		else
		{
			_pendingOwnerStates[syncId] = data.Duplicate(deep: true);
		}
	}

	private void ReplayPendingOwnerState(int syncId)
	{
		if (_pendingOwnerStates.Remove(syncId, out var value))
		{
			Godot.Collections.Array array = new Godot.Collections.Array { value };
			ApplyState(array);
		}
	}

	private void RemoveOwnerSequenceState(int syncId)
	{
		_pendingOwnerStates.Remove(syncId);
	}

	public override void Process(double delta)
	{
		if (base.Context == null || !base.Context.IsMultiplayerActive || _characterContext == null)
		{
			return;
		}
		ProcessPendingComponentOperationDestroys(delta);
		if (base.Context.IsHost)
		{
			_syncTimer += delta;
			if (_syncTimer >= 0.08)
			{
				_syncTimer = 0.0;
				BroadcastState();
			}
		}
		_pendingDestroyCleanupTimer += delta;
		if (_pendingDestroyCleanupTimer >= 10.0)
		{
			_pendingDestroyCleanupTimer = 0.0;
			CleanupPendingDestroys();
		}
	}

	public void BroadcastState()
	{
		if (base.Context == null || !base.Context.IsMultiplayerActive || _characterContext == null)
		{
			return;
		}
		Godot.Collections.Array array = new Godot.Collections.Array();
		List<int> list = new List<int>();
		foreach (int key12 in _characterContext.SyncCharacters.Keys)
		{
			TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[key12];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				list.Add(key12);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 == null || towerDefenseCharacter2.isDestroy)
			{
				continue;
			}
			bool num = towerDefenseCharacter2 is TowerDefenseZombie;
			Dictionary dictionary = new Dictionary { ["sync_id"] = key12 };
			bool flag = false;
			string text = "";
			bool flag2 = true;
			double num2 = 0.0;
			int num3 = 0;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter2.sprite))
			{
				text = towerDefenseCharacter2.sprite.clip;
				flag2 = towerDefenseCharacter2.sprite.loop;
				num2 = towerDefenseCharacter2.sprite.blendTime;
				num3 = towerDefenseCharacter2.sprite.frameIndex;
			}
			Dictionary dictionary2 = _characterContext.CharacterLastSyncState.GetValueOrDefault(key12, new Dictionary()).AsGodotDictionary();
			Dictionary dictionary3 = ((!num) ? towerDefenseCharacter2.ExportNetworkSpecialState() : new Dictionary());
			Dictionary dictionary4 = dictionary2.GetValueOrDefault("sp", new Dictionary()).AsGodotDictionary();
			if ((dictionary3.Count > 0 || dictionary4.Count > 0) && !NetworkVariantComparer.DictionaryApproxEquals(dictionary3, dictionary4))
			{
				dictionary["sp"] = dictionary3;
				flag = true;
			}
			IStateMachineController stateMachine = towerDefenseCharacter2.StateMachine;
			int num4;
			long num5;
			if (stateMachine == null)
			{
				num4 = 0;
			}
			else
			{
				num4 = (stateMachine.IsInitialized ? 1 : 0);
				if (num4 != 0)
				{
					num5 = towerDefenseCharacter2.StateMachine.SnapshotRevision;
					goto IL_01db;
				}
			}
			num5 = -1L;
			goto IL_01db;
			IL_01db:
			long num6 = num5;
			long num7 = ((num4 != 0 && towerDefenseCharacter2.StateMachine.HasPendingTransition) ? NetworkCharacterStateSnapshots.GetStateMachineCheckpoint() : (-1));
			long num8 = dictionary2.GetValueOrDefault("smr", -1).AsInt64();
			long num9 = dictionary2.GetValueOrDefault("smc", -1).AsInt64();
			if (num4 != 0 && (num6 != num8 || (num7 >= 0 && num7 != num9)))
			{
				Dictionary dictionary5 = towerDefenseCharacter2.CaptureMainStateMachineSnapshotData();
				if (dictionary5.Count > 0)
				{
					dictionary["sm"] = dictionary5;
					flag = true;
				}
			}
			if (!num && (text != dictionary2.GetValueOrDefault("clip", "").AsString() || flag2 != dictionary2.GetValueOrDefault("loop", true).AsBool() || Mathf.Abs((float)(num2 - dictionary2.GetValueOrDefault("blendTime", 0.0).AsDouble())) > 0.01f))
			{
				dictionary["clip"] = text;
				if (!flag2)
				{
					dictionary["loop"] = false;
				}
				if (num2 > 0.01)
				{
					dictionary["blendTime"] = Mathf.Snapped((float)num2, 0.01f);
				}
				dictionary["frame"] = num3;
				flag = true;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter2.componentManager))
			{
				Dictionary dictionary6 = NetworkCharacterStateSnapshots.BuildComponentDelta(towerDefenseCharacter2, _characterContext.ComponentLastSyncState, key12, force: false);
				if (dictionary6.Count > 0)
				{
					dictionary["components"] = dictionary6;
					flag = true;
				}
			}
			if (!num && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance) && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance.damagePointData))
			{
				int damagePointIndex = towerDefenseCharacter2.instance.damagePointIndex;
				int num10 = dictionary2.GetValueOrDefault("dpi", 0).AsInt32();
				if (damagePointIndex != num10)
				{
					dictionary["dpi"] = damagePointIndex;
					flag = true;
				}
			}
			if (!num && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance))
			{
				double hitpoints = towerDefenseCharacter2.instance.hitpoints;
				double num11 = dictionary2.GetValueOrDefault("hp", hitpoints).AsDouble();
				if (Mathf.Abs((float)(hitpoints - num11)) > 1f)
				{
					dictionary["hp"] = Mathf.Snapped((float)hitpoints, 0.1f);
					flag = true;
				}
			}
			if (!num && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance) && NetworkCharacterStateSnapshots.AppendArmorSnapshot(dictionary, towerDefenseCharacter2, dictionary2, force: false))
			{
				flag = true;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter2.componentManager))
			{
				BowlingComponent runtime = towerDefenseCharacter2.componentManager.GetRuntime<BowlingComponent>();
				if (runtime != null && !runtime.IsReleased && runtime.isRoll)
				{
					Vector2 logicalGlobalPosition = towerDefenseCharacter2.GetLogicalGlobalPosition();
					dictionary["px"] = Mathf.Snapped(logicalGlobalPosition.X, 1f);
					dictionary["py"] = Mathf.Snapped(logicalGlobalPosition.Y, 1f);
					flag = true;
				}
			}
			if (!num)
			{
				BuffComponent buff = towerDefenseCharacter2.buff;
				if (buff != null && !buff.IsReleased)
				{
					Godot.Collections.Array array2 = new Godot.Collections.Array();
					foreach (string key13 in towerDefenseCharacter2.buff.buffDictionary.Keys)
					{
						TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = towerDefenseCharacter2.buff.buffDictionary[key13];
						Dictionary dictionary7 = new Dictionary { ["k"] = key13 };
						if (GodotObject.IsInstanceValid(towerDefenseCharacterBuffConfig))
						{
							Variant variant = towerDefenseCharacterBuffConfig.Get("time");
							if (variant.VariantType != Variant.Type.Nil)
							{
								dictionary7["t"] = Mathf.Snapped((float)variant.AsDouble(), 0.01f);
							}
							Variant variant2 = towerDefenseCharacterBuffConfig.Get("currentTime");
							if (variant2.VariantType != Variant.Type.Nil)
							{
								dictionary7["ct"] = Mathf.Snapped((float)variant2.AsDouble(), 0.01f);
							}
						}
						array2.Add(dictionary7);
					}
					Godot.Collections.Array array3 = dictionary2.GetValueOrDefault("bf", new Godot.Collections.Array()).AsGodotArray();
					if (array2.Count > 0 || array3.Count > 0)
					{
						dictionary["bf"] = array2;
						flag = true;
					}
				}
			}
			_characterContext.CharacterLastSyncState[key12] = new Dictionary
			{
				["clip"] = text,
				["loop"] = flag2,
				["blendTime"] = num2,
				["frame"] = num3,
				["dpi"] = dictionary.GetValueOrDefault("dpi", 0),
				["hp"] = dictionary.GetValueOrDefault("hp", GodotObject.IsInstanceValid(towerDefenseCharacter2.instance) ? towerDefenseCharacter2.instance.hitpoints : 0.0),
				["ar"] = NetworkCharacterStateSnapshots.BuildArmorSnapshot(towerDefenseCharacter2),
				["bf"] = dictionary.GetValueOrDefault("bf", new Godot.Collections.Array()),
				["smr"] = num6,
				["smc"] = num7,
				["sp"] = dictionary3
			};
			if (flag)
			{
				dictionary["seq"] = NextOutgoingOwnerSequence(key12);
				array.Add(dictionary);
			}
		}
		if (array.Count > 0)
		{
			_characterContext.SendCharacterState(Json.Stringify(array));
		}
		foreach (int item in list)
		{
			_characterContext.RemoveCharacterSyncState(item);
			RemoveOwnerSequenceState(item);
		}
	}

	public void ApplyState(Variant charactersDataVariant)
	{
		if (_characterContext == null || charactersDataVariant.VariantType != Variant.Type.Array)
		{
			return;
		}
		foreach (Variant item in charactersDataVariant.AsGodotArray())
		{
			if (item.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary = item.AsGodotDictionary();
			int num = dictionary.GetValueOrDefault("sync_id", -1).AsInt32();
			if (num < 0 || !_characterContext.SyncCharacters.ContainsKey(num))
			{
				if (num >= 0)
				{
					QueuePendingOwnerState(num, dictionary);
				}
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[num];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 == null || towerDefenseCharacter2.isDestroy || !TryAcceptOwnerSequence(num, dictionary))
			{
				continue;
			}
			bool flag = towerDefenseCharacter2 is TowerDefenseZombie;
			if (dictionary.ContainsKey("sm"))
			{
				towerDefenseCharacter2.RestoreMainStateMachineSnapshotData(dictionary["sm"].AsGodotDictionary(), remote: true, suppressEntryEffects: true);
			}
			string text = dictionary.GetValueOrDefault("clip", "").AsString();
			if (!flag && text != "" && GodotObject.IsInstanceValid(towerDefenseCharacter2.sprite))
			{
				if (towerDefenseCharacter2.sprite.clip != text)
				{
					bool loopAnim = dictionary.GetValueOrDefault("loop", true).AsBool();
					double num2 = dictionary.GetValueOrDefault("blendTime", 0.0).AsDouble();
					towerDefenseCharacter2.SyncAnimation(text, loopAnim, (float)num2);
				}
				if (dictionary.ContainsKey("frame"))
				{
					int num3 = dictionary["frame"].AsInt32();
					if (towerDefenseCharacter2.sprite.frameIndex != num3)
					{
						towerDefenseCharacter2.sprite.frameIndex = num3;
					}
				}
			}
			if (!flag && dictionary.ContainsKey("px") && dictionary.ContainsKey("py"))
			{
				towerDefenseCharacter2.SetLogicalGlobalPosition(new Vector2((float)dictionary["px"].AsDouble(), (float)dictionary["py"].AsDouble()));
			}
			if (!flag && dictionary.ContainsKey("hp") && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance))
			{
				double num4 = dictionary["hp"].AsDouble();
				towerDefenseCharacter2.instance.hitpoints = num4;
				if (num4 <= towerDefenseCharacter2.instance.hitpointsNearDeath && !towerDefenseCharacter2.instance.nearDie)
				{
					towerDefenseCharacter2.instance.nearDie = true;
					towerDefenseCharacter2.nearDie = true;
				}
			}
			if (dictionary.ContainsKey("components") && GodotObject.IsInstanceValid(towerDefenseCharacter2.componentManager))
			{
				NetworkCharacterStateSnapshots.ApplyComponents(towerDefenseCharacter2, dictionary["components"].AsGodotDictionary());
			}
			if (!flag && dictionary.ContainsKey("dpi") && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance) && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance.damagePointData))
			{
				int num5 = dictionary["dpi"].AsInt32();
				if (num5 > towerDefenseCharacter2.instance.damagePointIndex)
				{
					for (int i = towerDefenseCharacter2.instance.damagePointIndex; i < num5; i++)
					{
						if (i >= towerDefenseCharacter2.instance.damagePoints.Count)
						{
							continue;
						}
						string damagePointName = (string)towerDefenseCharacter2.instance.damagePoints[i]["Name"];
						towerDefenseCharacter2.instance.damagePointData.SetDamagePointFliters(towerDefenseCharacter2.sprite, damagePointName);
						if (GodotObject.IsInstanceValid(towerDefenseCharacter2.config.customData))
						{
							foreach (string item2 in towerDefenseCharacter2.currentCustom)
							{
								towerDefenseCharacter2.config.customData.SetDamagePoint(towerDefenseCharacter2.sprite, item2, i);
							}
						}
						towerDefenseCharacter2.DamagePointReach(damagePointName);
					}
					towerDefenseCharacter2.instance.damagePointIndex = num5;
				}
			}
			if (!flag && dictionary.ContainsKey("ar") && GodotObject.IsInstanceValid(towerDefenseCharacter2.instance))
			{
				NetworkCharacterStateSnapshots.ApplyArmorSnapshot(towerDefenseCharacter2, dictionary["ar"].AsGodotArray());
			}
			if (!flag && dictionary.ContainsKey("bf"))
			{
				BuffComponent buff = towerDefenseCharacter2.buff;
				if (buff != null && !buff.IsReleased)
				{
					ApplyBuffState(towerDefenseCharacter2, dictionary["bf"].AsGodotArray());
				}
			}
			if (!flag && dictionary.ContainsKey("sp"))
			{
				towerDefenseCharacter2.ImportNetworkSpecialState(dictionary["sp"].AsGodotDictionary());
			}
		}
	}

	public Godot.Collections.Array BuildFullState()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		if (_characterContext == null)
		{
			return array;
		}
		foreach (KeyValuePair<int, TowerDefenseCharacter> syncCharacter in _characterContext.SyncCharacters)
		{
			TowerDefenseCharacter value = syncCharacter.Value;
			if (!GodotObject.IsInstanceValid(value) || value.isDestroy)
			{
				continue;
			}
			bool flag = value is TowerDefenseZombie;
			Vector2 logicalGlobalPosition = value.GetLogicalGlobalPosition();
			Dictionary dictionary = new Dictionary
			{
				["sync_id"] = syncCharacter.Key,
				["px"] = Mathf.Snapped(logicalGlobalPosition.X, 0.1f),
				["py"] = Mathf.Snapped(logicalGlobalPosition.Y, 0.1f)
			};
			Dictionary dictionary2 = value.CaptureMainStateMachineSnapshotData();
			if (dictionary2.Count > 0)
			{
				dictionary["sm"] = dictionary2;
			}
			if (!flag && GodotObject.IsInstanceValid(value.sprite))
			{
				dictionary["clip"] = value.sprite.clip;
				dictionary["loop"] = value.sprite.loop;
				dictionary["blendTime"] = value.sprite.blendTime;
				dictionary["frame"] = value.sprite.frameIndex;
			}
			if (!flag && GodotObject.IsInstanceValid(value.instance))
			{
				dictionary["hp"] = value.instance.hitpoints;
				dictionary["ar"] = NetworkCharacterStateSnapshots.BuildArmorSnapshot(value);
				if (GodotObject.IsInstanceValid(value.instance.damagePointData))
				{
					dictionary["dpi"] = value.instance.damagePointIndex;
				}
			}
			Dictionary dictionary3 = NetworkCharacterStateSnapshots.BuildComponentDelta(value, _characterContext.ComponentLastSyncState, syncCharacter.Key, force: true, updateCache: false);
			if (dictionary3.Count > 0)
			{
				dictionary["components"] = dictionary3;
			}
			if (!flag)
			{
				BuffComponent buff = value.buff;
				if (buff != null && !buff.IsReleased)
				{
					Godot.Collections.Array array2 = new Godot.Collections.Array();
					foreach (string key4 in value.buff.buffDictionary.Keys)
					{
						Dictionary dictionary4 = new Dictionary { ["k"] = key4 };
						TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = value.buff.buffDictionary[key4];
						if (GodotObject.IsInstanceValid(towerDefenseCharacterBuffConfig))
						{
							Variant value2 = towerDefenseCharacterBuffConfig.Get("time");
							Variant value3 = towerDefenseCharacterBuffConfig.Get("currentTime");
							if (value2.VariantType != Variant.Type.Nil)
							{
								dictionary4["t"] = value2;
							}
							if (value3.VariantType != Variant.Type.Nil)
							{
								dictionary4["ct"] = value3;
							}
						}
						array2.Add(dictionary4);
					}
					dictionary["bf"] = array2;
				}
			}
			if (!flag)
			{
				Dictionary dictionary5 = value.ExportNetworkSpecialState();
				if (dictionary5.Count > 0)
				{
					dictionary["sp"] = dictionary5;
				}
			}
			dictionary["seq"] = NextOutgoingOwnerSequence(syncCharacter.Key);
			array.Add(dictionary);
		}
		return array;
	}

	private static void ApplyBuffState(TowerDefenseCharacter character, Godot.Collections.Array buffsData)
	{
		character.buff.ApplyLegacyNetworkSnapshot(buffsData);
	}

	public void SendInitialState(int syncId)
	{
		if (_characterContext == null || !_characterContext.SyncCharacters.ContainsKey(syncId))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[syncId];
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy)
		{
			string clipName = "";
			bool loop = true;
			double blendTime = 0.0;
			int frame = 0;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.sprite))
			{
				clipName = towerDefenseCharacter.sprite.clip;
				loop = towerDefenseCharacter.sprite.loop;
				blendTime = towerDefenseCharacter.sprite.blendTime;
				frame = towerDefenseCharacter.sprite.frameIndex;
			}
			Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
			_characterContext.SendCharacterInit(syncId, logicalGlobalPosition.X, logicalGlobalPosition.Y, GodotObject.IsInstanceValid(towerDefenseCharacter.instance) ? towerDefenseCharacter.instance.hitpoints : 0.0, towerDefenseCharacter.die, clipName, loop, blendTime, frame, towerDefenseCharacter.timeScale, (towerDefenseCharacter is TowerDefenseZombie towerDefenseZombie) ? towerDefenseZombie.walkSpeedScale : 1.0);
			Dictionary dictionary = new Dictionary
			{
				["sync_id"] = syncId,
				["seq"] = NextOutgoingOwnerSequence(syncId)
			};
			Dictionary dictionary2 = towerDefenseCharacter.CaptureMainStateMachineSnapshotData();
			if (dictionary2.Count > 0)
			{
				dictionary["sm"] = dictionary2;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance.damagePointData))
			{
				dictionary["dpi"] = towerDefenseCharacter.instance.damagePointIndex;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
			{
				dictionary["ar"] = NetworkCharacterStateSnapshots.BuildArmorSnapshot(towerDefenseCharacter);
			}
			Dictionary dictionary3 = NetworkCharacterStateSnapshots.BuildComponentDelta(towerDefenseCharacter, _characterContext.ComponentLastSyncState, syncId, force: true);
			if (dictionary3.Count > 0)
			{
				dictionary["components"] = dictionary3;
			}
			if (dictionary.Count > 1)
			{
				Godot.Collections.Array array = new Godot.Collections.Array { dictionary };
				_characterContext.SendCharacterState(Json.Stringify(array));
			}
		}
	}

	public void SendPosition(int syncId)
	{
		if (_characterContext != null && _characterContext.SyncCharacters.ContainsKey(syncId))
		{
			TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[syncId];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy)
			{
				Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
				_characterContext.SendCharacterPosition(syncId, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
		}
	}

	public void SendDestroy(TowerDefenseCharacter character)
	{
		if (base.Context != null && base.Context.IsHost && _characterContext != null && GodotObject.IsInstanceValid(character) && character.syncId >= 0)
		{
			_characterContext.SendCharacterDestroy(character.syncId, character.isExplode, character.isSmash);
			RemoveOwnerSequenceState(character.syncId);
		}
	}

	public void ApplyDestroy(int syncId, bool isExplode, bool isSmash)
	{
		if (_characterContext == null || syncId < 0)
		{
			return;
		}
		if (_pendingComponentOperationDestroys.ContainsKey(syncId))
		{
			QueuePendingComponentOperationDestroy(syncId, isExplode, isSmash);
			return;
		}
		if (_characterContext.SyncCharacters.TryGetValue(syncId, out var value) && GodotObject.IsInstanceValid(value) && !value.isDestroy)
		{
			if (value.IsNodeReady())
			{
				DestroyComponent destroyComponent = value.destroyComponent;
				if (destroyComponent != null && !destroyComponent.IsReleased)
				{
					goto IL_0070;
				}
			}
			QueuePendingComponentOperationDestroy(syncId, isExplode, isSmash);
			return;
		}
		goto IL_0070;
		IL_0070:
		CharacterComponentOperationDestroyBarrier componentOperationDestroyBarrier = _componentOperationDestroyBarrier;
		if (componentOperationDestroyBarrier != null && componentOperationDestroyBarrier.HasPending(syncId))
		{
			QueuePendingComponentOperationDestroy(syncId, isExplode, isSmash);
			return;
		}
		_pendingComponentOperationDestroys.Remove(syncId);
		ApplyDestroyNow(syncId, isExplode, isSmash);
	}

	private void QueuePendingComponentOperationDestroy(int syncId, bool isExplode, bool isSmash)
	{
		if (_pendingComponentOperationDestroys.TryGetValue(syncId, out var value))
		{
			value.IsExplode |= isExplode;
			value.IsSmash |= isSmash;
			if (!value.HasBoundTarget && _characterContext.SyncCharacters.TryGetValue(syncId, out var value2) && GodotObject.IsInstanceValid(value2))
			{
				value.Target = value2;
				value.HasBoundTarget = true;
			}
		}
		else
		{
			_characterContext.SyncCharacters.TryGetValue(syncId, out var value3);
			_pendingComponentOperationDestroys[syncId] = new PendingComponentOperationDestroy
			{
				IsExplode = isExplode,
				IsSmash = isSmash,
				Target = (GodotObject.IsInstanceValid(value3) ? value3 : null),
				HasBoundTarget = GodotObject.IsInstanceValid(value3)
			};
		}
	}

	public void BindPendingDestroyTarget(int syncId, TowerDefenseCharacter character)
	{
		if (syncId < 0 || !GodotObject.IsInstanceValid(character) || !_pendingComponentOperationDestroys.TryGetValue(syncId, out var value))
		{
			return;
		}
		if (value.HasBoundTarget)
		{
			if (value.Target != character)
			{
				_pendingOwnerStates.Remove(syncId);
			}
		}
		else
		{
			value.Target = character;
			value.HasBoundTarget = true;
		}
	}

	public void DiscardPendingOwnerState(int syncId)
	{
		if (syncId >= 0)
		{
			_pendingOwnerStates.Remove(syncId);
		}
	}

	private void ProcessPendingComponentOperationDestroys(double delta)
	{
		if (_pendingComponentOperationDestroys.Count == 0)
		{
			return;
		}
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		foreach (KeyValuePair<int, PendingComponentOperationDestroy> pendingComponentOperationDestroy in _pendingComponentOperationDestroys)
		{
			PendingComponentOperationDestroy value = pendingComponentOperationDestroy.Value;
			value.Elapsed += Math.Max(0.0, delta);
			TowerDefenseCharacter value2;
			bool flag = _characterContext.SyncCharacters.TryGetValue(pendingComponentOperationDestroy.Key, out value2) && GodotObject.IsInstanceValid(value2);
			if (value.HasBoundTarget && (!GodotObject.IsInstanceValid(value.Target) || !flag || value2 != value.Target))
			{
				list2.Add(pendingComponentOperationDestroy.Key);
				continue;
			}
			bool num = _componentOperationDestroyBarrier?.HasPending(pendingComponentOperationDestroy.Key) ?? false;
			bool flag2 = flag && !value2.isDestroy && (!value2.IsNodeReady() || (value2.destroyComponent?.IsReleased ?? true));
			if (!num && !flag2)
			{
				list.Add(pendingComponentOperationDestroy.Key);
			}
			else if (!(value.Elapsed < 5.1))
			{
				_componentOperationDestroyBarrier?.Invalidate(pendingComponentOperationDestroy.Key);
				list.Add(pendingComponentOperationDestroy.Key);
			}
		}
		foreach (int item in list)
		{
			if (_pendingComponentOperationDestroys.Remove(item, out var value3))
			{
				ApplyDestroyNow(item, value3.IsExplode, value3.IsSmash, value3.Target, value3.HasBoundTarget);
			}
		}
		foreach (int item2 in list2)
		{
			if (_pendingComponentOperationDestroys.Remove(item2, out var value4))
			{
				CleanupStalePendingDestroy(item2, value4.Target);
			}
		}
	}

	private void ApplyDestroyNow(int syncId, bool isExplode, bool isSmash, TowerDefenseCharacter expectedCharacter = null, bool hasExpectedCharacter = false)
	{
		if (!_characterContext.SyncCharacters.ContainsKey(syncId))
		{
			if (hasExpectedCharacter)
			{
				CleanupStalePendingDestroy(syncId, expectedCharacter);
				return;
			}
			_characterContext.PendingDestroySyncIds[syncId] = new Dictionary
			{
				["is_explode"] = isExplode,
				["is_smash"] = isSmash,
				["created_at_msec"] = (long)Time.GetTicksMsec()
			};
			RemoveOwnerSequenceState(syncId);
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[syncId];
		if (hasExpectedCharacter && (!GodotObject.IsInstanceValid(expectedCharacter) || towerDefenseCharacter != expectedCharacter))
		{
			CleanupStalePendingDestroy(syncId, expectedCharacter);
			return;
		}
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || towerDefenseCharacter.isDestroy)
		{
			RemoveCharacterAndZombieSyncState(syncId);
			RemoveOwnerSequenceState(syncId);
			return;
		}
		if (towerDefenseCharacter.IsNodeReady())
		{
			DestroyComponent destroyComponent = towerDefenseCharacter.destroyComponent;
			if (destroyComponent != null && !destroyComponent.IsReleased)
			{
				RemoveCharacterAndZombieSyncState(syncId);
				RemoveOwnerSequenceState(syncId);
				_characterContext.CleanupCharacterCell(towerDefenseCharacter);
				if (isExplode && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && towerDefenseCharacter.instance.ashScene != null && !towerDefenseCharacter.inWater)
				{
					TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(towerDefenseCharacter.instance.ashScene, towerDefenseCharacter.gridPos, "Idle");
					Node2D characterNode = TowerDefenseManager.GetCharacterNode();
					towerDefenseEffectSpriteOnce.GlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
					towerDefenseEffectSpriteOnce.Scale = towerDefenseCharacter.Scale * towerDefenseCharacter.transformPoint.Scale;
					characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
					towerDefenseEffectSpriteOnce.ZIndex -= 6;
				}
				towerDefenseCharacter.isExplode = isExplode;
				towerDefenseCharacter.isSmash = isSmash;
				if (isSmash && towerDefenseCharacter is TowerDefensePlant && TowerDefenseManager.Instance.CheckMapGridPosIn(towerDefenseCharacter.gridPos))
				{
					towerDefenseCharacter.CancelCellMoveTween();
					towerDefenseCharacter.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(towerDefenseCharacter.gridPos));
				}
				DestroyComponent destroyComponent2 = towerDefenseCharacter.destroyComponent;
				if (destroyComponent2 != null && !destroyComponent2.IsReleased)
				{
					towerDefenseCharacter.destroyComponent.isRemoteDestroy = true;
				}
				towerDefenseCharacter.Destroy();
				return;
			}
		}
		RemoveCharacterAndZombieSyncState(syncId);
		RemoveOwnerSequenceState(syncId);
		_characterContext.CleanupCharacterCell(towerDefenseCharacter);
		towerDefenseCharacter.skipDestroySet = true;
		if (!towerDefenseCharacter.IsQueuedForDeletion())
		{
			towerDefenseCharacter.QueueFree();
		}
	}

	private void RemoveCharacterAndZombieSyncState(int syncId)
	{
		_characterContext.RemoveCharacterSyncState(syncId);
		if (_characterContext is IZombieStateNetworkContext zombieStateNetworkContext)
		{
			zombieStateNetworkContext.RemoveZombieSyncState(syncId);
		}
	}

	private void CleanupStalePendingDestroy(int syncId, TowerDefenseCharacter character)
	{
		_pendingOwnerStates.Remove(syncId);
		if (GodotObject.IsInstanceValid(character) && !character.isDestroy && character.syncId == syncId)
		{
			_characterContext.CleanupCharacterCell(character);
			character.skipDestroySet = true;
			if (!character.IsQueuedForDeletion())
			{
				character.QueueFree();
			}
		}
	}

	public override void Dispose()
	{
		_pendingComponentOperationDestroys.Clear();
		_componentOperationDestroyBarrier?.Clear();
		_componentOperationDestroyBarrier = null;
		base.Dispose();
	}

	public void ApplyInit(Dictionary data)
	{
		if (_characterContext == null || data == null)
		{
			return;
		}
		int num = data.GetValueOrDefault("sync_id", -1).AsInt32();
		if (num < 0 || !_characterContext.SyncCharacters.ContainsKey(num))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[num];
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || towerDefenseCharacter.isDestroy)
		{
			return;
		}
		Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
		towerDefenseCharacter.SetLogicalGlobalPosition(new Vector2((float)data.GetValueOrDefault("x", logicalGlobalPosition.X).AsDouble(), (float)data.GetValueOrDefault("y", logicalGlobalPosition.Y).AsDouble()));
		if (data.ContainsKey("hp") && GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			towerDefenseCharacter.instance.hitpoints = data["hp"].AsDouble();
		}
		if (data.ContainsKey("die") && data["die"].AsBool())
		{
			towerDefenseCharacter.die = true;
		}
		string text = data.GetValueOrDefault("clip", "").AsString();
		if (text != "")
		{
			bool loopAnim = data.GetValueOrDefault("loop", true).AsBool();
			double num2 = data.GetValueOrDefault("blendTime", 0.0).AsDouble();
			towerDefenseCharacter.SyncAnimation(text, loopAnim, (float)num2);
			if (data.ContainsKey("frame") && GodotObject.IsInstanceValid(towerDefenseCharacter.sprite))
			{
				towerDefenseCharacter.sprite.frameIndex = data["frame"].AsInt32();
			}
		}
		if (data.ContainsKey("timeScale"))
		{
			towerDefenseCharacter.timeScale = data["timeScale"].AsDouble();
		}
		if (data.ContainsKey("walkSpeedScale") && towerDefenseCharacter is TowerDefenseZombie towerDefenseZombie)
		{
			towerDefenseZombie.walkSpeedScale = data["walkSpeedScale"].AsDouble();
		}
		if (data.ContainsKey("sm"))
		{
			towerDefenseCharacter.RestoreMainStateMachineSnapshotData(data["sm"].AsGodotDictionary(), remote: true, suppressEntryEffects: true);
		}
		if (data.ContainsKey("components"))
		{
			NetworkCharacterStateSnapshots.ApplyComponents(towerDefenseCharacter, data["components"].AsGodotDictionary());
		}
		if (data.ContainsKey("ar") && GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			NetworkCharacterStateSnapshots.ApplyArmorSnapshot(towerDefenseCharacter, data["ar"].AsGodotArray());
		}
		if (towerDefenseCharacter is TowerDefensePlant)
		{
			ApplyRemotePlantStateProcessMode(towerDefenseCharacter);
		}
		ReplayPendingOwnerState(num);
	}

	private static void ApplyRemotePlantStateProcessMode(TowerDefenseCharacter character, bool allowDeferredRetry = true)
	{
		if (!(character is TowerDefensePlant) || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		character.ProcessMode = Node.ProcessModeEnum.Inherit;
		IStateMachineController stateMachine = character.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			character.SetMainStateMachineDispatchEnabled(enabled: true);
		}
		else if (allowDeferredRetry)
		{
			Callable.From(delegate
			{
				ApplyRemotePlantStateProcessMode(character, allowDeferredRetry: false);
			}).CallDeferred();
		}
	}

	public void ApplyPosition(Dictionary data)
	{
		if (_characterContext == null || data == null)
		{
			return;
		}
		int num = data.GetValueOrDefault("sync_id", -1).AsInt32();
		if (num >= 0 && _characterContext.SyncCharacters.ContainsKey(num))
		{
			TowerDefenseCharacter towerDefenseCharacter = _characterContext.SyncCharacters[num];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy && !(towerDefenseCharacter is TowerDefenseZombie))
			{
				Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
				towerDefenseCharacter.SetLogicalGlobalPosition(new Vector2((float)data.GetValueOrDefault("x", logicalGlobalPosition.X).AsDouble(), (float)data.GetValueOrDefault("y", logicalGlobalPosition.Y).AsDouble()));
			}
		}
	}

	private void CleanupPendingDestroys()
	{
		if (_characterContext == null || _characterContext.PendingDestroySyncIds.Count == 0)
		{
			return;
		}
		List<Variant> list = new List<Variant>();
		long ticksMsec = (long)Time.GetTicksMsec();
		foreach (Variant key in _characterContext.PendingDestroySyncIds.Keys)
		{
			long num = _characterContext.PendingDestroySyncIds[key].AsGodotDictionary().GetValueOrDefault("created_at_msec", ticksMsec).AsInt64();
			if (ticksMsec - num >= 30000 && !_characterContext.SyncCharacters.ContainsKey(key.AsInt32()))
			{
				list.Add(key);
			}
		}
		foreach (Variant item in list)
		{
			_characterContext.PendingDestroySyncIds.Remove(item);
		}
	}
}
