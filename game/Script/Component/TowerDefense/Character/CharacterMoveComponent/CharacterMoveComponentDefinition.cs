using Godot;

[GlobalClass]
[Tool]
public partial class CharacterMoveComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public Vector2 velocity = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public double gravity;

	[Export(PropertyHint.None, "")]
	public double moveScale = 1.0;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new CharacterMoveComponent();
	}
}
