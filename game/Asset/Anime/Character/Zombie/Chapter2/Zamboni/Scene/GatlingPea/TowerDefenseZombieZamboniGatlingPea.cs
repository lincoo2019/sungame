using Godot;

[Tool]
public partial class TowerDefenseZombieZamboniGatlingPea : TowerDefenseZombie
{
	private static PackedScene _ZAMBONI_EXPLOSION;

	private GpuParticles2D _zamboniSmoke;

	private Marker2D _iceCapMarker;

	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "Pea";

	public double speed = 30.0;

	public bool audioPlay;

	private static PackedScene ZAMBONI_EXPLOSION => _ZAMBONI_EXPLOSION ?? (_ZAMBONI_EXPLOSION = GD.Load<PackedScene>("uid://bbsti03vlotx6"));

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady())
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zamboniSmoke = GetNode<GpuParticles2D>("%ZamboniSmoke");
			_iceCapMarker = GetNode<Marker2D>("%IceCapMarker");
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			_fireComponent.fireInterval = (float)fireInterval;
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.5;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		if (!base.sprite.pause)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			double num = (((double)globalPositionForPhysicsFrame.X > TowerDefenseManager.Instance.GetMapGroundRight()) ? 2.0 : 1.0);
			globalPositionForPhysicsFrame.X -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * num * (double)((!base.sprite.playBack) ? 1 : (-1)));
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (!audioPlay && (double)logicalGlobalPosition.X < TowerDefenseManager.Instance.GetMapGroundRight())
		{
			AudioManager.Instance.AudioPlay("Zamboni");
			audioPlay = true;
		}
		if (!instance.hypnoses)
		{
			TowerDefenseManager.Instance.SetIceCapPos(base.gridPos.Y, GetLogicalGlobalPosition(_iceCapMarker));
		}
		if (attackComponent.CanAttack())
		{
			if (attackComponent.target is TowerDefensePlant && GodotObject.IsInstanceValid(attackComponent.target.cell) && attackComponent.target.cell.HasSpike())
			{
				attackComponent.target = attackComponent.target.cell.GetSpike();
			}
			if ((attackComponent.target.instance.physiqueTypeFlags & 0x10) == 0)
			{
				attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
			}
			else
			{
				if (attackComponent.target.instance.spikeHurt != -1.0)
				{
					TowerDefenseCharacter target = attackComponent.target;
					double spikeHurt = attackComponent.target.instance.spikeHurt;
					target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
				}
				Die();
			}
		}
		if (instance.hitpoints < (base.config.hitpoints + base.config.hitpointsNearDeath) * 0.2)
		{
			if (speed > 5.0)
			{
				speed -= delta * 1.0;
			}
			((ZombieZamboniGatlingPeaSprite)base.sprite).shake = true;
		}
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		if (!isExplode)
		{
			CreateEffect();
			Destroy();
		}
		else
		{
			spritePause = true;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (!(damangePointName == "DamagePoint2"))
		{
			if (damangePointName == "DamagePoint3")
			{
				speed = 20.0;
				_zamboniSmoke.Visible = true;
			}
		}
		else
		{
			speed = 25.0;
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Wheelie"))
		{
			if (clip == "Wheelie2")
			{
				CreateEffect();
			}
		}
		else
		{
			CreateEffect();
		}
	}

	public void CreateEffect()
	{
		AudioManager.Instance.AudioPlay("ZamboniExplosion");
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (!TowerDefenseManager.TryCreateEffectSceneOnceFast(ZAMBONI_EXPLOSION, node2D, base.gridPos, logicalGlobalPosition, preferSprite: false))
		{
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(ZAMBONI_EXPLOSION, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
			node2D.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantGatlingPea");
		if (cell.CanPacketPlant(packetConfig, noLimit: true))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos, playAudio: true, noLimit: true);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantGatlingPea", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
