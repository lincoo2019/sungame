using Godot;
using Godot.Collections;

[Tool]
public partial class ModEditorStateMachineProbeDerivedTransition : StateMachineTransitionDefinition
{
	[Export(PropertyHint.None, "")]
	public string ModTransitionNote { get; set; } = "transition-base";

	[Export(PropertyHint.Range, "0,8,0.05")]
	public double RuntimeScale { get; set; } = 1.25;

	[Export(PropertyHint.None, "")]
	public Color WireColor { get; set; } = new Color("ffb84d");

	[Export(PropertyHint.None, "")]
	public Vector3 RuntimeImpulse { get; set; } = new Vector3(1f, 2f, 3f);

	[Export(PropertyHint.None, "")]
	public Dictionary<string, int> RuntimeCosts { get; set; } = new Dictionary<string, int> { { "energy", 2 } };
}
