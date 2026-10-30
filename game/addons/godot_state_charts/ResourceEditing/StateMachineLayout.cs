using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class StateMachineLayout : Resource
{
	[Export(PropertyHint.None, "")]
	public Dictionary<string, Vector2> Positions { get; set; } = new Dictionary<string, Vector2>();

	[Export(PropertyHint.None, "")]
	public Dictionary<string, bool> Collapsed { get; set; } = new Dictionary<string, bool>();

	[Export(PropertyHint.None, "")]
	public Array<StateMachineLayoutComment> Comments { get; set; } = new Array<StateMachineLayoutComment>();

	[Export(PropertyHint.None, "")]
	public Vector2 ScrollOffset { get; set; }

	[Export(PropertyHint.None, "")]
	public float Zoom { get; set; } = 1f;
}
