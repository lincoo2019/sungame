using Godot;

[GlobalClass]
[Tool]
public partial class GroundMoveComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public NodePath groundSlotPath = new NodePath("GroundSlot");

	[Export(PropertyHint.None, "")]
	public float delay = 0.2f;

	[Export(PropertyHint.None, "")]
	public bool moveYAxis;

	[Export(PropertyHint.None, "")]
	public bool syncNetworkPosition = true;

	[Export(PropertyHint.Range, "0,512,0.1,or_greater")]
	public float networkSnapDistance = 32f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float networkCorrectionWeight = 0.35f;

	[Export(PropertyHint.None, "")]
	public StringName groundLayerName { get; set; }

	[Export(PropertyHint.None, "")]
	public Vector2 groundLayerOffset { get; set; } = Vector2.Zero;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new GroundMoveComponent();
	}
}
