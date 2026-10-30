using Godot;

[GlobalClass]
[Tool]
public partial class EffectCreateComponentDefinition : CharacterComponentDefinition
{
	public const int DefaultMaxEffectCount = 100;

	[Export(PropertyHint.Range, "-1,1000,1")]
	public int maxEffectCount = 100;

	[Export(PropertyHint.None, "")]
	public bool applyLimitToIceTrap;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new EffectCreateComponent();
	}
}
