using Godot;

[Tool]
public partial class ZombieNormalWallnutABCSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase head1;

	public AdobeAnimateSpriteBase head2;

	public AdobeAnimateSpriteBase head3;

	public override void _Ready()
	{
		base._Ready();
		if (!AdobeAnimateSprite.IsEditorContext)
		{
			head1 = GetNode<AdobeAnimateSpriteBase>("%Head1");
			head2 = GetNode<AdobeAnimateSpriteBase>("%Head2");
			head3 = GetNode<AdobeAnimateSpriteBase>("%Head3");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (!AdobeAnimateSprite.IsEditorContext)
		{
			if (GodotObject.IsInstanceValid(head1) && head1.IsRuntimeReadyCached)
			{
				SyncRuntimeChildState(head1);
			}
			if (GodotObject.IsInstanceValid(head2) && head2.IsRuntimeReadyCached)
			{
				SyncRuntimeChildState(head2);
			}
			if (GodotObject.IsInstanceValid(head3) && head3.IsRuntimeReadyCached)
			{
				SyncRuntimeChildState(head3);
			}
		}
	}
}
