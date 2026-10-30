using Godot;

[Tool]
public partial class ZombieBalloonColourSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _propeller;

	public bool head = true;

	public AdobeAnimateSpriteBase propeller => _propeller;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_propeller = GetNode<AdobeAnimateSpriteBase>("%Propeller");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (head && GodotObject.IsInstanceValid(_propeller) && _propeller.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_propeller);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (head && GodotObject.IsInstanceValid(_propeller))
		{
			_propeller.ResetAnimation();
		}
	}
}
