using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSquidGloomShroom : TowerDefensePlant
{
	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID;

	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID_BIG;

	private AttackComponent _attackComponent;

	public bool over;

	private double _fireInterval = 2.0;

	private static PackedScene TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID => _TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID ?? (_TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID = GD.Load<PackedScene>("uid://bexrfifm3d11i"));

	private static PackedScene TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID_BIG => _TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID_BIG ?? (_TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID_BIG = GD.Load<PackedScene>("uid://cc2e55hq70tn6"));

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

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID_BIG.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
			towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
			towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void Attack()
	{
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = TOWER_DEFENSE_PROJECTILE_EFFECT_GLOOM_SQUID.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
		towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["over"] = over,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}
}
