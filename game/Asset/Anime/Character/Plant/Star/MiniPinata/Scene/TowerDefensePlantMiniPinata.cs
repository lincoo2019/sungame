using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantMiniPinata : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			_attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
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
		foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(_attackComponent.GetCharcterList()))
		{
			if (item is TowerDefenseZombie && item.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS && !item.instance.hypnoses)
			{
				if ((double)item.transformPoint.Scale.X > 0.25)
				{
					Vector2 scale = item.transformPoint.Scale;
					Vector2 vector = item.transformPoint.Scale * 0.5f;
					item.transformPoint.Scale = vector;
					Tween tween = item.CreateTween();
					tween.SetEase(Tween.EaseType.In);
					tween.SetTrans(Tween.TransitionType.Back);
					tween.TweenProperty(item.transformPoint, "scale", vector, 1.0).From(scale);
					item.instance.hitpointScale *= 0.5;
				}
				item.instance.ArmorClear();
			}
		}
	}
}
