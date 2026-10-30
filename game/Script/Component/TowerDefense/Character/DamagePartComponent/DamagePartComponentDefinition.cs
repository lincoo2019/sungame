using Godot;

[GlobalClass]
[Tool]
public partial class DamagePartComponentDefinition : CharacterComponentDefinition
{
	public static readonly Vector2 DefaultRandomVelocityXRange = new Vector2(-100f, 100f);

	public const float DefaultVelocityY = -300f;

	public const float DefaultArmorHeightOffset = 24f;

	public const float DefaultNormalHeightOffset = 0f;

	public const bool DefaultInheritCharacterScale = true;

	[Export(PropertyHint.None, "")]
	public Vector2 randomVelocityXRange = DefaultRandomVelocityXRange;

	[Export(PropertyHint.None, "")]
	public float defaultVelocityY = -300f;

	[Export(PropertyHint.None, "")]
	public float armorHeightOffset = 24f;

	[Export(PropertyHint.None, "")]
	public float normalHeightOffset;

	[Export(PropertyHint.None, "")]
	public bool inheritCharacterScale = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new DamagePartComponent();
	}
}
