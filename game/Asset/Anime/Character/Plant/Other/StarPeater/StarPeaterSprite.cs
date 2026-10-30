using Godot;

[Tool]
[GlobalClass]
public partial class StarPeaterSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _head1;

	private AdobeAnimateSpriteBase _head2;

	private AdobeAnimateSpriteBase _head3;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_head1 = GetNode<AdobeAnimateSpriteBase>("%Head1");
			_head2 = GetNode<AdobeAnimateSpriteBase>("%Head2");
			_head3 = GetNode<AdobeAnimateSpriteBase>("%Head3");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			if (_head1.IsNodeReady())
			{
				_head1.pause = base.pause;
				_head1.LightMask = base.LightMask;
			}
			if (_head2.IsNodeReady())
			{
				_head2.pause = base.pause;
				_head2.LightMask = base.LightMask;
			}
			if (_head3.IsNodeReady())
			{
				_head3.pause = base.pause;
				_head3.LightMask = base.LightMask;
			}
		}
	}
}
