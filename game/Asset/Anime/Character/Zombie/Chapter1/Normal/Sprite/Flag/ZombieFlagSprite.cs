using Godot;

[Tool]
public partial class ZombieFlagSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase flag;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			flag = GetNode<AdobeAnimateSpriteBase>("%ZombieFlagpole");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (flag != null && flag.IsRuntimeReadyCached)
		{
			SyncRuntimeChildState(flag);
		}
	}
}
