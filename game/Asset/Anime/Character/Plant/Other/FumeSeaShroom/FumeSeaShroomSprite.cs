using Godot;

[Tool]
public partial class FumeSeaShroomSprite : AdobeAnimateSpriteBase
{
	public double timer;

	protected override bool RequiresDisplayFrameBatchPhysicsUpdate => true;

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (CanRun())
		{
			timer += delta * (double)Get("timeScale");
			base.offset = new Vector2(base.offset.X, -20f + (float)(Mathf.Sin(timer * 3.0) * 3.0));
		}
	}
}
