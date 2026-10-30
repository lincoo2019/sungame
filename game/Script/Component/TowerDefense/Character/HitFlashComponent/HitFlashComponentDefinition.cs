using Godot;

[GlobalClass]
[Tool]
public partial class HitFlashComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public StringName brightShaderParameter = "brightStrength";

	[Export(PropertyHint.None, "")]
	public StringName whiteShaderParameter = "whiteStrength";

	[Export(PropertyHint.None, "")]
	public bool restartBrightOnHit;

	[Export(PropertyHint.None, "")]
	public bool restartWhiteOnHit = true;

	[Export(PropertyHint.None, "")]
	public float minimumTweenDuration = 0.001f;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new HitFlashComponent();
	}
}
