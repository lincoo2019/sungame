using Godot;

[Tool]
public partial class SplitPeaCatSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _headRight;

	private AdobeAnimateSpriteBase _headLeft;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_headRight = GetNode<AdobeAnimateSpriteBase>("%HeadRight");
			_headLeft = GetNode<AdobeAnimateSpriteBase>("%HeadLeft");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (_headRight.IsNodeReady())
		{
			_headRight.pause = base.pause;
			_headRight.LightMask = base.LightMask;
		}
		if (_headLeft.IsNodeReady())
		{
			_headLeft.pause = base.pause;
			_headLeft.LightMask = base.LightMask;
		}
		if (_headRight.timeScale != 0.0 && _headRight.clip == "FlameHeadIdle")
		{
			int num = _headRight.clipRange.X + (frameIndex - clipRange.X);
			int num2 = _headRight.frameIndex;
			if (num2 < num - 1)
			{
				_headLeft.timeScale = 2.0;
				_headRight.timeScale = 2.0;
			}
			else if (num2 > num + 1)
			{
				_headLeft.timeScale = 0.5;
				_headRight.timeScale = 0.5;
			}
			else
			{
				_headLeft.timeScale = 1.0;
				_headRight.timeScale = 1.0;
			}
		}
	}
}
