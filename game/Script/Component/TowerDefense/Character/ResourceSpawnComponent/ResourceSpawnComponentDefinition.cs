using Godot;

[GlobalClass]
[Tool]
public partial class ResourceSpawnComponentDefinition : CharacterComponentDefinition
{
	public static readonly Vector2 DefaultRandomVelocityXRange = new Vector2(-50f, 50f);

	public const float DefaultLaunchVelocityY = -400f;

	public const float DefaultGroundHeightMultiplier = 2f;

	[Export(PropertyHint.None, "")]
	public Vector2 randomVelocityXRange = DefaultRandomVelocityXRange;

	[Export(PropertyHint.None, "")]
	public float defaultLaunchVelocityY = -400f;

	[Export(PropertyHint.Range, "0,10,0.01")]
	public float groundHeightMultiplier = 2f;

	[Export(PropertyHint.Range, "0,1000,1")]
	public int maxEffectCount = 100;

	[Export(PropertyHint.None, "")]
	public PackedScene healthEffectScene;

	[Export(PropertyHint.None, "")]
	public string healthEffectClip = "Idle";

	[Export(PropertyHint.None, "")]
	public bool showHealthEffect = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ResourceSpawnComponent();
	}
}
