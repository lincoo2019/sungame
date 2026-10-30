using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class TanglekelpComponentDefinition : CharacterComponentDefinition
{
	[ExportSubgroup("Dependencies", "")]
	[Export(PropertyHint.None, "")]
	public StringName attackComponentName { get; set; } = "AttackComponent";

	[ExportSubgroup("Drag", "")]
	[Export(PropertyHint.Range, "1,100,1")]
	public int grabNum { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public bool destroyUse { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool targetDestroyUse { get; set; } = true;

	[Export(PropertyHint.Range, "0,10,0.01")]
	public float dragDelay { get; set; } = 0.5f;

	[Export(PropertyHint.None, "")]
	public string dragAudio { get; set; } = "Floop";

	[Export(PropertyHint.None, "")]
	public string entrySplashAudio { get; set; } = "PlantWater";

	[Export(PropertyHint.None, "")]
	public string submergeAudio { get; set; } = "ZombieEnteringWater";

	[Export(PropertyHint.None, "")]
	public bool createTargetSplash { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool createParentSplash { get; set; } = true;

	[ExportSubgroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName dragStateEvent { get; set; } = "ToDrag";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[ExportSubgroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public PackedScene grabSpriteScene { get; set; }

	[Export(PropertyHint.None, "")]
	public string grabAnimeClips { get; set; } = "Grab";

	[Export(PropertyHint.Range, "0.01,10,0.01")]
	public float grabAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public Array<string> grabFliterOpen { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> grabFliterClose { get; set; } = new Array<string>();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new TanglekelpComponent();
	}
}
