using Godot;

[Tool]
public partial class ZombieGargantuarZamboniSprite : AdobeAnimateSpriteBase
{
	public bool shake;

	protected override bool RequiresDisplayFrameBatchPhysicsUpdate => shake;

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (CanRun() && shake)
		{
			base.offset = new Vector2(0f, -100f) + new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1));
		}
	}
}
