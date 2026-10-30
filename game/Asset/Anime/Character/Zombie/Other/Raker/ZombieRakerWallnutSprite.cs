using Godot;

[Tool]
public partial class ZombieRakerWallnutSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase head;

	private bool _headReady;

	private AdobeAnimateSlot _rakeBackSlot;

	private AdobeAnimatePart _rakeBack;

	public override void _Ready()
	{
		base._Ready();
		_rakeBackSlot = GetNode<AdobeAnimateSlot>("RakeHeadBackSlot");
		_rakeBack = _rakeBackSlot.GetNode<AdobeAnimatePart>("RakeHeadBack");
		RefreshRakeAttachment();
		if (!AdobeAnimateSprite.IsEditorContext)
		{
			head = GetNode<AdobeAnimateSpriteBase>("%Head");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		RefreshRakeAttachment();
		if (AdobeAnimateSprite.IsEditorContext || !GodotObject.IsInstanceValid(head) || head.GetParent() != this)
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

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (AdobeAnimateSprite.IsEditorContext)
		{
			RefreshRakeAttachment();
		}
	}

	public void RefreshRakeAttachment()
	{
		ZombieRakerSprite.RefreshAttachment(this, _rakeBackSlot, _rakeBack);
	}
}
