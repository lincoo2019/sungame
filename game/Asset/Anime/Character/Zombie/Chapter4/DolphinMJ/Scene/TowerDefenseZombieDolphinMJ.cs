using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieDolphinMJ : TowerDefenseZombie, IJackson, INetworkDancerOwner
{
	private const double PendingDancerResolveIntervalSeconds = 0.25;

	private Sprite2D _spotlight2;

	private Sprite2D _spotlight;

	private AttackComponent _attackComponent2;

	[Export(PropertyHint.None, "")]
	public Gradient spotlightGrandient;

	private bool _dolphin = true;

	public bool jumpMove;

	public bool isJump;

	public bool isJumpInWater;

	public bool isBlock;

	public bool audioPlay;

	public double timer = 5.0;

	public Array<TowerDefenseCharacter> dancerList = new Array<TowerDefenseCharacter>();

	private readonly int[] _pendingDancerSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingDancerNodeNames = new string[4] { "", "", "", "" };

	private double _pendingDancerResolveRemaining;

	private bool _hasPendingDancerRelations;

	[Export(PropertyHint.None, "")]
	public string dancerPacketName = "ZombieDolphinDC";

	public bool firstSpawn;

	public bool isSpawn;

	private StateHandle _jumpStateHandle;

	private StateHandle _runStateHandle;

	private bool _roleStateSignalsConnected;

	public bool dolphin
	{
		get
		{
			return _dolphin;
		}
		set
		{
			_dolphin = value;
			if (!_dolphin)
			{
				base.useAttackDps = true;
				attackComponent.attackType = "Eat";
				base.useAttackDps = true;
				base.walkAnimeClip = "Walk";
				base.attackAnimeClip = "Eat";
				base.dieAnimeClip = "Death";
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
		_jumpStateHandle = base.StateMachine?.GetStateById("zombie.dolphin_mj.jump");
		_runStateHandle = base.StateMachine?.GetStateById("zombie.dolphin_mj.run");
		StateHandle jumpStateHandle = _jumpStateHandle;
		if (jumpStateHandle != null && jumpStateHandle.IsValid)
		{
			StateHandle runStateHandle = _runStateHandle;
			if (runStateHandle != null && runStateHandle.IsValid)
			{
				_jumpStateHandle.Entered += JumpEntered;
				_jumpStateHandle.Exited += JumpExited;
				_jumpStateHandle.PhysicsProcessing += JumpProcessing;
				_runStateHandle.Entered += RunEntered;
				_runStateHandle.Exited += RunExited;
				_runStateHandle.PhysicsProcessing += RunProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_jumpStateHandle != null)
			{
				_jumpStateHandle.Entered -= JumpEntered;
				_jumpStateHandle.Exited -= JumpExited;
				_jumpStateHandle.PhysicsProcessing -= JumpProcessing;
			}
			_jumpStateHandle = null;
			if (_runStateHandle != null)
			{
				_runStateHandle.Entered -= RunEntered;
				_runStateHandle.Exited -= RunExited;
				_runStateHandle.PhysicsProcessing -= RunProcessing;
			}
			_runStateHandle = null;
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
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			dancerList.Resize(4);
			base.sprite.OnAnimeStarted += AnimeStarted;
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

	public override void WalkEntered()
	{
		if (jumpMove)
		{
			jumpMove = false;
			if (base.inWater)
			{
				base.sprite.SetAnimation(base.swimAnimeClip);
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
		}
		else if (isBlock)
		{
			isBlock = false;
			if (base.inWater)
			{
				base.sprite.SetAnimation(base.swimAnimeClip);
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
		}
		else if (base.inWater)
		{
			base.sprite.SetAnimation(base.swimAnimeClip, loop: true, 0.2);
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		ActivateGroundMovementAfterStateDelay(base.WalkStateHandle);
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (!audioPlay && (double)GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X < TowerDefenseManager.Instance.GetMapGroundRight())
		{
			AudioManager.Instance.AudioPlay("DolphinAppears");
			audioPlay = true;
		}
		if (!isSpawn && timer <= 0.0 && CanSpawnDancer())
		{
			if (base.inWater)
			{
				base.sprite.SetAnimation("PointUp2", loop: false);
				base.sprite.AddAnimation("PointDown2", 0.75, loop: false);
			}
			else if (dolphin)
			{
				base.sprite.SetAnimation("PointUp3", loop: false);
				base.sprite.AddAnimation("PointDown3", 0.75, loop: false);
			}
			else
			{
				base.sprite.SetAnimation("PointUp4", loop: false);
				base.sprite.AddAnimation("PointDown4", 0.75, loop: false);
			}
			isSpawn = true;
		}
		if (!dolphin && attackComponent.CanAttack() && GodotObject.IsInstanceValid(attackComponent.target) && attackComponent.target is TowerDefenseVase)
		{
			attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		if (!isSpawn && timer <= 0.0 && CanSpawnDancer())
		{
			if (base.inWater)
			{
				base.sprite.SetAnimation("PointUp2", loop: false);
				base.sprite.AddAnimation("PointDown2", 0.75, loop: false);
			}
			else if (dolphin)
			{
				base.sprite.SetAnimation("PointUp3", loop: false);
				base.sprite.AddAnimation("PointDown3", 0.75, loop: false);
			}
			else
			{
				base.sprite.SetAnimation("PointUp4", loop: false);
				base.sprite.AddAnimation("PointDown4", 0.75, loop: false);
			}
			isSpawn = true;
		}
	}

	public override void Walk()
	{
		if (dolphin && base.inWater)
		{
			SendStateEvent("ToRun");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public void RunEntered()
	{
		if (base.inWater)
		{
			if (!inSwimPlay && base.inSwimAnimeClip != "")
			{
				base.sprite.SetAnimation(base.inSwimAnimeClip, loop: false, 0.2);
				base.sprite.AddAnimation("DolphinRun", 0.0);
				inSwimPlay = true;
			}
			else
			{
				base.sprite.SetAnimation("DolphinRun");
			}
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		groundMoveComponent.SetAlive(true);
	}

	public void RunProcessing(double delta)
	{
		if (base.sprite.clip == base.inSwimAnimeClip)
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 0.5;
		}
		if (!nearDie && !TowerDefenseManager.Instance.backZombie)
		{
			if (base.sprite.clip != base.inSwimAnimeClip && (base.sprite.clip == "DolphinRun" || base.sprite.clip == "PointUp1" || base.sprite.clip == "PointDown1") && _attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target))
			{
				SendStateEvent("ToJump");
			}
			if (!isSpawn && timer <= 0.0 && CanSpawnDancer())
			{
				groundMoveComponent.SetAlive(false);
				base.sprite.SetAnimation("PointUp1", loop: false);
				isSpawn = true;
				CompleteDancerSpawnPresentationAsync();
			}
		}
	}

	private async Task CompleteDancerSpawnPresentationAsync()
	{
		if (await WaitForStatePhysicsFramesAsync(_runStateHandle, 2))
		{
			GroundMoveComponent obj = groundMoveComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundMoveComponent.SetAlive(true);
			}
			base.sprite.AddAnimation("PointDown1", 0.0, loop: false);
			base.sprite.AddAnimation("DolphinRun", 0.0);
		}
	}

	public void RunExited()
	{
		groundMoveComponent.SetAlive(false);
	}

	public void JumpEntered()
	{
		shadowSprite.Visible = false;
		base.sprite.SetAnimation("DolphinJump", loop: false, 0.2);
		instance.collisionFlags = 0;
		instance.maskFlags = 0;
	}

	public void JumpProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!TowerDefenseManager.Instance.backZombie)
		{
			globalPositionForPhysicsFrame.X -= base.Scale.X * (float)delta * (float)base.sprite.timeScale * 16f;
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		if (isJump)
		{
			TowerDefenseCharacter towerDefenseCharacter = _attackComponent2?.target;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.isDestroy && towerDefenseCharacter.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
			{
				dolphin = false;
				globalPositionForPhysicsFrame.X = towerDefenseCharacter.GetLogicalGlobalPosition().X + 40f;
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
				base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPositionForPhysicsFrame);
				base.waterHeight = 48.0;
				base.groundHeight = 0.0 - base.waterHeight;
				base.z = base.groundHeight;
				spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
				base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
				isBlock = true;
				AudioManager.Instance.AudioPlay("Bonk");
				Walk();
			}
		}
	}

	public void JumpExited()
	{
		isJump = false;
		if (!base.inWater)
		{
			shadowSprite.Visible = !base.invisible;
		}
		instance.collisionFlags = 1;
		instance.maskFlags = 1;
	}

	public override void InWater()
	{
		base.InWater();
		if (!dolphin)
		{
			base.sprite.offset = new Vector2(24f, -97f);
		}
		base.useAttackDps = true;
	}

	public override void OutWater()
	{
		base.OutWater();
		CreateTween().TweenProperty(base.sprite, "offset", new Vector2(-40f, -92f), 0.25);
		if (dolphin && !base.IsRemoteNetworkReplica)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 30f;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
		base.useAttackDps = !dolphin;
		base.waterHeight = 0.0;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		base.sprite.offset = new Vector2(-40f, -92f);
		if (base.inWater)
		{
			base.waterHeight = 60.0;
			base.groundHeight = -60.0;
			base.z = -60.0;
			Tween tween = CreateTween();
			tween.SetParallel();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenProperty(this, "groundHeight", -100.0, 1.0);
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		switch (command)
		{
		case "hit":
			if (!die && !nearDie && !base.sprite.pause)
			{
				attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
			break;
		case "audio":
			AudioManager.Instance.AudioPlay("DolphinreforeJumping");
			break;
		case "check":
			isJump = true;
			break;
		case "jumpOver":
			dolphin = false;
			break;
		case "spawn":
			if (!die && !nearDie)
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
			break;
		}
	}

	public void AnimeStarted(string clip)
	{
		if (clip == "DolphinRun")
		{
			base.sprite.offset = new Vector2(24f, base.sprite.offset.Y);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		switch (clip)
		{
		case "PointDown1":
			isSpawn = false;
			break;
		case "PointDown2":
		case "PointDown3":
		case "PointDown4":
			isSpawn = false;
			Walk();
			break;
		case "DolphinJump":
			if (isJump)
			{
				jumpMove = true;
				base.waterHeight = 48.0;
				base.groundHeight = 0.0 - base.waterHeight;
				base.z = base.groundHeight;
				spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
				base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
				if (!base.IsRemoteNetworkReplica && !TowerDefenseManager.Instance.backZombie)
				{
					Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition();
					logicalGlobalPosition2.X -= base.Scale.X * transformPoint.Scale.X * 98f;
					SetLogicalGlobalPosition(logicalGlobalPosition2);
				}
				base.sprite.QueueRedraw();
				Walk();
			}
			break;
		case "JumpInWater":
			if (!base.IsRemoteNetworkReplica && !TowerDefenseManager.Instance.backZombie)
			{
				Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
				logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 64f;
				SetLogicalGlobalPosition(logicalGlobalPosition);
			}
			base.sprite.offset = new Vector2(24f, base.sprite.offset.Y);
			break;
		}
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
		if (GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(transformPoint))
		{
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (base.gridPos.Y > 1 && CanReplaceDancer(0))
			{
				SpawnSingleDancer(packetConfig, 0, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y - 1)), base.gridPos - new Vector2I(0, 1));
			}
			if (base.gridPos.Y < mapGridNum.Y && CanReplaceDancer(1))
			{
				SpawnSingleDancer(packetConfig, 1, new Vector2(logicalGlobalPosition.X, (float)TowerDefenseManager.GetMapLineY(base.gridPos.Y + 1)), base.gridPos + new Vector2I(0, 1));
			}
			if (CanReplaceDancer(2))
			{
				SpawnSingleDancer(packetConfig, 2, logicalGlobalPosition - new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos - new Vector2I(1, 0));
			}
			if (CanReplaceDancer(3))
			{
				SpawnSingleDancer(packetConfig, 3, logicalGlobalPosition + new Vector2(mapGridSize.X * 1.25f, 0f), base.gridPos + new Vector2I(1, 0));
			}
		}
	}

	private bool CanReplaceDancer(int slot)
	{
		TowerDefenseCharacter towerDefenseCharacter = ((dancerList.Count > slot) ? dancerList[slot] : null);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die)
		{
			return towerDefenseCharacter.nearDie;
		}
		return true;
	}

	private void SpawnSingleDancer(TowerDefensePacketConfig packetConfig, int slot, Vector2 position, Vector2I dancerGridPos)
	{
		TowerDefenseCharacter dancer = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, position, dancerGridPos) : packetConfig.Create(position, dancerGridPos));
		if (!GodotObject.IsInstanceValid(dancer))
		{
			return;
		}
		bool isDolphinDancer = dancerPacketName == "ZombieDolphinDC";
		Dictionary dictionary = CreateDancerSpawnState(slot, isDolphinDancer);
		if (dancer is INetworkSpawnStateReceiver networkSpawnStateReceiver)
		{
			networkSpawnStateReceiver.ImportNetworkSpawnState(dictionary);
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
		TowerDefenseManager.PublishSpawnedCharacter(dancerPacketName, dancer, useCreate: true, 1.5, walkAfterSpawn: false, "", dictionary);
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(dancer))
			{
				if (isDolphinDancer)
				{
					dancer.Rise(1.5, 0.0, createDirt: true, changeState: false);
				}
				else
				{
					dancer.Rise(1.5);
				}
			}
		}).CallDeferred();
	}

	private Dictionary CreateDancerSpawnState(int slot, bool isDolphinDancer)
	{
		Dictionary dictionary = new Dictionary
		{
			["spawn_invisible"] = base.invisible,
			["dancer_parent_sync_id"] = syncId,
			["dancer_slot"] = slot
		};
		if (isDolphinDancer)
		{
			dictionary["rise_change_state"] = false;
			dictionary["dolphin"] = dolphin;
			dictionary["in_swim_play"] = true;
		}
		return dictionary;
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
			{ "dolphin", dolphin },
			{ "jumpMove", jumpMove },
			{ "isJump", isJump },
			{ "isBlock", isBlock },
			{ "firstSpawn", firstSpawn },
			{ "isSpawn", isSpawn },
			{ "timer", timer },
			{ "dancerNodeNames", array }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		dolphin = data.GetValueOrDefault("dolphin", true).AsBool();
		jumpMove = data.GetValueOrDefault("jumpMove", false).AsBool();
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		isBlock = data.GetValueOrDefault("isBlock", false).AsBool();
		firstSpawn = data.GetValueOrDefault("firstSpawn", false).AsBool();
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

	public override bool CanBlock()
	{
		return dolphin;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	public override void Block(TowerDefenseCharacter target)
	{
		dolphin = false;
		base.waterHeight = 48.0;
		base.groundHeight = 0.0 - base.waterHeight;
		base.z = base.groundHeight;
		spriteGroup.Position = new Vector2(spriteGroup.Position.X, 0f - (float)base.z);
		base.sprite.offset = new Vector2(-40f, base.sprite.offset.Y);
		isBlock = true;
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}
}
