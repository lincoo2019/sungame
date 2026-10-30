using Godot;

[GlobalClass]
[Tool]
public partial class CustomVisualComponentDefinition : CharacterComponentDefinition
{
	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CustomVisualComponent();
	}
}
