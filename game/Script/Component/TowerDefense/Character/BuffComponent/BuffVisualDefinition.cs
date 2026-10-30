using Godot;

[GlobalClass]
[Tool]
public partial class BuffVisualDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName buffKey;

	[Export(PropertyHint.None, "")]
	public bool enabled = true;

	[Export(PropertyHint.None, "")]
	public Texture2D texture;

	[Export(PropertyHint.None, "")]
	public PackedScene scene;

	[Export(PropertyHint.None, "")]
	public StringName nodeName;

	[Export(PropertyHint.None, "")]
	public Vector2 position;

	[Export(PropertyHint.None, "")]
	public Vector2 scale = Vector2.One;

	[Export(PropertyHint.None, "")]
	public float rotation;

	[Export(PropertyHint.Range, "-4096,4096,1")]
	public int zIndex;

	[Export(PropertyHint.None, "")]
	public bool zAsRelative = true;

	[Export(PropertyHint.None, "")]
	public bool centered = true;

	[Export(PropertyHint.None, "")]
	public Vector2 offset;

	[Export(PropertyHint.None, "")]
	public AdobeAnimateExternalVisualDrawBand drawBand = AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation;
}
