using Godot;

[Tool]
public partial class ZombieNormalGatlingPeaSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase head;

	public override void _Ready()
	{
		base._Ready();
		if (!AdobeAnimateSprite.IsEditorContext)
		{
			head = GetNode<AdobeAnimateSpriteBase>("%Head");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (!AdobeAnimateSprite.IsEditorContext && GodotObject.IsInstanceValid(head) && head.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(head);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (GodotObject.IsInstanceValid(head))
		{
			head.ResetAnimation();
		}
	}
}
