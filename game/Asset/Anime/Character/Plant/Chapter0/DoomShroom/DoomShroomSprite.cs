using Godot;

[Tool]
public partial class DoomShroomSprite : AdobeAnimateSpriteBase
{
	protected override bool RequiresDisplayFrameBatchPhysicsUpdate => true;

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (CanRun())
		{
			base.offset = new Vector2(-40f, -40f) + new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0));
		}
	}
}
