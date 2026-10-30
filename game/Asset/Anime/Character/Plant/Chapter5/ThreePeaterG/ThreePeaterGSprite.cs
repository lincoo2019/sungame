using Godot;

[Tool]
public partial class ThreePeaterGSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase head1;

	public AdobeAnimateSpriteBase head2;

	public AdobeAnimateSpriteBase head3;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			head1 = GetNode<AdobeAnimateSpriteBase>("%Head1");
			head2 = GetNode<AdobeAnimateSpriteBase>("%Head2");
			head3 = GetNode<AdobeAnimateSpriteBase>("%Head3");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			if (head1.IsNodeReady())
			{
				head1.pause = base.pause;
				head1.LightMask = base.LightMask;
			}
			if (head2.IsNodeReady())
			{
				head2.pause = base.pause;
				head2.LightMask = base.LightMask;
			}
			if (head3.IsNodeReady())
			{
				head3.pause = base.pause;
				head3.LightMask = base.LightMask;
			}
		}
	}
}
