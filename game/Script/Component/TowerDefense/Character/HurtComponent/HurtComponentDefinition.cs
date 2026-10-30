using Godot;

[GlobalClass]
[Tool]
public partial class HurtComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public PackedScene healthEffectScene;

	[Export(PropertyHint.Range, "-1,1000,1")]
	public int maxHealthEffectCount = 100;

	[Export(PropertyHint.None, "")]
	public StringName healthEffectAnimation = "Idle";

	[Export(PropertyHint.None, "")]
	public bool flashOnDamage = true;

	[Export(PropertyHint.None, "")]
	public bool flashOnHeal = true;

	[Export(PropertyHint.None, "")]
	public bool markHealthBarDirty = true;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new HurtComponent();
	}
}
