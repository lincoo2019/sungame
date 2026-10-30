using Godot;

[GlobalClass]
public partial class AdobeAnimateAtlasProfile : Resource
{
	[Export(PropertyHint.None, "")]
	public string ProfileId { get; set; } = "";

	[Export(PropertyHint.File, "*.tres")]
	public string ManifestPath { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public bool StartupOnly { get; set; }

	public string GetStableCacheKey()
	{
		if (!string.IsNullOrWhiteSpace(ProfileId))
		{
			return ProfileId.Trim();
		}
		if (!string.IsNullOrWhiteSpace(base.ResourcePath))
		{
			return base.ResourcePath;
		}
		return ManifestPath ?? string.Empty;
	}

	public AdobeAnimateGlobalAtlasManifest LoadManifest()
	{
		if (string.IsNullOrWhiteSpace(ManifestPath))
		{
			return null;
		}
		return ResourceLoader.Load<AdobeAnimateGlobalAtlasManifest>(ManifestPath, null, ResourceLoader.CacheMode.Reuse);
	}
}
