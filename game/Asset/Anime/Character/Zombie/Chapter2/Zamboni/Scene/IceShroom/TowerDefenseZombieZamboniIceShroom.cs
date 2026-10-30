using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieZamboniIceShroom : TowerDefenseZombie
{
	private static PackedScene _ZAMBONI_EXPLOSION;

	private GpuParticles2D _zamboniSmoke;

	private Marker2D _iceCapMarker;

	public double speed = 30.0;

	public bool audioPlay;

	public double timer;

	public double time;

	public bool over;

	public bool explode;

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

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["timer"] = timer;
		dictionary["time"] = time;
		dictionary["explode"] = explode;
		dictionary["over"] = over;
		dictionary["speed"] = speed;
		dictionary["audioPlay"] = audioPlay;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		timer = Math.Max(0.0, data.GetValueOrDefault("timer", timer).AsDouble());
		if (data.ContainsKey("time"))
		{
			time = Math.Max(0.0, data["time"].AsDouble());
		}
		else if (time <= 0.0)
		{
			time = GD.RandRange(25.0, 30.0);
		}
		explode = data.GetValueOrDefault("explode", explode).AsBool();
		over = data.GetValueOrDefault("over", over).AsBool();
		speed = Math.Max(0.0, data.GetValueOrDefault("speed", speed).AsDouble());
		audioPlay = data.GetValueOrDefault("audioPlay", audioPlay).AsBool();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zamboniSmoke = GetNode<GpuParticles2D>("%ZamboniSmoke");
			_iceCapMarker = GetNode<Marker2D>("%IceCapMarker");
			if (!base.IsProgressRestoreInFlight)
			{
				time = GD.RandRange(25.0, 30.0);
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || TowerDefenseManager.CurrentControl == null || !TowerDefenseManager.CurrentControl.isGameRunning || !base.IsInsideComponentBattlefield || base.sprite.pause)
		{
			return;
		}
		if (timer < time)
		{
			timer += delta * timeScale;
			return;
		}
		explode = true;
		if (!nearDie && !die)
		{
			TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
			Destroy();
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			AudioManager.Instance.AudioPlay("ReverseExplosion");
			base.sprite.GetNode<AdobeAnimateSprite>("Head").timeScale = 1.0;
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
			CreateEffect();
			Destroy();
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
			((ZombieZamboniIceShroomSprite)base.sprite).shake = true;
		}
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		if (!isExplode)
		{
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
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantIceShroom");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
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
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantIceShroom", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
