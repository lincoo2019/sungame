using Godot;

[GlobalClass]
[Tool]
public partial class DismemberComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Parts", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 velocityXRange { get; set; } = new Vector2(-150f, 150f);

	[Export(PropertyHint.None, "")]
	public Vector2 velocityYRange { get; set; } = new Vector2(-450f, -200f);

	[Export(PropertyHint.None, "")]
	public float heightOffset { get; set; }

	[Export(PropertyHint.Range, "0,5,0.01")]
	public float fadeDuration { get; set; } = 0.5f;

	[Export(PropertyHint.Range, "0,256,0.5")]
	public float minimumPartSize { get; set; }

	[Export(PropertyHint.Range, "0,256,1")]
	public int maxParts { get; set; }

	[Export(PropertyHint.None, "")]
	public Color partColor { get; set; } = Colors.White;

	[Export(PropertyHint.None, "")]
	public bool includeHiddenLayers { get; set; }

	[ExportGroup("Original Visual", "")]
	[Export(PropertyHint.None, "")]
	public bool hideOriginalSprite { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool hideShadow { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool useShadowComponentVisibility { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool clearSpecialDeathFlagsOnDestroy { get; set; } = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new DismemberComponent();
	}
}
