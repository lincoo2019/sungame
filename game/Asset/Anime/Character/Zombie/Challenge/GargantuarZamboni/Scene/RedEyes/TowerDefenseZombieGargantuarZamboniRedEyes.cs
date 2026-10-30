using Godot;

[Tool]
public partial class TowerDefenseZombieGargantuarZamboniRedEyes : TowerDefenseZombie
{
	private static PackedScene _ZAMBONI_EXPLOSION;

	private GpuParticles2D _zamboniSmoke;

	private Marker2D _iceCapMarker;

	public double speed = 30.0;

	public bool audioPlay;

	public bool over;

	private static PackedScene ZAMBONI_EXPLOSION => _ZAMBONI_EXPLOSION ?? (_ZAMBONI_EXPLOSION = GD.Load<PackedScene>("uid://bbsti03vlotx6"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zamboniSmoke = GetNode<GpuParticles2D>("%ZamboniSmoke");
			_iceCapMarker = GetNode<Marker2D>("%IceCapMarker");
		}
	}

	public override void WalkProcessing(double delta)
	{
		double num = timeScale * 0.4;
		base.sprite.timeScale = num;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		double mapGroundRight = towerDefenseManager.GetMapGroundRight();
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!base.sprite.pause)
		{
			double playbackSign = (base.sprite.playBack ? (-1.0) : 1.0);
			double zamboniPlaybackMultiplier = GetZamboniPlaybackMultiplier(playbackSign);
			double num2 = (((double)globalPositionForPhysicsFrame.X > mapGroundRight) ? 2.0 : 1.0);
			float x = transformPoint.Scale.X;
			float x2 = base.Scale.X;
			globalPositionForPhysicsFrame.X -= (float)(speed * delta * num * (double)x * (double)x2 * num2 * zamboniPlaybackMultiplier);
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		if (!audioPlay && (double)globalPositionForPhysicsFrame.X < mapGroundRight)
		{
			AudioManager.Instance.AudioPlay("Zamboni");
			audioPlay = true;
		}
		if (!instance.hypnoses)
		{
			towerDefenseManager.SetIceCapPos(base.gridPos.Y, GetLogicalGlobalPosition(_iceCapMarker));
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
			ZombieGargantuarZamboniSprite zombieGargantuarZamboniSprite = base.sprite as ZombieGargantuarZamboniSprite;
			if (GodotObject.IsInstanceValid(zombieGargantuarZamboniSprite))
			{
				zombieGargantuarZamboniSprite.shake = true;
			}
		}
	}

	private double GetZamboniPlaybackMultiplier(double playbackSign)
	{
		if (instance.hypnoses)
		{
			return 1.0;
		}
		return playbackSign;
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
			Destroy();
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "DamagePoint2"))
		{
			if (damagePointName == "DamagePoint3")
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

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		HitBoxDestroy();
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuarRedEyes");
		Vector2 pos = GetLogicalGlobalPosition() + new Vector2(120f, 0f);
		TowerDefenseZombie zombie = packetConfig.Create(pos, base.gridPos, base.groundHeight) as TowerDefenseZombie;
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		double num = (base.inWater ? (0.0 - zombie.waterHeight) : base.groundHeight);
		zombie.groundHeight = num;
		zombie.z = num;
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", zombie);
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		zombie.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
		zombie.invisible = base.invisible;
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.Hypnoses();
				}
			}).CallDeferred();
		}
		zombie.CallDeferred("WalkReady");
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuarRedEyes", base.gridPos.X, base.gridPos.Y, nextSyncId, hitpointScale, scale.X, instance.hypnoses, 0.0, useCreate: true, pos.X, pos.Y, walkAfterSpawn: true, num);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
