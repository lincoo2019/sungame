using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieZamboni : TowerDefenseZombie
{
	private static PackedScene _ZAMBONI_EXPLOSION;

	private GpuParticles2D _zamboniSmoke;

	private Marker2D _iceCapMarker;

	public double speed = 30.0;

	public bool audioPlay;

	private static PackedScene ZAMBONI_EXPLOSION => _ZAMBONI_EXPLOSION ?? (_ZAMBONI_EXPLOSION = GD.Load<PackedScene>("uid://bbsti03vlotx6"));

	private float GlobalPositionX
	{
		get
		{
			return GetLogicalGlobalPosition().X;
		}
		set
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X = value;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

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
		base.sprite.timeScale = timeScale * 0.5;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		if (!base.sprite.pause)
		{
			if ((double)GlobalPositionX > TowerDefenseManager.Instance.GetMapGroundRight())
			{
				GlobalPositionX -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * 2.0 * (double)((!base.sprite.playBack) ? 1 : (-1)));
			}
			else
			{
				GlobalPositionX -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
			}
		}
		if (!audioPlay && (double)GlobalPositionX < TowerDefenseManager.Instance.GetMapGroundRight())
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
			((ZombieZamboniSprite)base.sprite).shake = true;
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
				speed /= 1.5;
				_zamboniSmoke.Visible = true;
			}
		}
		else
		{
			speed /= 1.2;
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

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "speed", speed } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		speed = (double)data.GetValueOrDefault("speed", 30.0);
	}
}
