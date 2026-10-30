using Godot;

[Tool]
public partial class ZombieBalloonJacksonSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _propeller;

	public bool head = true;

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
		if (head && _propeller.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_propeller);
		}
	}

	public override void ResetAnimation()
	{
		base.ResetAnimation();
		if (head)
		{
			_propeller.ResetAnimation();
		}
	}
}
