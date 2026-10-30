using Godot;

[GlobalClass]
[Tool]
public partial class ArmorVisualComponentDefinition : CharacterComponentDefinition
{
	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ArmorVisualComponent();
	}
}
