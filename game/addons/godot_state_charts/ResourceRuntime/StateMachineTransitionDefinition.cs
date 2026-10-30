using Godot;

[GlobalClass]
[Tool]
public partial class StateMachineTransitionDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public string StableId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string SourceStateId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string TargetStateId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public StateMachineTriggerKind TriggerKind { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName EventName { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public double DelaySeconds { get; set; }

	[Export(PropertyHint.None, "")]
	public int Priority { get; set; }

	[Export(PropertyHint.None, "")]
	public int DeclarationOrder { get; set; }

	[Export(PropertyHint.None, "")]
	public Resource GuardDefinition { get; set; }
}
