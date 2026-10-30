using Godot;

[GlobalClass]
[Tool]
public partial class DragMoveComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public bool allowMouseDrag = true;

	[Export(PropertyHint.None, "")]
	public bool allowDirectionalInput = true;

	[Export(PropertyHint.None, "")]
	public StringName pressAction = "Press";

	[Export(PropertyHint.None, "")]
	public StringName moveUpAction = "P1Up";

	[Export(PropertyHint.None, "")]
	public StringName moveDownAction = "P1Down";

	[Export(PropertyHint.None, "")]
	public StringName moveLeftAction = "P1Left";

	[Export(PropertyHint.None, "")]
	public StringName moveRightAction = "P1Right";

	[Export(PropertyHint.Range, "1,300,1")]
	public int requestRetryPhysicsFrames = 30;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new DragMoveComponent();
	}
}
