using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePogoJackson : TowerDefenseZombie, IJackson, INetworkDancerOwner
{
	private const double PendingDancerResolveIntervalSeconds = 0.25;

	private AttackComponent _attackComponent2;

	private DancingComponent _dancingComponent;

	private string _dancerPacketName = "";

	private readonly int[] _pendingDancerSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingDancerNodeNames = new string[4] { "", "", "", "" };

	private double _pendingDancerResolveRemaining;

	private bool _hasPendingDancerRelations;

	public bool isJump;

	private bool _hasPogo = true;

	public bool pogoPlant;

	public double jumpToPos;

	public int jumpWait = 1;

	public bool isSpawn;

	public double timer = 5.0;

	private StateHandle _pogoStateHandle;

	private bool _roleStateSignalsConnected;

	private bool HasDancingRuntime
	{
		get
		{
			DancingComponent dancingComponent = _dancingComponent;
			if (dancingComponent == null)
			{
				return false;
			}
			return !dancingComponent.IsReleased;
		}
	}

	private bool HasDancingComponent
	{
		get
		{
			if (HasDancingRuntime)
			{
				return _dancingComponent.Lifecycle == ComponentRuntimeLifecycle.Active;
			}
			return false;
		}
	}

	[Export(PropertyHint.None, "")]
	public string dancerPacketName
	{
		get
		{
			return _dancerPacketName;
		}
		set
		{
			_dancerPacketName = value;
			if (!string.IsNullOrEmpty(value) && HasDancingComponent)
			{
				_dancingComponent.dancerPacketName = value;
			}
		}
	}

	public bool hasPogo
	{
		get
		{
			return _hasPogo;
		}
		set
		{
			_hasPogo = value;
			if (!_hasPogo)
			{
				idleAnimeClip = "Walk";
			}
		}
	}

	public void RemoveDancer(TowerDefenseCharacter dancer)
	{
		for (int i = 0; i < 4; i++)
		{
			bool flag = GodotObject.IsInstanceValid(dancer) && dancer.syncId >= 0 && _pendingDancerSyncIds[i] == dancer.syncId;
			if (IsSameDancer(GetDancer(i), dancer) || flag)
			{
				SetNetworkDancer(i, null);
				break;
			}
		}
	}

	private static bool IsSameDancer(TowerDefenseCharacter candidate, TowerDefenseCharacter dancer)
	{
		if (candidate != dancer)
		{
			if (GodotObject.IsInstanceValid(candidate) && GodotObject.IsInstanceValid(dancer) && candidate.syncId >= 0)
			{
				return candidate.syncId == dancer.syncId;
			}
			return false;
		}
		return true;
	}

	public void SetNetworkDancer(int slot, TowerDefenseCharacter dancer)
	{
		if (slot < 0 || slot >= 4)
		{
			return;
		}
		if (!HasDancingRuntime)
		{
			if (GodotObject.IsInstanceValid(dancer))
			{
				_pendingDancerSyncIds[slot] = dancer.syncId;
				_pendingDancerNodeNames[slot] = "";
			}
			else
			{
				_pendingDancerSyncIds[slot] = -1;
				_pendingDancerNodeNames[slot] = "";
			}
			RefreshPendingDancerBatchEligibility();
		}
		else
		{
			_dancingComponent.SetNetworkDancer(slot, dancer);
			_pendingDancerSyncIds[slot] = -1;
			_pendingDancerNodeNames[slot] = "";
			RefreshPendingDancerBatchEligibility();
		}
	}

	private TowerDefenseCharacter GetDancer(int slot)
	{
		if (!HasDancingRuntime)
		{
			return null;
		}
		return _dancingComponent.GetDancer(slot);
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
		if (!HasDancingRuntime)
		{
			_pendingDancerResolveRemaining = 0.25;
			return;
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
		if (HasDancingRuntime)
		{
			for (int i = 0; i < 4; i++)
			{
				_dancingComponent.SetNetworkDancer(i, null);
			}
		}
		System.Array.Fill(_pendingDancerSyncIds, -1);
		System.Array.Fill(_pendingDancerNodeNames, "");
		RefreshPendingDancerBatchEligibility();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_pogoStateHandle = base.StateMachine?.GetStateById("zombie.pogo_jackson.pogo");
			StateHandle pogoStateHandle = _pogoStateHandle;
			if (pogoStateHandle != null && pogoStateHandle.IsValid)
			{
				_pogoStateHandle.Entered += PogoEntered;
				_pogoStateHandle.Exited += PogoExited;
				_pogoStateHandle.PhysicsProcessing += PogoProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_pogoStateHandle != null)
			{
				_pogoStateHandle.Entered -= PogoEntered;
				_pogoStateHandle.Exited -= PogoExited;
				_pogoStateHandle.PhysicsProcessing -= PogoProcessing;
			}
			_pogoStateHandle = null;
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
		if (Engine.IsEditorHint())
		{
			return;
		}
		base.OnLand += Land;
		_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
		_dancingComponent = componentManager.GetRuntime<DancingComponent>();
		if (HasDancingComponent)
		{
			base.sprite.OnAnimeCompleted -= _dancingComponent.AnimeCompleted;
			base.sprite.OnAnimeEvent -= _dancingComponent.AnimeEvent;
			if (!string.IsNullOrEmpty(dancerPacketName))
			{
				_dancingComponent.dancerPacketName = dancerPacketName;
			}
			else
			{
				_dancingComponent.dancerPacketName = "ZombiePogoDancer";
			}
		}
		ySpeed = -300.0;
		ConnectRoleStateSignals();
		RefreshPendingDancerBatchEligibility();
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (base.IsInsideComponentBattlefield && timer > 0.0)
		{
			timer -= delta;
		}
		if (_hasPendingDancerRelations)
		{
			_pendingDancerResolveRemaining -= delta;
			if (!(_pendingDancerResolveRemaining > 0.0))
			{
				ResolvePendingDancerRelations();
			}
		}
	}

	public void PogoEntered()
	{
		base.sprite.SetAnimation("Pogo", loop: true, 0.2);
		if (isGround)
		{
			ySpeed = -300.0;
		}
	}

	public void PogoProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!pogoPlant)
		{
			if (!base.sprite.pause)
			{
				double num = (((double)globalPositionForPhysicsFrame.X > groundRight) ? 2.0 : 1.0);
				globalPositionForPhysicsFrame.X -= (float)(30.0 * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * num * (double)((!base.sprite.playBack) ? 1 : (-1)));
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (!base.sprite.pause && attackComponent.CanAttack())
			{
				pogoPlant = true;
				jumpToPos = globalPositionForPhysicsFrame.X - TowerDefenseManager.Instance.GetMapGridSize().X * base.Scale.X - 10f * base.Scale.X;
			}
		}
		else if (isJump && _attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target) && _attackComponent2.target.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
		{
			instance.ArmorDelete("Pogo");
			hasPogo = false;
			isJump = false;
			AudioManager.Instance.AudioPlay("Bonk");
			Walk();
		}
		if (!isSpawn && timer <= 0.0 && HasDancingComponent && _dancingComponent.CanSpawnDancer())
		{
			base.sprite.SetAnimation("PogoPointUp", loop: false);
			base.sprite.AddAnimation("PogoPointDown", 0.0, loop: false);
			base.sprite.AddAnimation("Pogo", 0.0, loop: false);
			isSpawn = true;
		}
	}

	public void PogoExited()
	{
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		if (HasDancingComponent)
		{
			_dancingComponent.OnWalkEntered();
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		if (HasDancingComponent)
		{
			_dancingComponent.OnAttackProcessing(delta);
		}
	}

	public override void Walk()
	{
		if (hasPogo)
		{
			SendStateEvent("ToPogo");
		}
		else if (die)
		{
			SendStateEvent("ToDie");
		}
		else if (!HasDancingComponent || !_dancingComponent.OnWalk())
		{
			SendStateEvent("ToWalk");
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		if (HasDancingComponent)
		{
			_dancingComponent.OnDieProcessing();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!inGame || !TowerDefenseManager.Instance.IsGameRunning() || !HasDancingComponent)
		{
			return;
		}
		if (clip == _dancingComponent.pointDownAnimeClip)
		{
			isSpawn = false;
			Walk();
			_dancingComponent.RequestIdle();
		}
		else if (clip == _dancingComponent.walkAnimeClip)
		{
			_dancingComponent.walkTime--;
			if (!die && !nearDie)
			{
				if (_dancingComponent.walkTime <= 0)
				{
					if (_dancingComponent.CanSpawnDancer())
					{
						Component();
						_dancingComponent.RequestPoint();
					}
					else
					{
						Component();
						_dancingComponent.RequestDance();
					}
				}
			}
			else
			{
				Die();
			}
		}
		else
		{
			if (!(clip == _dancingComponent.armRiseAnimeClip))
			{
				return;
			}
			if (_dancingComponent.armRiseFlipSprite)
			{
				base.sprite.Scale = new Vector2(0f - base.sprite.Scale.X, base.sprite.Scale.Y);
			}
			_dancingComponent.danceTime--;
			if (!die && !nearDie)
			{
				if (_dancingComponent.danceTime <= 0)
				{
					if (_dancingComponent.CanSpawnDancer())
					{
						Component();
						_dancingComponent.RequestPoint();
					}
					else
					{
						Walk();
						_dancingComponent.RequestIdle();
					}
				}
			}
			else
			{
				Die();
			}
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pogo")
		{
			instance.unUseBuffFlags = 0;
			isJump = false;
			hasPogo = false;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
			if (base.inWater)
			{
				base.groundHeight = 0.0 - base.waterHeight;
			}
			Walk();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!HasDancingComponent || !(command == "spawn") || die || nearDie)
		{
			return;
		}
		SpawnDancer();
		if (GodotObject.IsInstanceValid(_dancingComponent.spotlight))
		{
			_dancingComponent.spotlight.Visible = true;
		}
		if (GodotObject.IsInstanceValid(_dancingComponent.spotlight2))
		{
			_dancingComponent.spotlight2.Visible = true;
		}
		_dancingComponent.ChangeSpotlightColor();
		if (!_dancingComponent.firstSpawn)
		{
			_dancingComponent.firstSpawn = true;
			if (_dancingComponent.spotlightAudioName != "")
			{
				AudioManager.Instance.AudioPlay(_dancingComponent.spotlightAudioName);
			}
		}
	}

	public void SpawnDancer()
	{
		if (!base.IsInsideComponentBattlefield || !TowerDefenseManager.HasGameplayAuthority || !HasDancingComponent)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(_dancingComponent.dancerPacketName);
		if (GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(transformPoint))
		{
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (base.gridPos.Y > 1 && !GodotObject.IsInstanceValid(_dancingComponent.GetDancer(0)))
			{
				SpawnSingleDancer(packetConfig, 0, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y - 1)), base.gridPos - new Vector2I(0, 1));
			}
			if (base.gridPos.Y < mapGridNum.Y && !GodotObject.IsInstanceValid(_dancingComponent.GetDancer(1)))
			{
				SpawnSingleDancer(packetConfig, 1, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y + 1)), base.gridPos + new Vector2I(0, 1));
			}
			if (!GodotObject.IsInstanceValid(_dancingComponent.GetDancer(2)))
			{
				SpawnSingleDancer(packetConfig, 2, logicalGlobalPosition - new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos - new Vector2I(1, 0));
			}
			if (!GodotObject.IsInstanceValid(_dancingComponent.GetDancer(3)))
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
		Dictionary dictionary = CreateDancerSpawnState(slot);
		if (dancer is INetworkSpawnStateReceiver networkSpawnStateReceiver)
		{
			networkSpawnStateReceiver.ImportNetworkSpawnState(dictionary);
		}
		TowerDefenseGroundItemBase.characterNode.CallDeferred("add_child", dancer);
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
		TowerDefenseManager.PublishSpawnedCharacter(_dancingComponent.dancerPacketName, dancer, useCreate: true, 0.0, walkAfterSpawn: false, "", dictionary);
		if (dancer is TowerDefenseZombie towerDefenseZombie)
		{
			towerDefenseZombie.CallDeferred("WalkReady");
		}
	}

	private Dictionary CreateDancerSpawnState(int slot)
	{
		return new Dictionary
		{
			["spawn_invisible"] = base.invisible,
			["spawn_z"] = 600.0,
			["walk_ready_after_spawn"] = true,
			["dancer_parent_sync_id"] = syncId,
			["dancer_slot"] = slot
		};
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		ReleaseDancerOwnership();
	}

	public override Dictionary ExportVariantSave()
	{
		Array<string> array = new Array<string>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			array.Add(GodotObject.IsInstanceValid(dancer) ? dancer.Name.ToString() : _pendingDancerNodeNames[i]);
		}
		return new Dictionary
		{
			{ "hasPogo", hasPogo },
			{ "isJump", isJump },
			{ "pogoPlant", pogoPlant },
			{ "jumpToPos", jumpToPos },
			{ "jumpWait", jumpWait },
			{ "isSpawn", isSpawn },
			{ "timer", timer },
			{ "dancerNodeNames", array }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hasPogo = data.GetValueOrDefault("hasPogo", true).AsBool();
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		pogoPlant = data.GetValueOrDefault("pogoPlant", false).AsBool();
		jumpToPos = data.GetValueOrDefault("jumpToPos", 0.0).AsDouble();
		jumpWait = data.GetValueOrDefault("jumpWait", 1).AsInt32();
		isSpawn = data.GetValueOrDefault("isSpawn", false).AsBool();
		timer = data.GetValueOrDefault("timer", 5.0).AsDouble();
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
		if (_hasPendingDancerRelations)
		{
			ResolvePendingDancerRelations();
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Array<int> array = new Array<int>();
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = GetDancer(i);
			array.Add(GodotObject.IsInstanceValid(dancer) ? dancer.syncId : _pendingDancerSyncIds[i]);
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
			TowerDefenseCharacter dancer = GetDancer(i);
			int num2 = (GodotObject.IsInstanceValid(dancer) ? dancer.syncId : _pendingDancerSyncIds[i]);
			num = num * 31 + num2;
		}
		return num;
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

	public override void InWater()
	{
		base.InWater();
		if (hasPogo)
		{
			base.groundHeight = 0.0;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = false;
			}
		}
	}

	public override void OutWater()
	{
		if (hasPogo)
		{
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
		}
		base.OutWater();
		if (hasPogo)
		{
			ySpeed = -300.0;
		}
	}

	public async void Land()
	{
		if (!hasPogo)
		{
			return;
		}
		isJump = false;
		gravity = 490.0;
		ySpeed = -300.0;
		if (!base.IsRemoteNetworkReplica && pogoPlant)
		{
			isJump = true;
			if (jumpWait > 0)
			{
				jumpWait--;
			}
			else
			{
				jumpWait = 1;
				Tween tween = CreateTween();
				tween.SetEase(Tween.EaseType.InOut);
				tween.SetTrans(Tween.TransitionType.Sine);
				Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
				tween.TweenMethod(to: new Vector2((float)jumpToPos, logicalGlobalPosition.Y), method: Callable.From<Vector2>(base.SetLogicalGlobalPosition), from: logicalGlobalPosition, duration: 0.5);
				ySpeed = -400.0;
				await ToSignal(tween, Tween.SignalName.Finished);
				pogoPlant = false;
			}
			await ToSignal(GetTree().CreateTimer(0.2, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			isJump = false;
		}
	}

	public override bool CanBlock()
	{
		return hasPogo;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	public override void Block(TowerDefenseCharacter target)
	{
		instance.ArmorDelete("Pogo");
		hasPogo = false;
		isJump = false;
		GroundHeightComponent obj = groundHeightComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundHeightComponent.handleWaterHeight = true;
		}
		if (base.inWater)
		{
			base.groundHeight = 0.0 - base.waterHeight;
		}
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}
}
