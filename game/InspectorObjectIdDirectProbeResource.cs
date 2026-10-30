using Godot;

[Tool]
public partial class InspectorObjectIdDirectProbeResource : Resource
{
	[Export(PropertyHint.ObjectId, "")]
	public long TargetObjectId { get; set; }
}
