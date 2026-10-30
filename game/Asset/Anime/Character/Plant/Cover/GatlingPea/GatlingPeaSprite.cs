using Godot;

[Tool]
public partial class GatlingPeaSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _head;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_head = GetNode<AdobeAnimateSpriteBase>("%Head");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (_head.IsNodeReady())
		{
			_head.pause = base.pause;
			_head.LightMask = base.LightMask;
		}
		if (_head.timeScale != 0.0 && _head.clip == "HeadIdle")
		{
			int num = _head.clipRange.X + (frameIndex - clipRange.X);
			if (_head.frameIndex < num)
			{
				_head.timeScale = 2.0;
			}
			else if (_head.frameIndex > num)
			{
				_head.timeScale = 0.5;
			}
			else
			{
				_head.timeScale = 1.0;
			}
		}
	}
}
