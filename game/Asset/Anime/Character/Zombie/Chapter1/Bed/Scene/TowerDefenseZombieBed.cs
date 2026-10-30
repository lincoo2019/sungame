using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBed : TowerDefenseZombie
{
	public bool over;

	public override void WalkProcessing(double delta)
	{
		if ((double)GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame).X > TowerDefenseManager.Instance.GetMapGroundRight())
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale;
		}
		if (!attackComponent.CanAttack())
		{
			return;
		}
		if (GodotObject.IsInstanceValid(attackComponent.target) && GodotObject.IsInstanceValid(attackComponent.target.cell) && attackComponent.target.cell.HasSpike())
		{
			attackComponent.target = attackComponent.target.cell.GetSpike();
		}
		if ((attackComponent.target.instance.physiqueTypeFlags & 0x10) == 0)
		{
			attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
			return;
		}
		if (attackComponent.target.instance.spikeHurt != -1.0)
		{
			TowerDefenseCharacter target = attackComponent.target;
			double spikeHurt = attackComponent.target.instance.spikeHurt;
			target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
		}
		Die();
	}

	public override void DieEntered()
	{
		base.DieEntered();
		DestroySet();
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			over = true;
			return;
		}
		over = true;
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieSleeper");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2 pos = GetLogicalGlobalPosition() + new Vector2(45f, 0f);
		TowerDefenseCharacter sleeper = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, pos, base.gridPos, 50.0) : packetConfig.Create(pos, base.gridPos, 50.0));
		if (!GodotObject.IsInstanceValid(sleeper))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", sleeper);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(sleeper))
				{
					sleeper.Hypnoses();
				}
			}).CallDeferred();
		}
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(sleeper))
			{
				if (GodotObject.IsInstanceValid(sleeper.instance))
				{
					sleeper.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(sleeper.transformPoint))
				{
					sleeper.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		sleeper.SetDeferred("invisible", base.invisible);
		double rotationFrom = 90f * base.Scale.X;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(sleeper))
			{
				sleeper.CreateTween().TweenProperty(sleeper, "rotation_degrees", 0, 0.2).From(rotationFrom);
			}
		}).CallDeferred();
		GetTree().CreateTimer(0.1, processAlways: false).Timeout += delegate
		{
			if (GodotObject.IsInstanceValid(sleeper))
			{
				((TowerDefenseZombie)sleeper).Walk();
			}
		};
		Dictionary spawnState = new Dictionary
		{
			["spawn_invisible"] = base.invisible,
			["rotation_from_degrees"] = rotationFrom,
			["rotation_to_degrees"] = 0.0,
			["rotation_duration"] = 0.2,
			["walk_delay"] = 0.1
		};
		TowerDefenseManager.PublishSpawnedCharacter("ZombieSleeper", sleeper, useCreate: true, 0.0, walkAfterSpawn: true, "", spawnState);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
