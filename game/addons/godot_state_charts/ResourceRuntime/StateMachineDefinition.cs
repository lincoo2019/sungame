using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class StateMachineDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public int SchemaVersion { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public string DefinitionId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public StateMachineDefinition BaseDefinition { get; set; }

	[Export(PropertyHint.None, "")]
	public string RootStateId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public Array<StateMachineStateDefinition> States { get; set; } = new Array<StateMachineStateDefinition>();

	[Export(PropertyHint.None, "")]
	public Array<StateMachineTransitionDefinition> Transitions { get; set; } = new Array<StateMachineTransitionDefinition>();

	[Export(PropertyHint.None, "")]
	public Dictionary<string, string> Aliases { get; set; } = new Dictionary<string, string>();
}
