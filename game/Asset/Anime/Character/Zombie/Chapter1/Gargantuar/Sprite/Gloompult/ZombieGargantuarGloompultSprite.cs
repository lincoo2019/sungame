using Godot;

[Tool]
public partial class ZombieGargantuarGloompultSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _gloompultHead;

	private AdobeAnimateSpriteBase _puffShroomImpHead;

	public AdobeAnimateSpriteBase gloompultHead => _gloompultHead;

	public AdobeAnimateSpriteBase puffShroomImpHead => _puffShroomImpHead;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_gloompultHead = GetNode<AdobeAnimateSpriteBase>("%GloompultHead");
			_puffShroomImpHead = GetNode<AdobeAnimateSpriteBase>("%PuffShroomImpHead");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (GodotObject.IsInstanceValid(_gloompultHead) && _gloompultHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_gloompultHead);
		}
		if (GodotObject.IsInstanceValid(_puffShroomImpHead) && _puffShroomImpHead.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_puffShroomImpHead);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (GodotObject.IsInstanceValid(_gloompultHead))
		{
			_gloompultHead.ResetAnimation();
		}
		if (GodotObject.IsInstanceValid(_puffShroomImpHead))
		{
			_puffShroomImpHead.ResetAnimation();
		}
	}
}
