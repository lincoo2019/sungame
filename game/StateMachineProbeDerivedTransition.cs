using Godot;

public partial class StateMachineProbeDerivedTransition : StateMachineTransitionDefinition
{
	[Export(PropertyHint.None, "")]
	public int ModWeight { get; set; }
}
