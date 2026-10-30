using Godot;
using Godot.Collections;

[Tool]
public partial class ModEditorStateMachineProbeDerivedState : StateMachineStateDefinition
{
	[Export(PropertyHint.None, "")]
	public string ModStateNote { get; set; } = "state-base";

	[Export(PropertyHint.Range, "0,100,1")]
	public int RuntimeWeight { get; set; } = 3;

	[Export(PropertyHint.None, "")]
	public bool RuntimeEnabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color AccentColor { get; set; } = new Color("62d6ff");

	[Export(PropertyHint.None, "")]
	public Vector2 PreviewOffset { get; set; } = new Vector2(12f, -4f);

	[Export(PropertyHint.None, "")]
	public Array<string> RuntimeTags { get; set; } = new Array<string> { "probe", "state" };
}
