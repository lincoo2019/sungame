using Godot;

[GlobalClass]
[Tool]
public partial class FireComponentExtendCactusDefinition : CharacterComponentDefinition
{
	[ExportGroup("References", "")]
	[Export(PropertyHint.None, "")]
	public string fireComponentInstanceId { get; set; } = "character.fire";

	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string upAnimeClips { get; set; } = "Up";

	[Export(PropertyHint.None, "")]
	public float upAnimeTimeScale { get; set; } = 2f;

	[Export(PropertyHint.None, "")]
	public string downAnimeClips { get; set; } = "Down";

	[Export(PropertyHint.None, "")]
	public float downAnimeTimeScale { get; set; } = 2f;

	[Export(PropertyHint.None, "")]
	public string upFireAnimeClips { get; set; } = "UpFire";

	[Export(PropertyHint.None, "")]
	public string downFireAnimeClips { get; set; } = "Fire";

	[Export(PropertyHint.None, "")]
	public float transitionAnimationStartPosition { get; set; } = 0.2f;

	[ExportGroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName upStateEvent { get; set; } = "ToUp";

	[Export(PropertyHint.None, "")]
	public StringName downStateEvent { get; set; } = "ToDown";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new FireComponentExtendCactus();
	}
}
