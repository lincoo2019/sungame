using Godot;

[GlobalClass]
[Tool]
public partial class DancingComponentDefinition : CharacterComponentDefinition
{
	[ExportSubgroup("Dancer", "")]
	[Export(PropertyHint.None, "")]
	public string dancerPacketName { get; set; } = "ZombieDancer";

	[Export(PropertyHint.Range, "0,30,0.05")]
	public float dancerRiseDuration { get; set; } = 1.5f;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public float dancerWalkSpeedScaleMultiplier { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public bool syncDancerAnimation { get; set; } = true;

	[ExportSubgroup("Timing", "")]
	[Export(PropertyHint.Range, "1,100,1")]
	public int walkTimeInit { get; set; } = 4;

	[Export(PropertyHint.Range, "1,100,1")]
	public int danceTimeInit { get; set; } = 2;

	[Export(PropertyHint.Range, "0,20,0.05")]
	public float moonWalkGridDistance { get; set; } = 2.5f;

	[ExportSubgroup("State Events", "")]
	[Export(PropertyHint.None, "")]
	public StringName pointStateEvent { get; set; } = "ToPoint";

	[Export(PropertyHint.None, "")]
	public StringName moonWalkStateEvent { get; set; } = "ToMoonWalk";

	[Export(PropertyHint.None, "")]
	public StringName danceStateEvent { get; set; } = "ToDance";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[ExportSubgroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string moonWalkAnimeClip { get; set; } = "MoonWalk";

	[Export(PropertyHint.None, "")]
	public float moonWalkAnimeTimeScale { get; set; } = 2f;

	[Export(PropertyHint.None, "")]
	public string armRiseAnimeClip { get; set; } = "ArmRise";

	[Export(PropertyHint.None, "")]
	public float armRiseAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string pointUpAnimeClip { get; set; } = "PointUp";

	[Export(PropertyHint.None, "")]
	public string pointDownAnimeClip { get; set; } = "PointDown";

	[Export(PropertyHint.None, "")]
	public float pointAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.Range, "0,30,0.05")]
	public float pointDownDelay { get; set; } = 0.75f;

	[Export(PropertyHint.None, "")]
	public string walkAnimeClip { get; set; } = "Walk";

	[Export(PropertyHint.None, "")]
	public float dieAnimeTimeScale { get; set; } = 2f;

	[ExportSubgroup("Sprite Scale", "")]
	[Export(PropertyHint.None, "")]
	public float moonWalkSpriteScaleX { get; set; } = -1f;

	[Export(PropertyHint.None, "")]
	public float normalSpriteScaleX { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public float danceSpriteScaleX { get; set; } = -1f;

	[Export(PropertyHint.None, "")]
	public bool armRiseFlipSprite { get; set; } = true;

	[ExportSubgroup("Dependencies", "")]
	[Export(PropertyHint.None, "")]
	public StringName attackComponentName { get; set; } = "AttackComponent";

	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public NodePath spotlightPath { get; set; } = new NodePath();

	[Export(PropertyHint.None, "")]
	public NodePath spotlight2Path { get; set; } = new NodePath();

	[ExportSubgroup("Spotlight", "")]
	[Export(PropertyHint.None, "")]
	public Gradient spotlightGradient { get; set; }

	[Export(PropertyHint.Range, "0,60,0.05")]
	public float spotlightChangeInterval { get; set; } = 3f;

	[Export(PropertyHint.None, "")]
	public string spotlightAudioName { get; set; } = "Dancer";

	[Export(PropertyHint.None, "")]
	public bool spotlightColorCycleEnabled { get; set; } = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new DancingComponent();
	}
}
