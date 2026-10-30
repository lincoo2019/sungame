using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGloompult : TowerDefensePlant
{
	private static readonly string TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM = "uid://bto1eksfijahm";

	private FireComponent fireComponent;

	private AttackComponent attackComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

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
			if (IsNodeReady() && this.fireComponent != null && attackComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
				}
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = (float)value;
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireNum = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent.OnAttack += Attack;
			attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
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

	public void Attack()
	{
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = (TowerDefenseProjectileEffectBase)GD.Load<PackedScene>(TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM).Instantiate(PackedScene.GenEditState.Disabled);
		towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
		towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		fireInterval = data.GetValueOrDefault("fireInterval", 3.0).AsDouble();
	}
}
