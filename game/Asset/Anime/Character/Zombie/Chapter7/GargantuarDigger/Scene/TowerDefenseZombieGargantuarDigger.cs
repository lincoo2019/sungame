using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieGargantuarDigger : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_GARGANTUAR_HEAD2 = "uid://cfx6iqy08xujv";

	private static PackedScene _DIGGER_RISING_DIRT;

	public double speed = 50.0;

	public bool digOver;

	private StateHandle _digStateHandle;

	private StateHandle _drillStateHandle;

	private StateHandle _landStateHandle;

	private bool _roleStateSignalsConnected;

	private static PackedScene DIGGER_RISING_DIRT => _DIGGER_RISING_DIRT ?? (_DIGGER_RISING_DIRT = GD.Load<PackedScene>("uid://bjev0ulao283j"));

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
		_digStateHandle = base.StateMachine?.GetStateById("zombie.gargantuar_digger.dig");
		_drillStateHandle = base.StateMachine?.GetStateById("zombie.gargantuar_digger.drill");
		_landStateHandle = base.StateMachine?.GetStateById("zombie.gargantuar_digger.land");
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
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
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
			_roleStateSignalsConnected = false;
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
			base.useAttackDps = true;
			ConnectRoleStateSignals();
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			base.sprite.SetAtlasReplace("Zombie_gargantuar_head.png", "uid://cfx6iqy08xujv");
		}
		else if (damangePointName == impThrowDamagePointName)
		{
			TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
			if (mapFeature != null && (double)GetLogicalGlobalPosition().X < (double)mapFeature.config.edge.X + (double)TowerDefenseManager.Instance.GetMapGridSize().X * 5.0)
			{
				impThrowFlag = true;
			}
		}
	}

	public void DigEntered()
	{
		instance.collisionFlags = 16;
		instance.maskFlags = 16;
		base.useAttackDps = true;
		base.sprite.SetAnimation("Dig");
		SetDigShadowVisible(visible: false);
	}

	public virtual void DigProcessing(double delta)
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

	public void DigExited()
	{
		instance.unUseBuffFlags = 0;
	}

	public void DrillEntered()
	{
		instance.collisionFlags = 9;
		instance.maskFlags = 9;
		base.useAttackDps = false;
		SetDigShadowVisible(!base.invisible);
		CreateEffect();
		Rise(0.5, 0.0, createDirt: false, changeState: false);
		base.sprite.SetAnimation("Drill", loop: true, 0.2);
	}

	public virtual void DrillProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void DrillExited()
	{
	}

	public void LandEntered()
	{
		base.sprite.SetAnimation("Land", loop: false);
		base.sprite.AddAnimation("Dizzy", 0.0, loop: false, 0.2);
	}

	public virtual void LandProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
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
		if (!(clip == "Drill"))
		{
			if (clip == "Dizzy")
			{
				WalkWithoutTransitionDelay();
			}
		}
		else
		{
			SendStateEvent("ToLand");
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pick" && !digOver)
		{
			digOver = true;
			SendStateEvent("ToDrill");
		}
	}

	public void CreateEffect()
	{
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(DIGGER_RISING_DIRT, base.gridPos);
		towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(shadowSprite) - new Vector2(15f, 0f);
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
	}

	public void ImpSpawn()
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		impSpawnSlot.Update();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(impName);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(impSpawnSlot);
		Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition();
		double height = GetGroundHeight(logicalGlobalPosition.Y) - base.groundHeight;
		TowerDefenseZombieImpBase imp = packetConfig.Create(new Vector2(logicalGlobalPosition.X, logicalGlobalPosition2.Y), base.gridPos, height) as TowerDefenseZombieImpBase;
		imp.ySpeed = -60.0;
		imp._throw = true;
		if (Mathf.Sign(base.Scale.X) < 0 && imp is TowerDefenseZombieImpDigger towerDefenseZombieImpDigger)
		{
			towerDefenseZombieImpDigger.digOver = true;
		}
		imp.Scale = new Vector2(base.Scale.X, imp.Scale.Y);
		TowerDefenseGroundItemBase.characterNode.AddChild(imp, forceReadableName: false, InternalMode.Disabled);
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(imp))
			{
				if (GodotObject.IsInstanceValid(imp.instance))
				{
					imp.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(imp.transformPoint))
				{
					imp.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		imp.SetDeferred("invisible", base.invisible);
		if (instance.hypnoses)
		{
			imp.Hypnoses();
		}
		double num = GD.RandRange(TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(3, 0)).X, TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(5, 0)).X);
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quad);
		Vector2 logicalGlobalPosition3 = imp.GetLogicalGlobalPosition();
		tween.TweenMethod(to: new Vector2((float)num, logicalGlobalPosition3.Y), method: Callable.From<Vector2>(imp.SetLogicalGlobalPosition), from: logicalGlobalPosition3, duration: imp.GetFallTime());
	}

	public override bool CanDiggerBlock()
	{
		return !digOver;
	}

	public override void BlockDigger(TowerDefenseCharacter target)
	{
		digOver = true;
		SendStateEvent("ToDrill");
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["speed"] = speed;
		dictionary["digOver"] = digOver;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		speed = data.GetValueOrDefault("speed", 50.0).AsDouble();
		digOver = data.GetValueOrDefault("digOver", false).AsBool();
	}
}
