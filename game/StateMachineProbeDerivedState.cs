using Godot;

public partial class StateMachineProbeDerivedState : StateMachineStateDefinition
{
	[Export(PropertyHint.None, "")]
	public string ModPayload { get; set; } = string.Empty;
}
