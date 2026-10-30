using Godot;

[Tool]
[GlobalClass]
public abstract partial class BehaviorDefinitionBase : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName BehaviorTypeId { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public StringName DefinitionId { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public StringName InstanceId { get; set; } = "";

	[Export(PropertyHint.Range, "1,65535,1")]
	public int SchemaVersion { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public bool InitiallyEnabled { get; set; } = true;

	public string GetDiagnosticName()
	{
		if (InstanceId != null && !InstanceId.IsEmpty)
		{
			return InstanceId.ToString();
		}
		if (DefinitionId != null && !DefinitionId.IsEmpty)
		{
			return DefinitionId.ToString();
		}
		if (BehaviorTypeId != null && !BehaviorTypeId.IsEmpty)
		{
			return BehaviorTypeId.ToString();
		}
		return GetType().Name;
	}
}
