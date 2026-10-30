using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class BuffComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public Array<BuffVisualDefinition> visualDefinitions = new Array<BuffVisualDefinition>();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new BuffComponent();
	}
}
