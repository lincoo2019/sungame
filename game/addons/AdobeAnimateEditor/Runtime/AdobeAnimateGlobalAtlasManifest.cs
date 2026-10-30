using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AdobeAnimateGlobalAtlasManifest : Resource
{
	[Export(PropertyHint.None, "")]
	public string AtlasTextureArrayPath { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Vector2 AtlasTextureArrayLayerSize { get; set; } = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public int AtlasTextureArrayLayerOffset { get; set; }

	[Export(PropertyHint.None, "")]
	public int AtlasTextureArrayLayerCount { get; set; }

	[Export(PropertyHint.None, "")]
	public int AtlasTextureArrayColumns { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<string> SourceKeys { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<int> SourceStarts { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<int> SourceCounts { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<string> SourceSignatures { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public string PoseTextureArrayPath { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Vector2I PoseTextureArrayLayerSize { get; set; } = Vector2I.Zero;

	[Export(PropertyHint.None, "")]
	public int PoseTextureArrayLayerCount { get; set; }

	[Export(PropertyHint.None, "")]
	public int PoseTextureArrayColumns { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<string> PoseAtlasPagePaths { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<int> SourcePoseAtlasPages { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<int> SourcePoseTexelStarts { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<int> SourcePoseTexelCounts { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<string> SourcePoseSignatures { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<int> MediaAtlasPages { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<Rect2> MediaRects { get; set; } = new Array<Rect2>();

	[Export(PropertyHint.None, "")]
	public Array<string> ExternalTexturePaths { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<int> ExternalTextureAtlasPages { get; set; } = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<Rect2> ExternalTextureRects { get; set; } = new Array<Rect2>();
}
