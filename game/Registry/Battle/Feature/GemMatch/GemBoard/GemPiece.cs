using Godot;

[GlobalClass]
public partial class GemPiece : Node2D
{
	public const double RemoveAnimationDuration = 0.2;

	public Vector2I gridPos = new Vector2I(-1, -1);

	public StringName characterKey = "";

	public TowerDefenseCharacter character;

	public bool isAnimating;

	public bool isHole;

	public void Setup(Vector2I _gridPos, StringName _characterKey)
	{
		gridPos = _gridPos;
		characterKey = _characterKey;
	}

	public void SetGridPos(Vector2I pos)
	{
		gridPos = pos;
	}

	public void SetAsHole()
	{
		isHole = true;
		characterKey = "";
		character = null;
		for (int num = GetChildCount() - 1; num >= 0; num--)
		{
			if (GetChild(num) is ColorRect colorRect)
			{
				colorRect.QueueFree();
			}
		}
	}

	public void PlayRemoveAnimation()
	{
		isAnimating = true;
		TowerDefenseCharacter towerDefenseCharacter = character;
		character = null;
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			QueueFree();
			return;
		}
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.In);
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(towerDefenseCharacter, "scale", Vector2.Zero, 0.2);
		tween.TweenCallback(Callable.From(base.QueueFree));
		towerDefenseCharacter.DestroyWithVisualDelay(0.2);
	}
}
