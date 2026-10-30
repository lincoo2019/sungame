using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantEMPCherry : TowerDefensePlant
{
	private static PackedScene _empShockScene;

	private ExplodeComponent _explodeComponent;

	private Vector2 _empRange;

	private static PackedScene EMP_SHOCK_SCENE => _empShockScene ?? (_empShockScene = GD.Load<PackedScene>("uid://fbt1dfatem6i"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			if (_explodeComponent != null)
			{
				_explodeComponent.OnExplode += OnExplodeHandler;
			}
			_empRange = TowerDefenseManager.Instance.GetMapGridSize() * 2.75f * 0.5f;
		}
	}

	public override void _ExitTree()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= OnExplodeHandler;
		}
		base._ExitTree();
	}

	private void OnExplodeHandler()
	{
		if (GodotObject.IsInstanceValid(EMP_SHOCK_SCENE))
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(EMP_SHOCK_SCENE, base.gridPos, "Idle");
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint);
				TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		Array campTarget = TowerDefenseManager.Instance.GetCampTarget(base.camp);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		foreach (Variant item in campTarget)
		{
			TowerDefenseZombie towerDefenseZombie = item.As<TowerDefenseZombie>();
			if (towerDefenseZombie != null && (towerDefenseZombie.instance.physiqueTypeFlags & 0x800) != 0 && towerDefenseZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				Vector2 vector = (towerDefenseZombie.GetLogicalGlobalPosition() - logicalGlobalPosition).Abs();
				if (!(vector.X > _empRange.X) && !(vector.Y > _empRange.Y))
				{
					TowerDefenseCharacterBuffEMP towerDefenseCharacterBuffEMP = new TowerDefenseCharacterBuffEMP();
					towerDefenseCharacterBuffEMP.time = 8.0;
					towerDefenseZombie.buff.AddBuff(towerDefenseCharacterBuffEMP);
				}
			}
		}
	}
}
