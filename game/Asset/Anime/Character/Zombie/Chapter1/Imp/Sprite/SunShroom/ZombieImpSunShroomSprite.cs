using Godot;

[Tool]
public partial class ZombieImpSunShroomSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _head;

	public AdobeAnimateSpriteBase head => _head;

	public override void _Ready()
	{
		base._Ready();
		if (!AdobeAnimateSprite.IsEditorContext)
		{
			_head = GetNode<AdobeAnimateSpriteBase>("%Head");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (!AdobeAnimateSprite.IsEditorContext && GodotObject.IsInstanceValid(_head) && _head.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_head);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (GodotObject.IsInstanceValid(_head))
		{
			_head.ResetAnimation();
		}
	}
}
