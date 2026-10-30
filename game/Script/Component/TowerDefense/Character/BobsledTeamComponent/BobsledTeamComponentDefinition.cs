using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class BobsledTeamComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "1,4,1")]
	public int slotCount { get; set; } = 4;

	[Export(PropertyHint.None, "")]
	public Array<string> passengerPacketNames { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<NodePath> passengerMarkerPaths { get; set; } = new Array<NodePath>();

	[Export(PropertyHint.None, "")]
	public Array<Vector2> passengerOffsets { get; set; } = new Array<Vector2>();

	[Export(PropertyHint.None, "")]
	public bool hideAttachedPassengers { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public double passengerHeightOffset { get; set; } = 20.0;

	[Export(PropertyHint.None, "")]
	public bool autoSpawnPassengers { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public BobsledOwnerDestroyMode ownerDestroyMode { get; set; }

	public BobsledTeamComponentDefinition()
	{
		base.ComponentTypeId = "BobsledTeamComponent";
		base.InstanceId = "character.bobsled_team";
		base.WireIndex = 0;
	}

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new BobsledTeamComponent();
	}
}
