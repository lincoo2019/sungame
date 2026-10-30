using Godot;

[GlobalClass]
[Tool]
public partial class SlotComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public NodePath posMarkPath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public bool heightFollow { get; set; }

	[Export(PropertyHint.None, "")]
	public bool hideShadow { get; set; } = true;

	[Export(PropertyHint.Range, "1,30,1")]
	public int occupantRefreshInterval { get; set; } = 3;

	[Export(PropertyHint.None, "")]
	public bool restoreGroundHeightOnRelease { get; set; } = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new SlotComponent();
	}
}
