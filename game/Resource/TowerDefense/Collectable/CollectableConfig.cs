using Godot;

[GlobalClass]
public partial class CollectableConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string saveKey;

	[Export(PropertyHint.None, "")]
	public Resource config;
}
