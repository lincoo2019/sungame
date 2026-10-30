using Godot;

[Tool]
public partial class StateMachineLayoutComment : Resource
{
	[Export(PropertyHint.None, "")]
	public string StableId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string Text { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public Vector2 Position { get; set; }

	[Export(PropertyHint.None, "")]
	public Vector2 Size { get; set; } = new Vector2(240f, 120f);
}
