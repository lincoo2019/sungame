using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieDisco : TowerDefenseZombie, IJackson, INetworkDancerOwner
{
	private const double PendingDancerResolveIntervalSeconds = 0.25;

	private StateHandle _moonWalkStateHandle;

	private StateHandle _danceStateHandle;

	private StateHandle _pointStateHandle;

	private bool _stateSignalsConnected;

	private Sprite2D _spotlight2;

	private Sprite2D _spotlight;

	public bool moonWalkOver;

	public bool moonWalkMode;

	public Vector2 savePos;

	public int walkTime = 4;

	public int danceTime = 2;

	public Array<TowerDefenseCharacter> dancerList = new Array<TowerDefenseCharacter>();

	private readonly int[] _pendingDancerSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingDancerNodeNames = new string[4] { "", "", "", "" };

	private double _pendingDancerResolveRemaining;

	private bool _hasPendingDancerRelations;

	public bool firstSpawn;

	public bool isPointing;

	private float _moonWalkThreshold;

	private double _mapGroundRight;

	[Export(PropertyHint.None, "")]
	public Gradient spotlightGrandient { get; set; }

	[Export(PropertyHint.None, "")]
	public string dancerPacketName { get; set; } = "ZombieBackup";

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_moonWalkStateHandle = base.StateMachine?.GetStateById("zombie.disco.moon_walk");
		_danceStateHandle = base.StateMachine?.GetStateById("zombie.disco.dance");
		_pointStateHandle = base.StateMachine?.GetStateById("zombie.disco.point");
		StateHandle moonWalkStateHandle = _moonWalkStateHandle;
		if (moonWalkStateHandle == null || !moonWalkStateHandle.IsValid)
		{
			return;
		}
		StateHandle danceStateHandle = _danceStateHandle;
		if (danceStateHandle != null && danceStateHandle.IsValid)
		{
			StateHandle pointStateHandle = _pointStateHandle;
			if (pointStateHandle != null && pointStateHandle.IsValid)
			{
				_moonWalkStateHandle.Entered += MoonWalkEntered;
				_moonWalkStateHandle.Exited += MoonWalkExited;
				_moonWalkStateHandle.PhysicsProcessing += MoonWalkProcessing;
				_danceStateHandle.Entered += DanceEntered;
				_danceStateHandle.Exited += DanceExited;
				_danceStateHandle.PhysicsProcessing += DanceProcessing;
				_pointStateHandle.Entered += PointEntered;
				_pointStateHandle.Exited += PointExited;
				_pointStateHandle.PhysicsProcessing += PointProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_moonWalkStateHandle != null)
			{
				_moonWalkStateHandle.Entered -= MoonWalkEntered;
				_moonWalkStateHandle.Exited -= MoonWalkExited;
				_moonWalkStateHandle.PhysicsProcessing -= MoonWalkProcessing;
			}
			_moonWalkStateHandle = null;
			if (_danceStateHandle != null)
			{
				_danceStateHandle.Entered -= DanceEntered;
				_danceStateHandle.Exited -= DanceExited;
				_danceStateHandle.PhysicsProcessing -= DanceProcessing;
			}
			_danceStateHandle = null;
			if (_pointStateHandle != null)
			{
				_pointStateHandle.Entered -= PointEntered;
				_pointStateHandle.Exited -= PointExited;
				_pointStateHandle.PhysicsProcessing -= PointProcessing;
			}
			_pointStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		if (IsQueuedForDeletion())
		{
			ReleaseDancerOwnership();
		}
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_spotlight2 = GetNode<Sprite2D>("%Spotlight2");
			_spotlight = GetNode<Sprite2D>("%Spotlight");
			_moonWalkThreshold = (float)((double)TowerDefenseManager.Instance.GetMapGridSize().X * 2.5);
			_mapGroundRight = TowerDefenseManager.Instance.GetMapGroundRight();
			dancerList.Resize(4);
			ConnectStateSignals();
			RefreshPendingDancerBatchEligibility();
			if (_hasPendingDancerRelations)
			{
				ResolvePendingDancerRelations();
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_hasPendingDancerRelations)
		{
			_pendingDancerResolveRemaining -= delta;
			if (!(_pendingDancerResolveRemaining > 0.0))
			{
				ResolvePendingDancerRelations();
			}
		}
	}

	public void MoonWalkEntered()
	{
		base.sprite.SetAnimation("MoonWalk", loop: true, 0.2);
		base.sprite.Scale = new Vector2(-0.8f, base.sprite.Scale.Y);
		groundMoveComponent.RefreshDirectionCache();
		groundMoveComponent.SetAlive(true);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if ((double)logicalGlobalPosition.X < TowerDefenseManager.Instance.GetMapGroundRight())
		{
			moonWalkMode = true;
			savePos = logicalGlobalPosition;
		}
	}

	public void MoonWalkProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		float x = GetLogicalGlobalPosition().X;
		if (moonWalkMode)
		{
			if (Mathf.Abs(x - savePos.X) > _moonWalkThreshold)
			{
				moonWalkOver = true;
				SendStateEvent("ToPoint");
				return;
			}
		}
		else if ((double)x < _mapGroundRight - (double)_moonWalkThreshold)
		{
			moonWalkOver = true;
			SendStateEvent("ToPoint");
			return;
		}
		if (attackComponent.CanAttack())
		{
			moonWalkOver = true;
			SendStateEvent("ToPoint");
		}
	}

	public void MoonWalkExited()
	{
		groundMoveComponent.SetAlive(false);
		base.sprite.Scale = new Vector2(0.8f, base.sprite.Scale.Y);
		groundMoveComponent.RefreshDirectionCache();
	}

	public void DanceEntered()
	{
		danceTime = 2;
		base.sprite.SetAnimation("ArmRise", loop: true, 0.2);
	}

	public void DanceProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (!base.sprite.pause && attackComponent.CanAttack())
		{
			Attack();
		}
	}

	public void DanceExited()
	{
		base.sprite.Scale = new Vector2(0.8f, base.sprite.Scale.Y);
	}

	public void PointEntered()
	{
		isPointing = true;
		base.sprite.SetAnimation("PointUp", loop: false, 0.2);
		base.sprite.AddAnimation("PointDown", 0.75, loop: false, 0.2);
	}

	public void PointProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.75;
	}

	public void PointExited()
	{
		isPointing = false;
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		base.sprite.Scale = new Vector2(0.8f, base.sprite.Scale.Y);
		foreach (TowerDefenseCharacter dancer in dancerList)
		{
			if (GodotObject.IsInstanceValid(dancer) && dancer is TowerDefenseZombie towerDefenseZombie)
			{
				towerDefenseZombie.timeScale = timeScale;
				towerDefenseZombie.walkSpeedScale = base.walkSpeedScale;
			}
		}
		walkTime = 4;
	}

	public override void WalkProcessing(double delta)
	{
		groundMoveComponent.SetAlive(CanWalk());
		base.WalkProcessing(delta);
	}

	public override void Walk()
	{
		if (!moonWalkOver)
		{
			SendStateEvent("ToMoonWalk");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		groundMoveComponent.SetAlive(false);
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		switch (clip)
		{
		case "PointDown":
			isPointing = false;
			foreach (TowerDefenseCharacter dancer in dancerList)
			{
				if (GodotObject.IsInstanceValid(dancer) && !dancer.die && !dancer.nearDie)
				{
					((TowerDefenseZombie)dancer).Walk();
				}
			}
			Walk();
			break;
		case "Walk":
			walkTime--;
			if (!die && !nearDie)
			{
				if (walkTime > 0)
				{
					break;
				}
				if (CanSpawnDancer())
				{
					SendStateEvent("ToPoint");
					break;
				}
				foreach (TowerDefenseCharacter dancer2 in dancerList)
				{
					if (GodotObject.IsInstanceValid(dancer2) && !dancer2.die && !dancer2.nearDie && dancer2.sprite.clip == "Walk")
					{
						dancer2.SendStateEvent("ToDance");
					}
				}
				SendStateEvent("ToDance");
			}
			else
			{
				Die();
			}
			break;
		case "ArmRise":
			danceTime--;
			if (!die && !nearDie)
			{
				if (danceTime > 0)
				{
					break;
				}
				if (CanSpawnDancer())
				{
					SendStateEvent("ToPoint");
					break;
				}
				foreach (TowerDefenseCharacter dancer3 in dancerList)
				{
					if (GodotObject.IsInstanceValid(dancer3) && !dancer3.die && !dancer3.nearDie && dancer3.sprite.clip == "ArmRise")
					{
						((TowerDefenseZombie)dancer3).Walk();
					}
				}
				Walk();
			}
			else
			{
				Die();
			}
			break;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "spawn" && !die && !nearDie)
		{
			SpawnDancer();
			_spotlight.Visible = true;
			_spotlight2.Visible = true;
			ChangeSpotlightColor();
			if (!firstSpawn)
			{
				firstSpawn = true;
				AudioManager.Instance.AudioPlay("Dancer");
			}
		}
	}

	public bool CanWalk()
	{
		foreach (TowerDefenseCharacter dancer in dancerList)
		{
			if (!CanDancerMoveWithJackson(dancer))
			{
				return false;
			}
		}
		return true;
	}

	private static bool CanDancerMoveWithJackson(TowerDefenseCharacter dancer)
	{
		if (!GodotObject.IsInstanceValid(dancer))
		{
			return true;
		}
		if (dancer.die || dancer.nearDie || dancer.isRise)
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(dancer.sprite))
		{
			return true;
		}
		return dancer.sprite.clip == "Walk";
	}

	public bool CanSpawnDancer()
	{
		if (!base.IsInsideComponentBattlefield)
		{
			return false;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		if (base.gridPos.Y > 1 && !GodotObject.IsInstanceValid(dancerList[0]))
		{
			return true;
		}
		if (base.gridPos.Y < mapGridNum.Y && !GodotObject.IsInstanceValid(dancerList[1]))
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(dancerList[2]))
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(dancerList[3]))
		{
			return true;
		}
		return false;
	}

	public void SpawnDancer()
	{
		if (!base.IsInsideComponentBattlefield || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(dancerPacketName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (base.gridPos.Y > 1 && !GodotObject.IsInstanceValid(dancerList[0]))
		{
			TowerDefenseCharacter dancer = CreateDancer(packetConfig, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y - 1)), base.gridPos - new Vector2I(0, 1));
			if (!GodotObject.IsInstanceValid(dancer))
			{
				return;
			}
			TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", dancer);
			dancer.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
			dancer.SetDeferred("invisible", base.invisible);
			if (instance.hypnoses)
			{
				Callable.From(delegate
				{
					dancer.Hypnoses();
				}).CallDeferred();
			}
			SetNetworkDancer(0, dancer);
			TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer, useCreate: true, 1.5, walkAfterSpawn: false, "", CreateDancerSpawnState(0));
			Callable.From(delegate
			{
				dancer.Rise(1.5, 0.0, createDirt: true, changeState: true, 150.0, emitRiseOver: false);
			}).CallDeferred();
		}
		if (base.gridPos.Y < mapGridNum.Y && !GodotObject.IsInstanceValid(dancerList[1]))
		{
			TowerDefenseCharacter dancer2 = CreateDancer(packetConfig, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y + 1)), base.gridPos + new Vector2I(0, 1));
			if (!GodotObject.IsInstanceValid(dancer2))
			{
				return;
			}
			TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", dancer2);
			dancer2.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
			dancer2.SetDeferred("invisible", base.invisible);
			if (instance.hypnoses)
			{
				Callable.From(delegate
				{
					dancer2.Hypnoses();
				}).CallDeferred();
			}
			SetNetworkDancer(1, dancer2);
			TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer2, useCreate: true, 1.5, walkAfterSpawn: false, "", CreateDancerSpawnState(1));
			Callable.From(delegate
			{
				dancer2.Rise(1.5, 0.0, createDirt: true, changeState: true, 150.0, emitRiseOver: false);
			}).CallDeferred();
		}
		if (!GodotObject.IsInstanceValid(dancerList[2]))
		{
			TowerDefenseCharacter dancer3 = CreateDancer(packetConfig, logicalGlobalPosition - new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos - new Vector2I(1, 0));
			if (!GodotObject.IsInstanceValid(dancer3))
			{
				return;
			}
			TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", dancer3);
			dancer3.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
			dancer3.SetDeferred("invisible", base.invisible);
			if (instance.hypnoses)
			{
				Callable.From(delegate
				{
					dancer3.Hypnoses();
				}).CallDeferred();
			}
			SetNetworkDancer(2, dancer3);
			TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer3, useCreate: true, 1.5, walkAfterSpawn: false, "", CreateDancerSpawnState(2));
			Callable.From(delegate
			{
				dancer3.Rise(1.5, 0.0, createDirt: true, changeState: true, 150.0, emitRiseOver: false);
			}).CallDeferred();
		}
		if (GodotObject.IsInstanceValid(dancerList[3]))
		{
			return;
		}
		TowerDefenseCharacter dancer4 = CreateDancer(packetConfig, logicalGlobalPosition + new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos + new Vector2I(1, 0));
		if (!GodotObject.IsInstanceValid(dancer4))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", dancer4);
		dancer4.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
		dancer4.SetDeferred("invisible", base.invisible);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				dancer4.Hypnoses();
			}).CallDeferred();
		}
		SetNetworkDancer(3, dancer4);
		TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer4, useCreate: true, 1.5, walkAfterSpawn: false, "", CreateDancerSpawnState(3));
		Callable.From(delegate
		{
			dancer4.Rise(1.5, 0.0, createDirt: true, changeState: true, 150.0, emitRiseOver: false);
		}).CallDeferred();
	}

	private TowerDefenseCharacter CreateDancer(TowerDefensePacketConfig packetConfig, Vector2 position, Vector2I dancerGridPos)
	{
		if (!base.EconomyOwnerAccountId.IsValid)
		{
			return packetConfig.Create(position, dancerGridPos);
		}
		return packetConfig.Create(base.EconomyOwnerAccountId, position, dancerGridPos);
	}

	private Dictionary CreateDancerSpawnState(int slot)
	{
		return new Dictionary
		{
			["rise_delay"] = 0.0,
			["rise_create_dirt"] = true,
			["rise_change_state"] = true,
			["rise_from"] = 150.0,
			["rise_emit_over"] = false,
			["spawn_invisible"] = base.invisible,
			["dancer_parent_sync_id"] = syncId,
			["dancer_slot"] = slot
		};
	}

	public void ChangeSpotlightColor()
	{
		Color modulate = spotlightGrandient.Sample(GD.Randf());
		_spotlight.Modulate = modulate;
		_spotlight2.Modulate = modulate;
		GetTree().CreateTimer(3.0, processAlways: false).Timeout += ChangeSpotlightColor;
	}

	public void RemoveDancer(TowerDefenseCharacter dancer)
	{
		int num = dancerList.IndexOf(dancer);
		if (num != -1)
		{
			SetNetworkDancer(num, null);
		}
	}

	public void SetNetworkDancer(int slot, TowerDefenseCharacter dancer)
	{
		if (slot >= 0 && slot < 4)
		{
			if (dancerList.Count != 4)
			{
				dancerList.Resize(4);
			}
			TowerDefenseCharacter towerDefenseCharacter = dancerList[slot];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter != dancer && towerDefenseCharacter is IDancer dancer2)
			{
				dancer2.SetJackson(null);
			}
			dancerList[slot] = dancer;
			if (GodotObject.IsInstanceValid(dancer) && dancer is IDancer dancer3)
			{
				dancer3.SetJackson(this);
			}
			_pendingDancerSyncIds[slot] = -1;
			_pendingDancerNodeNames[slot] = "";
			RefreshPendingDancerBatchEligibility();
		}
	}

	private void RefreshPendingDancerBatchEligibility()
	{
		bool flag = false;
		for (int i = 0; i < 4; i++)
		{
			if (_pendingDancerSyncIds[i] >= 0 || _pendingDancerNodeNames[i] != "")
			{
				flag = true;
				break;
			}
		}
		if (_hasPendingDancerRelations != flag)
		{
			_hasPendingDancerRelations = flag;
			_pendingDancerResolveRemaining = (flag ? 0.25 : 0.0);
		}
	}

	private void ResolvePendingDancerRelations()
	{
		if (!_hasPendingDancerRelations)
		{
			return;
		}
		if (dancerList.Count != 4)
		{
			dancerList.Resize(4);
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		for (int i = 0; i < 4; i++)
		{
			int num = _pendingDancerSyncIds[i];
			if (num >= 0 && GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value) && GodotObject.IsInstanceValid(value))
			{
				SetNetworkDancer(i, value);
				continue;
			}
			string text = _pendingDancerNodeNames[i];
			if (!(text == "") && GodotObject.IsInstanceValid(node2D))
			{
				TowerDefenseCharacter nodeOrNull = node2D.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text));
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					SetNetworkDancer(i, nodeOrNull);
				}
			}
		}
		_pendingDancerResolveRemaining = (_hasPendingDancerRelations ? 0.25 : 0.0);
	}

	private void ReleaseDancerOwnership()
	{
		for (int i = 0; i < 4; i++)
		{
			SetNetworkDancer(i, null);
		}
		System.Array.Fill(_pendingDancerSyncIds, -1);
		System.Array.Fill(_pendingDancerNodeNames, "");
		RefreshPendingDancerBatchEligibility();
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		ReleaseDancerOwnership();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Array<int> array = new Array<int>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = ((dancerList.Count > i) ? dancerList[i] : null);
			array.Add(GodotObject.IsInstanceValid(towerDefenseCharacter) ? towerDefenseCharacter.syncId : _pendingDancerSyncIds[i]);
		}
		return new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["dancerSyncIds"] = array
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (!data.ContainsKey("dancerSyncIds"))
		{
			return;
		}
		Godot.Collections.Array array = data["dancerSyncIds"].AsGodotArray();
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		for (int i = 0; i < 4; i++)
		{
			int num = ((i < array.Count) ? array[i].AsInt32() : (-1));
			if (num < 0)
			{
				SetNetworkDancer(i, null);
				continue;
			}
			if (GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value) && GodotObject.IsInstanceValid(value))
			{
				SetNetworkDancer(i, value);
				continue;
			}
			SetNetworkDancer(i, null);
			_pendingDancerSyncIds[i] = num;
			_pendingDancerNodeNames[i] = "";
		}
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override int GetNetworkSpecialStateRevision()
	{
		int num = 17;
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = ((dancerList.Count > i) ? dancerList[i] : null);
			int num2 = (GodotObject.IsInstanceValid(towerDefenseCharacter) ? towerDefenseCharacter.syncId : _pendingDancerSyncIds[i]);
			num = num * 31 + num2;
		}
		return num;
	}

	public override Dictionary ExportVariantSave()
	{
		Array<string> array = new Array<string>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = ((dancerList.Count > i) ? dancerList[i] : null);
			array.Add(GodotObject.IsInstanceValid(towerDefenseCharacter) ? towerDefenseCharacter.Name.ToString() : _pendingDancerNodeNames[i]);
		}
		return new Dictionary
		{
			["moonWalkOver"] = moonWalkOver,
			["moonWalkMode"] = moonWalkMode,
			["savePos"] = savePos,
			["walkTime"] = walkTime,
			["danceTime"] = danceTime,
			["firstSpawn"] = firstSpawn,
			["isPointing"] = isPointing,
			["dancerNodeNames"] = array
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		moonWalkOver = data.GetValueOrDefault("moonWalkOver", false).AsBool();
		moonWalkMode = data.GetValueOrDefault("moonWalkMode", false).AsBool();
		savePos = data.GetValueOrDefault("savePos", Vector2.Zero).AsVector2();
		walkTime = data.GetValueOrDefault("walkTime", 4).AsInt32();
		danceTime = data.GetValueOrDefault("danceTime", 2).AsInt32();
		firstSpawn = data.GetValueOrDefault("firstSpawn", false).AsBool();
		isPointing = data.GetValueOrDefault("isPointing", false).AsBool();
		if (data.ContainsKey("dancerNodeNames"))
		{
			Godot.Collections.Array array = data["dancerNodeNames"].AsGodotArray();
			Node2D node2D = TowerDefenseManager.GetCharacterNode();
			for (int i = 0; i < 4; i++)
			{
				string text = ((i < array.Count) ? array[i].AsString() : "");
				TowerDefenseCharacter dancer = ((text != "" && GodotObject.IsInstanceValid(node2D)) ? node2D.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text)) : null);
				if (text == "")
				{
					SetNetworkDancer(i, null);
					continue;
				}
				if (GodotObject.IsInstanceValid(dancer))
				{
					SetNetworkDancer(i, dancer);
					continue;
				}
				SetNetworkDancer(i, null);
				_pendingDancerSyncIds[i] = -1;
				_pendingDancerNodeNames[i] = text;
			}
		}
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}
}
