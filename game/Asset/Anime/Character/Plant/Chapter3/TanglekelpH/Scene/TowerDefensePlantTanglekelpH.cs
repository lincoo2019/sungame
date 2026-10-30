using Godot;

[Tool]
public partial class TowerDefensePlantTanglekelpH : TowerDefensePlant
{
	private TanglekelpComponent _tanglekelpComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_tanglekelpComponent = componentManager.GetRuntime<TanglekelpComponent>();
			if (_tanglekelpComponent != null)
			{
				_tanglekelpComponent.OnDrag += Drag;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
		if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
		{
			_tanglekelpComponent.OnDrag -= Drag;
		}
	}

	public virtual void Drag(TowerDefenseCharacter character, bool success)
	{
		if (success && GodotObject.IsInstanceValid(character))
		{
			Tween tween = character.CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Back);
			tween.SetParallel();
			tween.TweenProperty(character.transformPoint, "scale", Vector2.One, 0.5).From(Vector2.One * 0.5f);
			character.Hypnoses();
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			Vector2 logicalGlobalPosition2 = character.GetLogicalGlobalPosition();
			character.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X, logicalGlobalPosition2.Y));
			if (!instance.hypnoses && !character.instance.hypnoses)
			{
				character.Destroy();
			}
		}
	}
}
