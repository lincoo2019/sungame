using Godot;

[GlobalClass]
[Tool]
public partial class ShadowComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool followHeight;

	[Export(PropertyHint.Range, "1,10000,1")]
	public float heightScaleFactor = 900f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float minimumHeightScale;

	[Export(PropertyHint.None, "")]
	public bool preferMultiMesh = true;

	[Export(PropertyHint.None, "")]
	public bool hideWhenInvisible = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ShadowComponent();
	}
}
