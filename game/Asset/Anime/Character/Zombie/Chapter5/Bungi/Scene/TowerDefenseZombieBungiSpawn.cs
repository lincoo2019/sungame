using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBungiSpawn : TowerDefenseZombie
{
	[Export(PropertyHint.None, "")]
	public string characterName = "";

	public TowerDefenseCharacterOverride override_;

	public bool canBlock = true;

	[Export(PropertyHint.None, "")]
	public int waveOperationId = -1;

	public TowerDefenseBattleFeatureWave waveOperationOwner;

	public TowerDefenseControlNew battleOperationOwner;

	public int battleOperationId = -1;

	public Tween dropTween;

	public TowerDefenseCharacter character;

	private Tween _riseTween;

	private StateHandle _dropStateHandle;

	private StateHandle _riseStateHandle;

	private bool _roleStateSignalsConnected;

	private bool _payloadPlaced;

	private bool _restoredFromProgress;

	private bool _payloadGravitySuspended;

	private bool _payloadGravityUseBeforeCarry = true;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride @override
	{
		get
		{
			return override_;
		}
		set
		{
			override_ = value;
		}
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "override_")
		{
			override_ = value.As<TowerDefenseCharacterOverride>();
			return true;
		}
		return base._Set(property, value);
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
		_dropStateHandle = base.StateMachine?.GetStateById("zombie.bungi_spawn.drop");
		_riseStateHandle = base.StateMachine?.GetStateById("zombie.bungi_spawn.rise");
		StateHandle dropStateHandle = _dropStateHandle;
		if (dropStateHandle != null && dropStateHandle.IsValid)
		{
			StateHandle riseStateHandle = _riseStateHandle;
			if (riseStateHandle != null && riseStateHandle.IsValid)
			{
				_dropStateHandle.Entered += DropEntered;
				_dropStateHandle.Exited += DropExited;
				_dropStateHandle.PhysicsProcessing += DropProcessing;
				_riseStateHandle.Entered += RiseEntered;
				_riseStateHandle.Exited += RiseExited;
				_riseStateHandle.PhysicsProcessing += RiseProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_dropStateHandle != null)
			{
				_dropStateHandle.Entered -= DropEntered;
				_dropStateHandle.Exited -= DropExited;
				_dropStateHandle.PhysicsProcessing -= DropProcessing;
			}
			_dropStateHandle = null;
			if (_riseStateHandle != null)
			{
				_riseStateHandle.Entered -= RiseEntered;
				_riseStateHandle.Exited -= RiseExited;
				_riseStateHandle.PhysicsProcessing -= RiseProcessing;
			}
			_riseStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void PrepareForProgressRestore()
	{
		base.PrepareForProgressRestore();
		_restoredFromProgress = true;
	}

	public override async void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		ConnectRoleStateSignals();
		targetRegistrationComponent.canCarry = false;
		AddToGroup("Bungi", persistent: true);
		base.z = 600.0;
		isGround = false;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!AreSpawnOperationsCurrent())
		{
			AbortSpawn();
			return;
		}
		if (_restoredFromProgress)
		{
			if (!GodotObject.IsInstanceValid(character))
			{
				AbortSpawn();
			}
			else if (!_payloadPlaced)
			{
				SuspendPayloadVerticalPhysics();
			}
			return;
		}
		shadowSprite.Visible = !base.invisible;
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			Walk();
			return;
		}
		try
		{
			if (characterName != "")
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(characterName);
				if (GodotObject.IsInstanceValid(packetConfig))
				{
					character = packetConfig.Create(GetLogicalGlobalPosition(), base.gridPos);
					if (!GodotObject.IsInstanceValid(character))
					{
						throw new InvalidOperationException("Failed to create bungee payload '" + characterName + "'.");
					}
					SuspendPayloadVerticalPhysics();
					character.z = 600.0;
					character.isGround = false;
					TowerDefenseGroundItemBase.characterNode.AddChild(character, forceReadableName: false, InternalMode.Disabled);
					if (instance.hypnoses)
					{
						character.Hypnoses();
					}
					character.instance.canBeCollection = false;
					character.shadowSprite.Visible = false;
					character.PreSpawn();
					if (character is TowerDefenseZombie towerDefenseZombie)
					{
						AttackComponent obj = towerDefenseZombie.attackComponent;
						if (obj != null && !obj.IsReleased)
						{
							towerDefenseZombie.attackComponent.alive = false;
						}
						towerDefenseZombie.instance.collisionFlags = 0;
						towerDefenseZombie.instance.maskFlags = 0;
						if (towerDefenseZombie.inWater)
						{
							WaterInteractionComponent obj2 = towerDefenseZombie.waterInteractionComponent;
							if (obj2 != null && !obj2.IsReleased)
							{
								if (GodotObject.IsInstanceValid(towerDefenseZombie.waterInteractionComponent.activeTween))
								{
									towerDefenseZombie.waterInteractionComponent.activeTween.Kill();
								}
								towerDefenseZombie.waterInteractionComponent.isInWater = false;
							}
							towerDefenseZombie.SetSpriteGroupShaderParameter("discardDownPos", 10000.0);
							if (GodotObject.IsInstanceValid(towerDefenseZombie.waterLineSprite))
							{
								towerDefenseZombie.waterLineSprite.Visible = false;
							}
							if (GodotObject.IsInstanceValid(towerDefenseZombie.duckytobeSprite))
							{
								towerDefenseZombie.duckytobeSprite.Visible = false;
							}
						}
					}
					if (GodotObject.IsInstanceValid(override_))
					{
						override_.ExecuteCharacter(character);
					}
					character.SetLogicalGlobalPosition(GetLogicalGlobalPosition());
					if (character is TowerDefenseZombie towerDefenseZombie2 && GodotObject.IsInstanceValid(waveOperationOwner))
					{
						waveOperationOwner.AddSpawnCharacter(towerDefenseZombie2);
					}
				}
			}
			if (!GodotObject.IsInstanceValid(character))
			{
				throw new InvalidOperationException("Invalid bungee payload '" + characterName + "'.");
			}
			Walk();
		}
		catch (Exception value)
		{
			GD.PushError($"[BungiSpawn] Failed to initialize '{characterName}': {value}");
			AbortSpawn();
		}
	}

	public override void _ExitTree()
	{
		CancelDropPresentation();
		CancelRisePresentation();
		DisconnectRoleStateSignals();
		CleanupUnplacedPayload();
		CompleteSpawnOperations();
		base._ExitTree();
	}

	private void CompleteSpawnOperations()
	{
		if (battleOperationId >= 0 && GodotObject.IsInstanceValid(battleOperationOwner))
		{
			battleOperationOwner.CompletePendingBattleOperation(battleOperationId);
		}
		if (waveOperationId >= 0 && GodotObject.IsInstanceValid(waveOperationOwner))
		{
			waveOperationOwner.CompletePendingSpawnOperation(waveOperationId);
		}
		battleOperationId = -1;
		waveOperationId = -1;
		battleOperationOwner = null;
		waveOperationOwner = null;
	}

	private bool AreSpawnOperationsCurrent()
	{
		if (battleOperationId >= 0 && (!GodotObject.IsInstanceValid(battleOperationOwner) || !battleOperationOwner.IsPendingBattleOperationCurrent(battleOperationId)))
		{
			return false;
		}
		if (waveOperationId >= 0 && (!GodotObject.IsInstanceValid(waveOperationOwner) || !waveOperationOwner.IsPendingSpawnOperationCurrent(waveOperationId)))
		{
			return false;
		}
		return true;
	}

	private void AbortSpawn()
	{
		CleanupUnplacedPayload();
		CompleteSpawnOperations();
		if (!IsQueuedForDeletion())
		{
			QueueFree();
		}
	}

	private void CleanupUnplacedPayload()
	{
		if (!_payloadPlaced && GodotObject.IsInstanceValid(character) && !character.IsQueuedForDeletion())
		{
			TowerDefenseCharacter towerDefenseCharacter = character;
			character = null;
			if (towerDefenseCharacter.GetParent() == this)
			{
				towerDefenseCharacter.QueueFree();
			}
			else
			{
				towerDefenseCharacter.Destroy();
			}
		}
	}

	private void SuspendPayloadVerticalPhysics()
	{
		if (!_payloadPlaced && GodotObject.IsInstanceValid(character))
		{
			if (!_payloadGravitySuspended)
			{
				_payloadGravityUseBeforeCarry = character.gravityUse;
				_payloadGravitySuspended = true;
			}
			character.gravityUse = false;
			character.ySpeed = 0.0;
		}
	}

	private void RestorePayloadVerticalPhysics()
	{
		if (GodotObject.IsInstanceValid(character))
		{
			if (_payloadGravitySuspended)
			{
				character.gravityUse = _payloadGravityUseBeforeCarry;
			}
			character.ySpeed = 0.0;
			_payloadGravitySuspended = false;
		}
	}

	private void SetCarriedZ(double height)
	{
		base.z = height;
		if (GodotObject.IsInstanceValid(character))
		{
			character.SetLogicalGlobalPosition(GetLogicalGlobalPosition());
			character.z = height;
		}
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		Destroy();
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		Destroy();
	}

	public override void Walk()
	{
		ActivateGameplayProcessing();
		SendStateEvent("ToDrop");
	}

	public void DropEntered()
	{
		AudioManager.Instance.AudioPlay("BungeeScream");
		base.z = 600.0;
		isGround = false;
		if (GodotObject.IsInstanceValid(character))
		{
			SuspendPayloadVerticalPhysics();
			character.SetLogicalGlobalPosition(GetLogicalGlobalPosition());
			character.z = 600.0;
			character.isGround = false;
		}
		dropTween = CreateTween();
		dropTween.SetParallel();
		dropTween.SetEase(Tween.EaseType.Out);
		dropTween.SetTrans(Tween.TransitionType.Cubic);
		double num = 0.0;
		if (GodotObject.IsInstanceValid(cell))
		{
			num = cell.GetGroundHeight();
		}
		dropTween.TweenMethod(Callable.From<double>(SetCarriedZ), 600.0, num, 0.75);
		base.sprite.SetAnimation("Drop", loop: true, 0.2);
		CompleteDropAsync();
	}

	private async Task CompleteDropAsync()
	{
		if (await WaitForStateDelayAsync(_dropStateHandle, 0.75))
		{
			isGround = true;
			if (PutCharacter())
			{
				SendStateEvent("ToRise");
			}
		}
	}

	public void DropProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void DropExited()
	{
		CancelDropPresentation();
	}

	private void CancelDropPresentation()
	{
		if (GodotObject.IsInstanceValid(dropTween))
		{
			dropTween.Kill();
		}
		dropTween = null;
	}

	public void RiseEntered()
	{
		base.sprite.SetAnimation("Rise", loop: true, 0.2);
		isGround = false;
		double duration = 1.5;
		if (!canBlock)
		{
			duration = 0.75;
		}
		_riseTween = CreateTween();
		_riseTween.SetParallel();
		_riseTween.SetEase(Tween.EaseType.Out);
		_riseTween.SetTrans(Tween.TransitionType.Cubic);
		if (!canBlock)
		{
			_riseTween.TweenMethod(Callable.From<double>(SetCarriedZ), base.z, 600.0, duration);
		}
		else
		{
			_riseTween.TweenProperty(this, "z", 600, duration);
		}
		CompleteRiseAsync(duration);
	}

	private async Task CompleteRiseAsync(double duration)
	{
		if (await WaitForStateDelayAsync(_riseStateHandle, duration))
		{
			Destroy();
		}
	}

	public void RiseProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void RiseExited()
	{
		CancelRisePresentation();
	}

	private void CancelRisePresentation()
	{
		if (GodotObject.IsInstanceValid(_riseTween))
		{
			_riseTween.Kill();
		}
		_riseTween = null;
	}

	public override bool CanBlock()
	{
		if (canBlock)
		{
			return base.z <= 50.0;
		}
		return false;
	}

	public override void Block(TowerDefenseCharacter target)
	{
		CancelDropPresentation();
		canBlock = false;
		instance.invincible = true;
		SendStateEvent("ToRise");
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "characterName", characterName },
			{ "canBlock", canBlock },
			{ "payloadPlaced", _payloadPlaced },
			{ "payloadGravitySuspended", _payloadGravitySuspended },
			{ "payloadGravityUseBeforeCarry", _payloadGravityUseBeforeCarry }
		};
		if (GodotObject.IsInstanceValid(character))
		{
			dictionary["payloadNodeName"] = character.Name;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		characterName = data.GetValueOrDefault("characterName", characterName).AsString();
		canBlock = data.GetValueOrDefault("canBlock", canBlock).AsBool();
		_payloadPlaced = data.GetValueOrDefault("payloadPlaced", false).AsBool();
		_payloadGravitySuspended = data.GetValueOrDefault("payloadGravitySuspended", false).AsBool();
		_payloadGravityUseBeforeCarry = data.GetValueOrDefault("payloadGravityUseBeforeCarry", true).AsBool();
		character = null;
		string text = data.GetValueOrDefault("payloadNodeName", "").AsString();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		TowerDefenseCharacter nodeOrNull = node2D.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text));
		if (GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull != this)
		{
			character = nodeOrNull;
			if (!_payloadPlaced)
			{
				SuspendPayloadVerticalPhysics();
			}
		}
	}

	public bool PutCharacter()
	{
		if (!AreSpawnOperationsCurrent() || !GodotObject.IsInstanceValid(character))
		{
			AbortSpawn();
			return false;
		}
		try
		{
			character.gridPos = base.gridPos;
			RestorePayloadVerticalPhysics();
			if (character.GetParent() != TowerDefenseGroundItemBase.characterNode)
			{
				character.Reparent(TowerDefenseGroundItemBase.characterNode);
			}
			character.ActivateGameplayProcessing();
			if (character is TowerDefenseZombie towerDefenseZombie)
			{
				GroundMoveComponent obj = towerDefenseZombie.groundMoveComponent;
				if (obj != null && !obj.IsReleased)
				{
					towerDefenseZombie.groundMoveComponent.ConnectGroundNodeSignals();
				}
			}
			character.isGround = true;
			if (GodotObject.IsInstanceValid(character.cell))
			{
				character.groundHeight = character.cell.GetGroundHeight();
				character.z = character.groundHeight;
			}
			if (GodotObject.IsInstanceValid(cell) && !cell.isWater)
			{
				character.shadowSprite.Visible = !character.invisible;
			}
			character.instance.canBeCollection = true;
			if (character is TowerDefenseZombie towerDefenseZombie2)
			{
				AttackComponent obj2 = towerDefenseZombie2.attackComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					towerDefenseZombie2.attackComponent.alive = true;
				}
				towerDefenseZombie2.instance.collisionFlags = 1;
				towerDefenseZombie2.instance.maskFlags = 1;
			}
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(base.gridPos);
			character.SetLogicalGlobalPosition(mapCellPlantPos);
			character.shadowSprite.Scale = character.shadowComponent.saveShadowScale;
			character.shadowComponent.saveShadowPosition = mapCellPlantPos + new Vector2(12f, 36f);
			character.shadowSprite.GlobalPosition = mapCellPlantPos + new Vector2(12f, 36f);
			if (GodotObject.IsInstanceValid(cell) && cell.isWater && character is TowerDefenseZombie { inWater: not false } towerDefenseZombie3)
			{
				WaterInteractionComponent obj3 = towerDefenseZombie3.waterInteractionComponent;
				if (obj3 != null && !obj3.IsReleased)
				{
					towerDefenseZombie3.waterInteractionComponent.InWaterDiscardSet();
				}
				if (GodotObject.IsInstanceValid(towerDefenseZombie3.waterLineSprite))
				{
					towerDefenseZombie3.waterLineSprite.Visible = true;
				}
				if (GodotObject.IsInstanceValid(towerDefenseZombie3.duckytobeSprite))
				{
					towerDefenseZombie3.duckytobeSprite.Visible = true;
				}
			}
			if (GodotObject.IsInstanceValid(cell) && character is TowerDefensePlant towerDefensePlant)
			{
				cell.CharacterPlant(towerDefensePlant.packet, towerDefensePlant);
			}
			if (TowerDefenseManager.Instance != null)
			{
				TowerDefenseManager.Instance.CharacterRegister(character);
			}
			if (TowerDefenseManager.Instance.IsGameRunning() && character is TowerDefenseZombie towerDefenseZombie4)
			{
				towerDefenseZombie4.Walk();
			}
			_payloadPlaced = true;
			CompleteSpawnOperations();
			return true;
		}
		catch (Exception value)
		{
			GD.PushError($"[BungiSpawn] Failed to place '{characterName}': {value}");
			AbortSpawn();
			return false;
		}
	}
}
