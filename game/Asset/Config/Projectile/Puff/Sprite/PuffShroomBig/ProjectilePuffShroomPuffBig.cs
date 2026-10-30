using Godot;

public partial class ProjectilePuffShroomPuffBig : Sprite2D
{
	public virtual void Refresh()
	{
		CreateTween().TweenProperty(this, "scale", Vector2.One * 1.5f, 0.25).From(Vector2.Zero);
	}
}
