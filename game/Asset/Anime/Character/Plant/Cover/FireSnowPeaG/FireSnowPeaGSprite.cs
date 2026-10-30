using Godot;

[Tool]
public partial class FireSnowPeaGSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase headRight;

	private AdobeAnimateSpriteBase headLeft;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			headRight = GetNode<AdobeAnimateSpriteBase>("%HeadRight");
			headLeft = GetNode<AdobeAnimateSpriteBase>("%HeadLeft");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (headRight.IsNodeReady())
		{
			headRight.pause = base.pause;
			headRight.LightMask = base.LightMask;
		}
		if (headLeft.IsNodeReady())
		{
			headLeft.pause = base.pause;
			headLeft.LightMask = base.LightMask;
		}
		if (headRight.timeScale != 0.0 && headRight.clip == "FlameHeadIdle")
		{
			int num = headRight.clipRange.X + (frameIndex - clipRange.X);
			if (headRight.frameIndex < num - 1)
			{
				headLeft.timeScale = 2.0;
				headRight.timeScale = 2.0;
			}
			else if (headRight.frameIndex > num + 1)
			{
				headLeft.timeScale = 0.5;
				headRight.timeScale = 0.5;
			}
			else
			{
				headLeft.timeScale = 1.0;
				headRight.timeScale = 1.0;
			}
		}
	}
}
