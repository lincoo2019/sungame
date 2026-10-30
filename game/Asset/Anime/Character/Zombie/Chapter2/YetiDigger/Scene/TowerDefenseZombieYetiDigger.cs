using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieYetiDigger : TowerDefenseZombie
{
	private StateHandle _digStateHandle;

	private StateHandle _drillStateHandle;

	private StateHandle _landStateHandle;

	private bool _stateSignalsConnected;

	private static PackedScene _DIGGER_RISING_DIRT;

	public double speed = 50.0;

	public bool digOver;

	private static PackedScene DIGGER_RISING_DIRT => _DIGGER_RISING_DIRT ?? (_DIGGER_RISING_DIRT = GD.Load<PackedScene>("uid://bjev0ulao283j"));

	private bool RequestEmergence()
	{
		if (!SendStateEvent("ToDrill"))
		{
			return false;
		}
		digOver = true;
		return true;
	}

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
		_digStateHandle = base.StateMachine?.GetStateById("zombie.yeti_digger.dig");
		_drillStateHandle = base.StateMachine?.GetStateById("zombie.yeti_digger.drill");
		_landStateHandle = base.StateMachine?.GetStateById("zombie.yeti_digger.land");
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
			instance.collisionFlags = 16;
			instance.maskFlags = 16;
			ConnectStateSignals();
		}
	}

	public void DigEntered()
	{
		instance.collisionFlags = 16;
		instance.maskFlags = 16;
		base.sprite.SetAnimation("Dig");
		SetDigShadowVisible(visible: false);
	}

	public void DigProcessing(double delta)
	{
		if (base.IsRemoteNetworkReplica)
		{
			base.sprite.timeScale = timeScale;
			return;
		}
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
		if ((double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + (double)(TowerDefenseManager.Instance.GetMapGridSize().X * 1.5f) && !digOver && RequestEmergence())
		{
			instance.ArmorDelete("Pick");
			if (!instance.hypnoses)
			{
				TurnAroundAndRefreshGroundMove();
			}
		}
	}

	public void DigExited()
	{
		instance.unUseBuffFlags = 0;
	}

	public void DrillEntered()
	{
		digOver = true;
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		SetDigShadowVisible(!base.invisible);
		CreateEffect();
		Rise(0.75, 0.0, createDirt: false, changeState: false);
		base.sprite.SetAnimation("Drill");
	}

	public void DrillProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void DrillExited()
	{
	}

	public void LandEntered()
	{
		base.sprite.SetAnimation("Land", loop: false);
		base.sprite.AddAnimation("Vertigo", 0.0, loop: false, 0.2);
	}

	public void LandProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.5;
	}

	public void LandExited()
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
		if (digOver)
		{
			if ((instance.collisionFlags & 0x10) != 0)
			{
				SendStateEvent("ToDrill");
			}
			else
			{
				base.Walk();
			}
		}
		else
		{
			SendStateEvent("ToDig");
		}
	}

	public override async void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Drill"))
		{
			if (clip == "Vertigo")
			{
				WalkWithoutTransitionDelay();
				await ToSignal(GetTree().CreateTimer(10.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
				if (!instance.hypnoses)
				{
					TurnAroundAndRefreshGroundMove();
				}
			}
		}
		else
		{
			SendStateEvent("ToLand");
		}
	}

	internal void TurnAroundAndRefreshGroundMove()
	{
		base.Scale = new Vector2(0f - base.Scale.X, base.Scale.Y);
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.RefreshDirectionCache();
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pick" && !digOver)
		{
			RequestEmergence();
		}
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

	public override async void DestroySet()
	{
		if ((!Global.Instance.isEditor || !(Global.Instance.enterLevelMode == "DiyLevel")) && !(Global.Instance.enterLevelMode == "LoadLevel") && !(Global.Instance.enterLevelMode == "OnlineLevel"))
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			float num = GD.Randf();
			if ((double)num <= 0.02)
			{
				TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, logicalGlobalPosition - new Vector2(0f, 40f), 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0).gridPos = base.gridPos;
			}
			else if ((double)num < 0.2)
			{
				TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition - new Vector2(0f, 40f), 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0).gridPos = base.gridPos;
			}
			else
			{
				TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, logicalGlobalPosition - new Vector2(0f, 40f), 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0).gridPos = base.gridPos;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
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
		return !digOver;
	}

	public override void BlockDigger(TowerDefenseCharacter target)
	{
		if (!digOver)
		{
			RequestEmergence();
		}
	}
}
