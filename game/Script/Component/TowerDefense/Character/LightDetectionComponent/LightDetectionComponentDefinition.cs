using Godot;

[GlobalClass]
[Tool]
public partial class LightDetectionComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,8,1")]
	public int detectionRadius = 1;

	[Export(PropertyHint.None, "")]
	public bool includeCenter = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new LightDetectionComponent();
	}
}
