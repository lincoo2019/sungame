using Godot;

[GlobalClass]
[Tool]
public partial class StateMachineStateDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public string StableId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public StringName DisplayName { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public StateMachineStateKind Kind { get; set; }

	[Export(PropertyHint.None, "")]
	public string ParentId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string InitialChildId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public StateMachineProcessFlags ProcessFlags { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName CallbackKey { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public StringName EnterCallbackKey { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public StringName ExitCallbackKey { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public StringName ProcessCallbackKey { get; set; } = new StringName();

	[Export(PropertyHint.None, "")]
	public StringName PhysicsProcessCallbackKey { get; set; } = new StringName();

	public StringName GetLifecycleCallbackKey(StateMachineCallbackPhase phase)
	{
		StringName stringName = phase switch
		{
			StateMachineCallbackPhase.Enter => EnterCallbackKey, 
			StateMachineCallbackPhase.Exit => ExitCallbackKey, 
			StateMachineCallbackPhase.Process => ProcessCallbackKey, 
			StateMachineCallbackPhase.PhysicsProcess => PhysicsProcessCallbackKey, 
			_ => new StringName(), 
		};
		if (!string.IsNullOrWhiteSpace(stringName.ToString()))
		{
			return stringName;
		}
		return CallbackKey;
	}
}
