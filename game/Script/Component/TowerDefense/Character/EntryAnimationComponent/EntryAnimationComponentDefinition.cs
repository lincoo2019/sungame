using Godot;

[GlobalClass]
[Tool]
public partial class EntryAnimationComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Fall", "")]
	[Export(PropertyHint.None, "")]
	public float defaultFallHeight = 900f;

	[Export(PropertyHint.Range, "0,10,0.01,or_greater")]
	public float defaultFallDelay;

	[Export(PropertyHint.Range, "0.001,10,0.001,or_greater")]
	public float defaultFallDuration = 0.25f;

	[ExportGroup("Bounce", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 preImpactScale = new Vector2(0.75f, 1.25f);

	[Export(PropertyHint.None, "")]
	public Vector2 impactScale = new Vector2(1.5f, 0.5f);

	[Export(PropertyHint.None, "")]
	public Vector2 settleScale = Vector2.One;

	[Export(PropertyHint.Range, "0.001,2,0.001,or_greater")]
	public float impactDuration = 0.1f;

	[Export(PropertyHint.Range, "0.001,2,0.001,or_greater")]
	public float settleDuration = 0.2f;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new EntryAnimationComponent();
	}
}
