using Godot;

[Tool]
[GlobalClass]
public partial class FireComponentProjectileWeightItem : Resource
{
	[Export(PropertyHint.None, "")]
	public FireComponentProjectileResource projectileResource;

	[Export(PropertyHint.None, "")]
	public float weight = 1f;
}
