using Godot;

[GlobalClass]
[Tool]
public partial class GravebusterComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public string landAnimeClips { get; set; } = "Land";

	[Export(PropertyHint.None, "")]
	public float landAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string gravebusterAnimeClips { get; set; } = "Idle";

	[Export(PropertyHint.None, "")]
	public float gravebusterTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public bool requireGravestoneTarget { get; set; } = true;

	[Export(PropertyHint.Range, "0,30,0.01")]
	public float consumeDuration { get; set; } = 5f;

	[Export(PropertyHint.None, "")]
	public float landStartPositionY { get; set; } = -70f;

	[Export(PropertyHint.None, "")]
	public float consumeEndPositionY { get; set; } = -30f;

	[Export(PropertyHint.None, "")]
	public float discardStartOffset { get; set; } = -30f;

	[Export(PropertyHint.None, "")]
	public float discardEndOffset { get; set; }

	[Export(PropertyHint.None, "")]
	public float discardResetPosition { get; set; } = -10000f;

	[Export(PropertyHint.None, "")]
	public StringName discardShaderParameter { get; set; } = "discardUpPos";

	[Export(PropertyHint.None, "")]
	public Tween.EaseType consumeEase { get; set; } = Tween.EaseType.InOut;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType consumeTransition { get; set; }

	[ExportGroup("Audio And Drop", "")]
	[Export(PropertyHint.None, "")]
	public string consumeAudio { get; set; } = "GraveBusterChomp";

	[Export(PropertyHint.None, "")]
	public bool createDrop { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public string dropFeatureName { get; set; } = "Coins";

	[Export(PropertyHint.None, "")]
	public Vector2 dropVelocityXRange { get; set; } = new Vector2(-50f, 50f);

	[Export(PropertyHint.None, "")]
	public float dropVelocityY { get; set; } = -400f;

	[Export(PropertyHint.None, "")]
	public float dropGravity { get; set; } = 980f;

	[Export(PropertyHint.None, "")]
	public StringName gravebusterStateEvent { get; set; } = "ToGravebuster";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new GravebusterComponent();
	}
}
