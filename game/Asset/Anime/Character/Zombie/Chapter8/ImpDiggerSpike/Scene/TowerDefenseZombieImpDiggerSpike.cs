using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieImpDiggerSpike : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	private static PackedScene _DIGGER_RISING_DIRT;

	public double speed = 50.0;

	public bool digOver;

	public double discardDownPos = 10000.0;

	private StateHandle _digStateHandle;

	private StateHandle _drillStateHandle;

	private StateHandle _landStateHandle;

	private StateHandle _upStateHandle;

	private bool _roleStateSignalsConnected;

	private bool _networkDrillSpawn;

	private double _upWalkDelayRemaining = -1.0;

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
		_digStateHandle = base.StateMachine?.GetStateById("zombie.imp_digger_spike.dig");
		_drillStateHandle = base.StateMachine?.GetStateById("zombie.imp_digger_spike.drill");
		_landStateHandle = base.StateMachine?.GetStateById("zombie.imp_digger_spike.land");
		_upStateHandle = base.StateMachine?.GetStateById("zombie.imp_digger_spike.up");
		StateHandle digStateHandle = _digStateHandle;
		if (digStateHandle == null || !digStateHandle.IsValid)
		{
			return;
		}
		StateHandle drillStateHandle = _drillStateHandle;
		if (drillStateHandle == null || !drillStateHandle.IsValid)
		{
			return;
		}
		StateHandle landStateHandle = _landStateHandle;
		if (landStateHandle != null && landStateHandle.IsValid)
		{
			StateHandle upStateHandle = _upStateHandle;
			if (upStateHandle != null && upStateHandle.IsValid)
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
				_upStateHandle.Entered += UpEntered;
				_upStateHandle.Exited += UpExited;
				_upStateHandle.PhysicsProcessing += UpProcessing;
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
			if (_upStateHandle != null)
			{
				_upStateHandle.Entered -= UpEntered;
				_upStateHandle.Exited -= UpExited;
				_upStateHandle.PhysicsProcessing -= UpProcessing;
			}
			_upStateHandle = null;
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
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			instance.collisionFlags = 17;
			instance.maskFlags = 16;
			ConnectRoleStateSignals();
			if (_networkDrillSpawn)
			{
				CallDeferred("ApplyNetworkDrillSpawnState");
			}
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (data != null)
		{
			_networkDrillSpawn = data.GetValueOrDefault("drill_spawn", false).AsBool();
			if (_networkDrillSpawn && IsNodeReady())
			{
				CallDeferred("ApplyNetworkDrillSpawnState");
			}
		}
	}

	private void ApplyNetworkDrillSpawnState()
	{
		if (_networkDrillSpawn && !die && !nearDie)
		{
			_networkDrillSpawn = false;
			digOver = true;
			SendStateEvent("ToDrill");
			instance.ArmorDelete("Pick");
			isRise = false;
			instance.collisionFlags = 1;
			instance.maskFlags = 9;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && ((long)Engine.GetPhysicsFrames() + (long)randFreshIndex) % 30 == 0L && !base.inWater)
		{
			SetSpriteGroupShaderParameter("discardDownPos", discardDownPos);
		}
	}

	public void DigEntered()
	{
		base.attackAnimeClip = "Eat2";
		base.dieAnimeClip = "Death2";
		base.dieWaterAnimeClip = "Death2";
		instance.collisionFlags = 17;
		instance.maskFlags = 16;
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
		if (attackComponent.CanAttack() && !nearDie && !base.sprite.pause && !digOver)
		{
			SendStateEvent("ToUp");
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
		}
	}

	public void DigExited()
	{
		instance.unUseBuffFlags = 0;
	}

	public void DrillEntered()
	{
		base.attackAnimeClip = "Eat";
		base.dieAnimeClip = "Death";
		base.dieWaterAnimeClip = "Death";
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		if (!base.inWater)
		{
			SetDigShadowVisible(!base.invisible);
			CreateEffect();
		}
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
		base.attackAnimeClip = "Eat";
		base.dieAnimeClip = "Death";
		base.dieWaterAnimeClip = "Death";
		base.sprite.SetAnimation("Land", loop: false);
	}

	public virtual void LandProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void LandExited()
	{
	}

	public void UpEntered()
	{
		Transform2D screenTransform = GetViewport().GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		discardDownPos = (screenTransform * (GetLogicalGlobalPosition(spriteGroup) + new Vector2(0f, 53f))).Y;
		SetSpriteGroupShaderParameter("discardDownPos", discardDownPos);
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		if (!base.inWater)
		{
			SetDigShadowVisible(!base.invisible);
		}
		base.attackAnimeClip = "Eat2";
		base.dieAnimeClip = "Death2";
		base.dieWaterAnimeClip = "Death2";
		base.sprite.SetAnimation("Up", loop: false);
		SetSpriteGroupShaderParameter("discardDownPos", discardDownPos);
		_upWalkDelayRemaining = -1.0;
		CompleteUpEntryAsync();
	}

	private async Task CompleteUpEntryAsync()
	{
		if (await WaitForStateDelayAsync(_upStateHandle, 0.2))
		{
			base.sprite.SetAnimation(base.attackAnimeClip, loop: true, 0.2);
			ActivateAttackAfterStateDelay(_upStateHandle);
		}
	}

	public void UpExited()
	{
		_upWalkDelayRemaining = -1.0;
		startAttack = false;
		discardDownPos = 10000.0;
		SetSpriteGroupShaderParameter("discardDownPos", discardDownPos);
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

	public void UpProcessing(double delta)
	{
		if (!nearDie && !die && !digOver && !base.sprite.pause && !attackComponent.CanAttack())
		{
			if (_upWalkDelayRemaining < 0.0)
			{
				_upWalkDelayRemaining = 0.2;
			}
			else
			{
				_upWalkDelayRemaining -= delta;
			}
			if (_upWalkDelayRemaining <= 0.0 && !attackComponent.CanAttack())
			{
				_upWalkDelayRemaining = -1.0;
				Walk();
			}
		}
		else
		{
			_upWalkDelayRemaining = -1.0;
			if (startAttack && !nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps)
			{
				attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
			}
		}
		base.sprite.timeScale = timeScale * 2.0;
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

	public override void DieEntered()
	{
		base.DieEntered();
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Drill"))
		{
			if (clip == "Land")
			{
				WalkWithoutTransitionDelay();
			}
		}
		else
		{
			SendStateEvent("ToLand");
		}
	}

	public override async void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pick")
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (!digOver && !nearDie && !die)
			{
				digOver = true;
				SendStateEvent("ToDrill");
			}
		}
	}

	public void CreateEffect()
	{
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(DIGGER_RISING_DIRT, base.gridPos);
		towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(shadowSprite) - new Vector2(15f, 0f);
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
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
		dictionary["discardDownPos"] = discardDownPos;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		speed = data.GetValueOrDefault("speed", 50.0).AsDouble();
		digOver = data.GetValueOrDefault("digOver", false).AsBool();
		discardDownPos = data.GetValueOrDefault("discardDownPos", 10000.0).AsDouble();
	}
}
