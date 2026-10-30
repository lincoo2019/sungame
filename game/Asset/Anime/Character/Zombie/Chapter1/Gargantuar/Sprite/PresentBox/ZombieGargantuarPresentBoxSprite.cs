using Godot;

[Tool]
public partial class ZombieGargantuarPresentBoxSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _presentBoxHead;

	private AdobeAnimateSpriteBase _presentBoxImpHead;

	public AdobeAnimateSpriteBase presentBoxHead => _presentBoxHead;

	public AdobeAnimateSpriteBase presentBoxImpHead => _presentBoxImpHead;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_presentBoxHead = GetNode<AdobeAnimateSpriteBase>("%PresentBoxHead");
			_presentBoxImpHead = GetNode<AdobeAnimateSpriteBase>("%PresentBoxImpHead");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (GodotObject.IsInstanceValid(_presentBoxHead) && _presentBoxHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_presentBoxHead);
		}
		if (GodotObject.IsInstanceValid(_presentBoxImpHead) && _presentBoxImpHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_presentBoxImpHead);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (GodotObject.IsInstanceValid(_presentBoxHead))
		{
			_presentBoxHead.ResetAnimation();
		}
		if (GodotObject.IsInstanceValid(_presentBoxImpHead))
		{
			_presentBoxImpHead.ResetAnimation();
		}
	}
}
