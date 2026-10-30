using Godot;

[GlobalClass]
[Tool]
public partial class ImpFlightComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public string flyAudio = "Imp";

	[Export(PropertyHint.Range, "0,4,0.05")]
	public float flyAnimationSpeedScale = 0.8f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double animationBlend = 0.2;

	[Export(PropertyHint.None, "")]
	public StringName flyEvent = "ToFly";

	[Export(PropertyHint.None, "")]
	public StringName landEvent = "ToLand";

	[Export(PropertyHint.None, "")]
	public StringName idleEvent = "ToIdle";

	[Export(PropertyHint.None, "")]
	public StringName flyStateName = "Fly";

	[Export(PropertyHint.None, "")]
	public bool suppressCollisionInFlight = true;

	[Export(PropertyHint.None, "")]
	public bool restoreCollisionOnDisable = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ImpFlightComponent();
	}
}
