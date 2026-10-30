using Godot;

[GlobalClass]
public partial class CameraMovingControl : Node
{
	private int WINDOW_WIDTH = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");

	private int WINDOW_HEIGHT = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");

	[Export(PropertyHint.None, "")]
	public bool alive = true;

	[Export(PropertyHint.None, "")]
	public Camera2D camera;

	[Export(PropertyHint.None, "")]
	public Vector2 cameraZoomMin = new Vector2(1.5f, 1.5f);

	[Export(PropertyHint.None, "")]
	public Vector2 cameraZoomMax = new Vector2(1f, 1f);

	[Export(PropertyHint.None, "")]
	public Marker2D edgeMarkUL;

	[Export(PropertyHint.None, "")]
	public Marker2D edgeMarkDR;

	public bool mousePress;

	public Vector2 mouseSavePos = Vector2.Zero;

	public override void _Ready()
	{
	}

	public override void _Input(InputEvent _event)
	{
		if (alive)
		{
			if (Input.IsActionJustReleased("RollUp"))
			{
				camera.Zoom += Vector2.One * 0.05f;
			}
			if (Input.IsActionJustReleased("RollDown"))
			{
				camera.Zoom -= Vector2.One * 0.05f;
			}
			if (Input.IsActionJustPressed("Press"))
			{
				mousePress = true;
				mouseSavePos = GetViewport().GetMousePosition();
			}
			if (Input.IsActionJustReleased("Press"))
			{
				mousePress = false;
			}
			if (mousePress)
			{
				camera.GlobalPosition += mouseSavePos - GetViewport().GetMousePosition();
				mouseSavePos = GetViewport().GetMousePosition();
			}
			camera.Zoom = new Vector2(Mathf.Clamp(camera.Zoom.X, cameraZoomMax.X, cameraZoomMin.X), Mathf.Clamp(camera.Zoom.Y, cameraZoomMax.Y, cameraZoomMin.Y));
			camera.GlobalPosition = new Vector2(Mathf.Clamp(camera.GlobalPosition.X, edgeMarkUL.GlobalPosition.X, edgeMarkDR.GlobalPosition.X - (float)WINDOW_WIDTH / camera.Zoom.X), Mathf.Clamp(camera.GlobalPosition.Y, edgeMarkUL.GlobalPosition.Y, edgeMarkDR.GlobalPosition.Y - (float)WINDOW_HEIGHT / camera.Zoom.Y));
		}
	}
}
