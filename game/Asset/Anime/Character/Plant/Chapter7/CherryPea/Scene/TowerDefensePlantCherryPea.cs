using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantCherryPea : TowerDefensePlant
{
	private static PackedScene _TOWER_DEFENSE_PROJECTILE_EFFECT_CHERRY_PEA;

	private static PackedScene _PEA_BOMB_S_EXPLOSION;

	private ExplodeComponent _explodeComponent;

	private static PackedScene TOWER_DEFENSE_PROJECTILE_EFFECT_CHERRY_PEA => _TOWER_DEFENSE_PROJECTILE_EFFECT_CHERRY_PEA ?? (_TOWER_DEFENSE_PROJECTILE_EFFECT_CHERRY_PEA = GD.Load<PackedScene>("uid://c4cl6w0kg0pup"));

	private static PackedScene PEA_BOMB_S_EXPLOSION => _PEA_BOMB_S_EXPLOSION ?? (_PEA_BOMB_S_EXPLOSION = GD.Load<PackedScene>("uid://esp31duw2h25"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			if (base.currentCustom.Contains("Custom0"))
			{
				_explodeComponent.explodeEffect = PEA_BOMB_S_EXPLOSION;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = TOWER_DEFENSE_PROJECTILE_EFFECT_CHERRY_PEA.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, base.config.collisionFlags, null, base.groundHeight);
		towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
	}
}
