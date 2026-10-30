using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantButterGloomShroom : TowerDefensePlant
{
	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM_BUTTER;

	private AttackComponent _attackComponent;

	private double _fireInterval = 2.0;

	private static PackedScene TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM_BUTTER => _TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM_BUTTER ?? (_TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM_BUTTER = GD.Load<PackedScene>("uid://d3d0raevayau"));

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
			if (IsNodeReady() && _attackComponent != null)
			{
				AttackComponent attackComponent = _attackComponent;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					_attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_attackComponent.OnAttack += Attack;
			_attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= Attack;
		}
	}

	public void Attack()
	{
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = TOWER_DEFENSE_PROJECTILE_EFFECT_GROOM_BUTTER.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
		towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["fireInterval"] = fireInterval };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}
}
