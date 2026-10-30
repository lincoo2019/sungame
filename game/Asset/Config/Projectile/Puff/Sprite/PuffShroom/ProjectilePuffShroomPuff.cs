using Godot;

public partial class ProjectilePuffShroomPuff : Sprite2D
{
	public virtual void Refresh()
	{
		CreateTween().TweenProperty(this, "scale", Vector2.One, 0.25).From(Vector2.Zero);
	}
}
