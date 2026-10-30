using Godot;

public partial class SuperBigMapBattleZoomGuiProbe : Control
{
	public bool ReceivedPress { get; private set; }

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
		{
			ReceivedPress = true;
			AcceptEvent();
		}
	}
}
