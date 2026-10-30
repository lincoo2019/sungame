using Godot;

public partial class StateMachineResourceRuntimeExtensionTransition : StateMachineTransitionDefinition
{
	[Export(PropertyHint.None, "")]
	public string ProbeTransitionValue { get; set; } = string.Empty;
}
