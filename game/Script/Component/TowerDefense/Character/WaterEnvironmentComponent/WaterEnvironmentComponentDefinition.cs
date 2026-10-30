using Godot;

[GlobalClass]
[Tool]
public partial class WaterEnvironmentComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public NodePath waterLineSpritePath = new NodePath();

	[Export(PropertyHint.None, "")]
	public float waterHeight = 35f;

	[Export(PropertyHint.None, "")]
	public string waterIdleAnime = "";

	[Export(PropertyHint.Range, "0,30,0.1")]
	public float groundHeightLerpSpeed = 3f;

	[Export(PropertyHint.Range, "1,30,1")]
	public int environmentCheckInterval = 5;

	[Export(PropertyHint.None, "")]
	public float entryDiscardOffset = 45f;

	[Export(PropertyHint.None, "")]
	public float exitDiscardStartOffset = 56f;

	[Export(PropertyHint.None, "")]
	public float exitDiscardEndOffset = 86f;

	[Export(PropertyHint.Range, "0,10,0.01")]
	public float exitDuration = 1f;

	[Export(PropertyHint.None, "")]
	public float discardResetPosition = 10000f;

	[Export(PropertyHint.None, "")]
	public StringName discardShaderParameter = "discardDownPos";

	[Export(PropertyHint.None, "")]
	public bool createEntrySplash = true;

	[Export(PropertyHint.None, "")]
	public bool restoreIdleOnExit = true;

	[Export(PropertyHint.None, "")]
	public Tween.EaseType exitEase = Tween.EaseType.Out;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType exitTransition = Tween.TransitionType.Cubic;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new WaterEnvironmentComponent();
	}
}
