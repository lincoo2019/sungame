using Godot;

[Tool]
[GlobalClass]
public partial class ZombieSnorkleTanglekelpSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _head;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_head = GetNode<AdobeAnimateSpriteBase>("%Head");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (_head.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(_head);
		}
	}
}
