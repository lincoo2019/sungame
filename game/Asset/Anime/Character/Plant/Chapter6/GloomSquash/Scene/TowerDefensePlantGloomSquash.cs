using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGloomSquash : TowerDefensePlant
{
	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUASH;

	public SquashComponent squashComponent;

	public AttackComponent attackComponent;

	public AttackComponent attackComponent2;

	private double _fireInterval = 2.0;

	private static PackedScene TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUASH => _TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUASH ?? (_TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUASH = GD.Load<PackedScene>("uid://b3y8v3hddvg"));

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
			if (IsNodeReady() && attackComponent != null)
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			squashComponent = componentManager.GetRuntime<SquashComponent>();
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			attackComponent.OnAttack += Attack;
			attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
			instance.hitpointsEmpty -= OnHitpointsEmpty;
			instance.keepAlive = true;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttack -= Attack;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && instance.hitpoints <= 0.0)
		{
			SquashComponent obj = squashComponent;
			if (obj != null && !obj.Alive)
			{
				ToSquash();
			}
			else
			{
				Destroy();
			}
		}
	}

	public void OnHitpointsEmpty()
	{
		Destroy();
	}

	public virtual void Attack()
	{
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUASH.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
		towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
	}

	public virtual void ToSquash()
	{
		attackComponent.SetAlive(alive: false);
		attackComponent2.SetAlive(alive: true);
		instance.biteHurt = 0.0;
		instance.keepAlive = false;
		squashComponent.SetAlive(alive: true);
		instance.hitpoints = 300.0;
		instance.die = false;
		instance.nearDie = false;
		nearDie = false;
		die = false;
		destroyComponent?.EndDeathSettlement();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "fireInterval", fireInterval } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireInterval = data.GetValueOrDefault("fireInterval", 2.0).AsDouble();
	}
}
