using Godot;

[Tool]
public partial class ReCactusSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase head;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			head = GetNode<AdobeAnimateSpriteBase>("%Head");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (head.IsNodeReady())
		{
			head.pause = base.pause;
			head.LightMask = base.LightMask;
		}
		if (head.timeScale != 0.0 && head.clip == "HeadIdle")
		{
			int num = head.clipRange.X + (frameIndex - clipRange.X);
			if (head.frameIndex < num)
			{
				head.timeScale = 2.0;
			}
			else if (head.frameIndex > num)
			{
				head.timeScale = 0.5;
			}
			else
			{
				head.timeScale = 1.0;
			}
		}
	}
}
