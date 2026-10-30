using Godot;

public partial class XWInlineTextProbeResource : Resource
{
	[Export(PropertyHint.None, "")]
	public string Title { get; set; } = "Original title";

	[Export(PropertyHint.MultilineText, "")]
	public string Description { get; set; } = "Original description";

	[Export(PropertyHint.None, "")]
	public string DialogueText { get; set; } = "Original dialogue";

	[Export(PropertyHint.None, "")]
	public string HintText { get; set; } = "Original hint";

	[Export(PropertyHint.None, "")]
	public string ButtonText { get; set; } = "Original button";

	[Export(PropertyHint.None, "")]
	public StringName SaveKey { get; set; } = new StringName("probe_key");

	[Export(PropertyHint.Enum, "Never,Night,Day")]
	public string SleepMode { get; set; } = "Never";

	[Export(PropertyHint.None, "")]
	public NodePath TargetPath { get; set; } = new NodePath("PreviewRoot/Target");

	[Export(PropertyHint.File, "*.tres,*.res,*.tscn")]
	public string ResourceFile { get; set; } = "res://";
}
