using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieRobot : TowerDefenseZombie
{
	private static PackedScene _robotExplosionScene;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool halfHp;

	public bool isAttack;

	public bool over;

	private static PackedScene RobotExplosionScene => _robotExplosionScene ?? (_robotExplosionScene = GD.Load<PackedScene>("uid://cxnt2jbnk48fp"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (HasShield())
		{
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Arm")
		{
			halfHp = true;
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_upper" }, open: true);
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(RobotExplosionScene, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(0.25f, 0.25f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1, suppressDeathrattles: true);
		int num = 0;
		foreach (Node item in GetTree().GetNodesInGroup(base.config.name))
		{
			if (num < 5)
			{
				if (item is TowerDefenseZombieRobot towerDefenseZombieRobot && towerDefenseZombieRobot != this && towerDefenseZombieRobot.camp == base.camp && GodotObject.IsInstanceValid(towerDefenseZombieRobot.instance) && !towerDefenseZombieRobot.die && !towerDefenseZombieRobot.nearDie && !(towerDefenseZombieRobot.instance.hitpoints >= 3000.0))
				{
					double num2 = Math.Min(500.0, 3000.0 - towerDefenseZombieRobot.instance.hitpoints);
					towerDefenseZombieRobot.Health(num2);
					num++;
				}
				continue;
			}
			break;
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		base.DestroySet();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "over", over },
			{ "halfHp", halfHp },
			{ "isAttack", isAttack }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
		halfHp = data.GetValueOrDefault("halfHp", false).AsBool();
		isAttack = data.GetValueOrDefault("isAttack", false).AsBool();
		if (halfHp)
		{
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_outerarm_upper" }, open: true);
		}
	}
}
