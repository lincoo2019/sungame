using Godot;

[GlobalClass]
[Tool]
public partial class RecycleComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,10,0.05")]
	public float defaultPercentage = 0.2f;

	[Export(PropertyHint.None, "")]
	public float horizontalVelocityMin = -50f;

	[Export(PropertyHint.None, "")]
	public float horizontalVelocityMax = 50f;

	[Export(PropertyHint.None, "")]
	public float verticalVelocity = -400f;

	[Export(PropertyHint.None, "")]
	public float sunGravity = 980f;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new RecycleComponent();
	}
}
