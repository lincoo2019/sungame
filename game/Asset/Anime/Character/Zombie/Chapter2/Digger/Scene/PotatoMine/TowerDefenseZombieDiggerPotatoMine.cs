using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieDiggerPotatoMine : TowerDefenseZombie
{
	private StateHandle _digStateHandle;

	private StateHandle _drillStateHandle;

	private StateHandle _landStateHandle;

	private bool _stateSignalsConnected;

	private static PackedScene _DIGGER_RISING_DIRT;

	private PotatoComponent _potatoComponent;

	[Export(PropertyHint.None, "")]
	public double readyTime = 5.0;

	public double speed = 50.0;

	public bool digOver;

	private static PackedScene DIGGER_RISING_DIRT => _DIGGER_RISING_DIRT ?? (_DIGGER_RISING_DIRT = GD.Load<PackedScene>("uid://bjev0ulao283j"));

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
		_digStateHandle = base.StateMachine?.GetStateById("zombie.digger.dig");
		_drillStateHandle = base.StateMachine?.GetStateById("zombie.digger.drill");
		_landStateHandle = base.StateMachine?.GetStateById("zombie.digger.land");
		StateHandle digStateHandle = _digStateHandle;
		if (digStateHandle == null || !digStateHandle.IsValid)
		{
			return;
		}
		StateHandle drillStateHandle = _drillStateHandle;
		if (drillStateHandle != null && drillStateHandle.IsValid)
		{
			StateHandle landStateHandle = _landStateHandle;
			if (landStateHandle != null && landStateHandle.IsValid)
			{
				_digStateHandle.Entered += DigEntered;
				_digStateHandle.Exited += DigExited;
				_digStateHandle.PhysicsProcessing += DigProcessing;
				_drillStateHandle.Entered += DrillEntered;
				_drillStateHandle.Exited += DrillExited;
				_drillStateHandle.PhysicsProcessing += DrillProcessing;
				_landStateHandle.Entered += LandEntered;
				_landStateHandle.Exited += LandExited;
				_landStateHandle.PhysicsProcessing += LandProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_digStateHandle != null)
			{
				_digStateHandle.Entered -= DigEntered;
				_digStateHandle.Exited -= DigExited;
				_digStateHandle.PhysicsProcessing -= DigProcessing;
			}
			_digStateHandle = null;
			if (_drillStateHandle != null)
			{
				_drillStateHandle.Entered -= DrillEntered;
				_drillStateHandle.Exited -= DrillExited;
				_drillStateHandle.PhysicsProcessing -= DrillProcessing;
			}
			_drillStateHandle = null;
			if (_landStateHandle != null)
			{
				_landStateHandle.Entered -= LandEntered;
				_landStateHandle.Exited -= LandExited;
				_landStateHandle.PhysicsProcessing -= LandProcessing;
			}
			_landStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.readyTime = (float)readyTime;
			}
			instance.collisionFlags = 17;
			instance.maskFlags = 16;
			ConnectStateSignals();
		}
	}

	public virtual void DigEntered()
	{
		instance.collisionFlags = 17;
		instance.maskFlags = 16;
		base.sprite.SetAnimation("Dig");
		AdobeAnimateSpriteBase nodeOrNull = base.sprite.GetNodeOrNull<AdobeAnimateSpriteBase>("Head");
		if (nodeOrNull != null)
		{
			nodeOrNull.offset = new Vector2(-58f, -58f);
			nodeOrNull.offsetRotate = 9.4;
		}
		SetDigShadowVisible(visible: false);
	}

	public virtual void DigProcessing(double delta)
	{
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (attackComponent.CanAttack())
		{
			if (!nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps && GodotObject.IsInstanceValid(attackComponent.target))
			{
				if ((attackComponent.target.instance.collisionFlags & 0x10) != 0)
				{
					attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
				}
				else
				{
					attackComponent.target = null;
				}
			}
		}
		else if (!base.sprite.pause)
		{
			double num = speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1));
			if ((double)globalPositionForPhysicsFrame.X > TowerDefenseManager.Instance.GetMapGroundRight())
			{
				num *= 2.0;
			}
			globalPositionForPhysicsFrame.X -= (float)num;
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		base.sprite.timeScale = timeScale * 1.0;
		if (!digOver && (double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 30.0)
		{
			digOver = true;
			SendStateEvent("ToDrill");
			instance.ArmorDelete("Pick");
			if (!instance.hypnoses)
			{
				base.Scale = new Vector2(0f - base.Scale.X, base.Scale.Y);
			}
		}
	}

	public virtual void DigExited()
	{
		instance.unUseBuffFlags = 0;
	}

	public virtual void DrillEntered()
	{
		AdobeAnimateSpriteBase nodeOrNull = base.sprite.GetNodeOrNull<AdobeAnimateSpriteBase>("Head");
		if (nodeOrNull != null)
		{
			nodeOrNull.offset = new Vector2(-58f, -30f);
			nodeOrNull.offsetRotate = 9.1;
		}
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		SetDigShadowVisible(!base.invisible);
		CreateEffect();
		Rise(0.5, 0.0, createDirt: false, changeState: false);
		base.sprite.SetAnimation("Drill2", loop: true, 0.2);
	}

	public virtual void DrillProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void DrillExited()
	{
	}

	public virtual void LandEntered()
	{
		base.sprite.SetAnimation("Land", loop: false);
		base.sprite.AddAnimation("Dizzy", 0.0, loop: false, 0.2);
	}

	public virtual void LandProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public virtual void LandExited()
	{
	}

	private void SetDigShadowVisible(bool visible)
	{
		ShadowComponent obj = shadowComponent;
		if (obj != null && !obj.IsReleased)
		{
			shadowComponent.SetShadowVisible(visible);
		}
		else
		{
			shadowSprite.Visible = visible;
		}
	}

	public override void Walk()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else if (digOver)
		{
			base.Walk();
		}
		else
		{
			SendStateEvent("ToDig");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (die)
		{
			return;
		}
		if (!(clip == "Drill2"))
		{
			if (clip == "Dizzy")
			{
				ArmPotatoMineAfterBurrow();
			}
		}
		else
		{
			SendStateEvent("ToLand");
		}
	}

	private void ArmPotatoMineAfterBurrow()
	{
		if (die)
		{
			SendStateEvent("ToDie");
			return;
		}
		digOver = true;
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		SetDigShadowVisible(!base.invisible);
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.alive = false;
			attackComponent.target = null;
		}
		Component();
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.ReadyRise();
		}
		SendStateEvent("ToWalk");
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pick" && !(instance.hitpoints <= instance.hitpointsNearDeath) && !digOver)
		{
			digOver = true;
			SendStateEvent("ToDrill");
		}
	}

	public void CreateEffect()
	{
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(DIGGER_RISING_DIRT, base.gridPos);
		towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(shadowSprite) - new Vector2(15f, 0f);
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
	}

	public override bool CanDiggerBlock()
	{
		if (!die)
		{
			return !digOver;
		}
		return false;
	}

	public override void BlockDigger(TowerDefenseCharacter target)
	{
		if (die)
		{
			SendStateEvent("ToDie");
			return;
		}
		digOver = true;
		SendStateEvent("ToDrill");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "speed", speed },
			{ "digOver", digOver }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		speed = data.GetValueOrDefault("speed", 50.0).AsDouble();
		digOver = data.GetValueOrDefault("digOver", false).AsBool();
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			DamagePartCreate("Head", base.sprite.GetNodeOrNull<AdobeAnimateSpriteBase>("Head"), new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPotatoMine");
		if (cell.CanPacketPlant(packetConfig, noLimit: true))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos, playAudio: true, noLimit: true);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantPotatoMine", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
