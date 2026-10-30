using Godot;

[Tool]
[GlobalClass]
public partial class PumpSledSprite : AdobeAnimateSpriteBase
{
	public AdobeAnimateSpriteBase Back;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			Back = GetNode<AdobeAnimateSpriteBase>("%Back");
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		Back.frameIndex = frameIndex;
		Back.elapsedTimer = elapsedTimer;
	}

	public override void SetClip(string clip)
	{
		base.SetClip(clip);
		if (GodotObject.IsInstanceValid(Back))
		{
			Back.SetClip(clip);
		}
	}
}
