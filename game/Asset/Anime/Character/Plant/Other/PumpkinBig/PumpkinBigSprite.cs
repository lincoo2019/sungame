using Godot;

[Tool]
public partial class PumpkinBigSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase back;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			back = GetNode<AdobeAnimateSpriteBase>("%Back");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(back))
		{
			back.frameIndex = frameIndex;
			back.elapsedTimer = elapsedTimer;
		}
	}

	public override void SetClip(string _clip)
	{
		base.SetClip(_clip);
		if (GodotObject.IsInstanceValid(back))
		{
			back.SetClip(_clip);
		}
	}
}
