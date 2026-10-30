using Godot;

[Tool]
[GlobalClass]
public partial class ZombieZamboniIceShroomSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSpriteBase _head;

	public bool shake;

	protected override bool RequiresDisplayFrameBatchPhysicsUpdate => shake;

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
		if (CanRun() && shake)
		{
			base.offset = new Vector2(0f, -100f) + new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1));
			if (GodotObject.IsInstanceValid(_head) && _head.IsRuntimeReadyCached)
			{
				SyncRuntimeChildState(_head);
			}
		}
	}
}
