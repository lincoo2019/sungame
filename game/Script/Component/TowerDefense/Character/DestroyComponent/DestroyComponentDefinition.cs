using Godot;

[GlobalClass]
[Tool]
public partial class DestroyComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,10,0.01")]
	public float destroyDelay = 1f;

	[Export(PropertyHint.Range, "0.01,1,0.01")]
	public float smashScaleY = 0.25f;

	[Export(PropertyHint.None, "")]
	public StringName ashShaderParameter = "ash";

	[Export(PropertyHint.None, "")]
	public bool waitForPhysicsFrame = true;

	[Export(PropertyHint.None, "")]
	public bool pauseSpriteOnSpecialDestroy = true;

	[Export(PropertyHint.None, "")]
	public bool hideShadowOnSmash = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new DestroyComponent();
	}
}
