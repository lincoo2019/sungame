using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieDiscopaper : TowerDefenseZombie, IJackson, INetworkDancerOwner
{
	private const double PendingDancerResolveIntervalSeconds = 0.25;

	private const string ZOMBIE_PAPER_MADHEAD = "uid://chsy8qvq8nmd";

	private Sprite2D _spotlight2;

	private Sprite2D _spotlight;

	[Export(PropertyHint.None, "")]
	public Gradient spotlightGrandient;

	public int walkTime = 4;

	private bool _angry;

	public Array<TowerDefenseCharacter> dancerList = new Array<TowerDefenseCharacter>();

	private readonly int[] _pendingDancerSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingDancerNodeNames = new string[4] { "", "", "", "" };

	private double _pendingDancerResolveRemaining;

	private bool _hasPendingDancerRelations;

	[Export(PropertyHint.None, "")]
	public string dancerPacketName = "ZombieBackuppaper";

	public bool firstSpawn;

	public bool isPointing;

	private StateHandle _gaspStateHandle;

	private StateHandle _pointStateHandle;

	private bool _roleStateSignalsConnected;

	public bool angry
	{
		get
		{
			return _angry;
		}
		set
		{
			_angry = value;
			if (_angry)
			{
				base.walkAnimeClip = "AngryWalk";
				base.swimAnimeClip = "AngryWalk";
				base.sprite.SetAtlasReplace("Zombie_dancer__head.png", "uid://chsy8qvq8nmd");
			}
			else
			{
				base.walkAnimeClip = "Walk";
				base.swimAnimeClip = "Walk";
			}
		}
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_gaspStateHandle = base.StateMachine?.GetStateById("zombie.discopaper.gasp");
		_pointStateHandle = base.StateMachine?.GetStateById("zombie.discopaper.point");
		StateHandle gaspStateHandle = _gaspStateHandle;
		if (gaspStateHandle != null && gaspStateHandle.IsValid)
		{
			StateHandle pointStateHandle = _pointStateHandle;
			if (pointStateHandle != null && pointStateHandle.IsValid)
			{
				_gaspStateHandle.Entered += GaspEntered;
				_gaspStateHandle.Exited += GaspExited;
				_gaspStateHandle.PhysicsProcessing += GaspProcessing;
				_pointStateHandle.Entered += PointEntered;
				_pointStateHandle.Exited += PointExited;
				_pointStateHandle.PhysicsProcessing += PointProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_gaspStateHandle != null)
			{
				_gaspStateHandle.Entered -= GaspEntered;
				_gaspStateHandle.Exited -= GaspExited;
				_gaspStateHandle.PhysicsProcessing -= GaspProcessing;
			}
			_gaspStateHandle = null;
			if (_pointStateHandle != null)
			{
				_pointStateHandle.Entered -= PointEntered;
				_pointStateHandle.Exited -= PointExited;
				_pointStateHandle.PhysicsProcessing -= PointProcessing;
			}
			_pointStateHandle = null;
			_roleStateSignalsConnected = false;
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
			dancerList.Resize(4);
			ConnectRoleStateSignals();
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
		SendStateEvent("ToWalk");
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

	public void GaspEntered()
	{
		base.sprite.SetAnimation("Gasp", loop: false, 0.1);
	}

	public void GaspProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public void GaspExited()
	{
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
		case "AngryWalk":
			if (angry)
			{
				walkTime--;
			}
			if (!die && !nearDie)
			{
				if (walkTime <= 0 && CanSpawnDancer())
				{
					SendStateEvent("ToPoint");
				}
			}
			else
			{
				Die();
			}
			break;
		case "Gasp":
			AudioManager.Instance.AudioPlay("NewspaperRarrgh");
			base.sprite.SetAtlasReplace("Zombie_head.png", "uid://chsy8qvq8nmd");
			timeScaleInit = 2.0;
			angry = true;
			SendStateEvent("ToPoint");
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
		if (!base.IsInsideComponentBattlefield || isShow || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(dancerPacketName);
		if (GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(transformPoint))
		{
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (base.gridPos.Y > 1 && !GodotObject.IsInstanceValid(dancerList[0]))
			{
				SpawnSingleDancer(packetConfig, 0, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y - 1)), base.gridPos - new Vector2I(0, 1));
			}
			if (base.gridPos.Y < mapGridNum.Y && !GodotObject.IsInstanceValid(dancerList[1]))
			{
				SpawnSingleDancer(packetConfig, 1, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y + 1)), base.gridPos + new Vector2I(0, 1));
			}
			if (!GodotObject.IsInstanceValid(dancerList[2]))
			{
				SpawnSingleDancer(packetConfig, 2, logicalGlobalPosition - new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos - new Vector2I(1, 0));
			}
			if (!GodotObject.IsInstanceValid(dancerList[3]))
			{
				SpawnSingleDancer(packetConfig, 3, logicalGlobalPosition + new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos + new Vector2I(1, 0));
			}
		}
	}

	private void SpawnSingleDancer(TowerDefensePacketConfig packetConfig, int slot, Vector2 position, Vector2I dancerGridPos)
	{
		TowerDefenseCharacter dancer = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, position, dancerGridPos) : packetConfig.Create(position, dancerGridPos));
		if (!GodotObject.IsInstanceValid(dancer))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", dancer);
		dancer.CallDeferred("SetHitpointAndScale", instance.hitpointScale, transformPoint.Scale);
		dancer.SetDeferred("invisible", base.invisible);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(dancer))
				{
					dancer.Hypnoses();
				}
			}).CallDeferred();
		}
		SetNetworkDancer(slot, dancer);
		TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer, useCreate: true, 1.5, walkAfterSpawn: false, "", CreateDancerSpawnState(slot));
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(dancer))
			{
				dancer.Rise(1.5);
			}
		}).CallDeferred();
	}

	private Dictionary CreateDancerSpawnState(int slot)
	{
		return new Dictionary
		{
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
		dancerList.Clear();
		dancerList.Resize(4);
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Paper")
		{
			SendStateEvent("ToGasp");
			AudioManager.Instance.AudioPlay("NewspaperRip");
		}
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
			{ "walkTime", walkTime },
			{ "angry", angry },
			{ "firstSpawn", firstSpawn },
			{ "isPointing", isPointing },
			{ "dancerNodeNames", array }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		walkTime = data.GetValueOrDefault("walkTime", 4).AsInt32();
		angry = data.GetValueOrDefault("angry", false).AsBool();
		firstSpawn = data.GetValueOrDefault("firstSpawn", false).AsBool();
		isPointing = data.GetValueOrDefault("isPointing", false).AsBool();
		if (data.ContainsKey("dancerNodeNames"))
		{
			Godot.Collections.Array array = data["dancerNodeNames"].AsGodotArray();
			for (int i = 0; i < 4; i++)
			{
				string text = ((i < array.Count) ? array[i].AsString() : "");
				SetNetworkDancer(i, null);
				_pendingDancerNodeNames[i] = text;
			}
		}
		RefreshPendingDancerBatchEligibility();
		ResolvePendingDancerRelations();
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
		if (data.ContainsKey("dancerSyncIds"))
		{
			Godot.Collections.Array array = data["dancerSyncIds"].AsGodotArray();
			for (int i = 0; i < 4; i++)
			{
				int num = ((i < array.Count) ? array[i].AsInt32() : (-1));
				SetNetworkDancer(i, null);
				_pendingDancerSyncIds[i] = num;
			}
			RefreshPendingDancerBatchEligibility();
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

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		ResolvePendingDancerRelations();
	}
}
