using Godot;

[Tool]
public partial class DiceBombSprite : AdobeAnimateSpriteBase
{
	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (CanRun())
		{
			base.offset = new Vector2(-40f, -40f) + new Vector2((float)((double)GD.Randf() * 2.0 - 1.0), (float)((double)GD.Randf() * 2.0 - 1.0));
		}
	}
}
