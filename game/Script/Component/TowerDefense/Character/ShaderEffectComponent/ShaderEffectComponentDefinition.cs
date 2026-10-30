using Godot;

[GlobalClass]
[Tool]
public partial class ShaderEffectComponentDefinition : CharacterComponentDefinition
{
	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ShaderEffectComponent();
	}
}
