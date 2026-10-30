using Godot;

[Tool]
public partial class ModEditorStateMachineProbeDerivedDefinition : StateMachineDefinition
{
	[Export(PropertyHint.None, "")]
	public string ModAuthorNote { get; set; } = "derived-before";
}
