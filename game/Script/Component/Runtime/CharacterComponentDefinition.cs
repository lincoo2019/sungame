using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public abstract partial class CharacterComponentDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public string ComponentTypeId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string DefinitionId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string InstanceId { get; set; } = string.Empty;

	[Export(PropertyHint.Range, "-1,128,1")]
	public int WireIndex { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public int SchemaVersion { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public bool InitiallyAlive { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public StateMachineDefinition StateMachineDefinition { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<StringName> LegacyNodeNames { get; set; } = new Array<StringName>();

	public abstract CharacterComponentRuntime CreateRuntime();
}
