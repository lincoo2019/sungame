using Godot;

[GlobalClass]
[Tool]
public partial class PotatoComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.Range, "0,300,0.1")]
	public float readyTime { get; set; } = 15f;

	[Export(PropertyHint.None, "")]
	public StringName readyTimerName { get; set; } = "Ready";

	[Export(PropertyHint.None, "")]
	public bool autoRise { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool componentStateUse { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public PackedScene riseEffect { get; set; }

	[Export(PropertyHint.None, "")]
	public string riseAudio { get; set; } = "GravestoneRumble";

	[Export(PropertyHint.None, "")]
	public bool createRiseEffect { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool setSmashInvincibleOnCharge { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool destroyAfterExplode { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool autoExplodeOnCharge { get; set; } = true;

	[ExportSubgroup("StateEvents", "")]
	[Export(PropertyHint.None, "")]
	public StringName readyStateEvent { get; set; } = "ToReady";

	[Export(PropertyHint.None, "")]
	public StringName riseStateEvent { get; set; } = "ToRise";

	[Export(PropertyHint.None, "")]
	public StringName chargeStateEvent { get; set; } = "ToCharge";

	[Export(PropertyHint.None, "")]
	public StringName idleStateEvent { get; set; } = "ToIdle";

	[ExportSubgroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public string readyAnimeClips { get; set; } = "Ready";

	[Export(PropertyHint.None, "")]
	public float readyAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string riseAnimeClips { get; set; } = "Rise";

	[Export(PropertyHint.None, "")]
	public float riseAnimeTimeScale { get; set; } = 1f;

	[Export(PropertyHint.None, "")]
	public string chargeAnimeClips { get; set; } = "Idle";

	[Export(PropertyHint.None, "")]
	public float chargeAnimeTimeScale { get; set; } = 1f;

	[ExportSubgroup("Dependencies", "")]
	[Export(PropertyHint.None, "")]
	public StringName attackComponentName { get; set; } = "AttackComponent";

	[Export(PropertyHint.None, "")]
	public StringName explodeComponentName { get; set; } = "ExplodeComponent";

	[Export(PropertyHint.None, "")]
	public NodePath spritePath { get; set; } = new NodePath();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new PotatoComponent();
	}
}
