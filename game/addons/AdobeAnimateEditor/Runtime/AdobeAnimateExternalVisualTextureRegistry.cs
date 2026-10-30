using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AdobeAnimateExternalVisualTextureRegistry : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<Texture2D> Textures { get; set; } = new Array<Texture2D>();
}
