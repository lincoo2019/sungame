using Godot;

[Tool]
public partial class ZombieNormalWallnutSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase head;

	private bool _headReady;

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
		if (AdobeAnimateSprite.IsEditorContext || !GodotObject.IsInstanceValid(head))
		{
			return;
		}
		if (!_headReady)
		{
			if (!head.IsRuntimeReadyCached)
			{
				return;
			}
			_headReady = true;
		}
		SyncRuntimeChildState(head);
	}
}
