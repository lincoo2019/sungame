using Godot;

[Tool]
public partial class ZombieImpPresentBoxSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _presentBoxImpHead;

	public AdobeAnimateSpriteBase presentBoxImpHead => _presentBoxImpHead;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_presentBoxImpHead = GetNode<AdobeAnimateSpriteBase>("%PresentBoxImpHead");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (GodotObject.IsInstanceValid(_presentBoxImpHead) && _presentBoxImpHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_presentBoxImpHead);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (GodotObject.IsInstanceValid(_presentBoxImpHead))
		{
			_presentBoxImpHead.ResetAnimation();
		}
	}
}
