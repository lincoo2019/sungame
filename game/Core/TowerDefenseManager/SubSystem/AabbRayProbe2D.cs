using Godot;

[Tool]
[GlobalClass]
public partial class AabbRayProbe2D : Node2D, IAabbCollisionPreview2D
{
	private Vector2 _targetPosition = new Vector2(2000f, 0f);

	private bool _collisionPreviewDraw;

	[Export(PropertyHint.None, "")]
	public Vector2 targetPosition
	{
		get
		{
			return _targetPosition;
		}
		set
		{
			_targetPosition = value;
			QueueRedraw();
		}
	}

	[ExportGroup("Editor Debug", "")]
	[Export(PropertyHint.None, "")]
	public bool editorDraw { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color editorRayColor { get; set; } = new Color(0.2f, 1f, 0.25f, 0.8f);

	[Export(PropertyHint.None, "")]
	public float editorLineWidth { get; set; } = 2f;

	[Export(PropertyHint.None, "")]
	public float editorEndpointRadius { get; set; } = 5f;

	[Export(PropertyHint.None, "")]
	public float editorArrowSize { get; set; } = 14f;

	public override void _Ready()
	{
		SetProcess(Engine.IsEditorHint() || _collisionPreviewDraw);
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint() || _collisionPreviewDraw)
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if ((Engine.IsEditorHint() || _collisionPreviewDraw) && editorDraw)
		{
			DrawLine(Vector2.Zero, targetPosition, editorRayColor, editorLineWidth);
			DrawCircle(Vector2.Zero, editorEndpointRadius, editorRayColor);
			DrawCircle(targetPosition, editorEndpointRadius, editorRayColor);
			DrawArrowHead();
		}
	}

	public void SetCollisionPreviewDraw(bool enabled)
	{
		_collisionPreviewDraw = enabled;
		if (IsInsideTree())
		{
			SetProcess(Engine.IsEditorHint() || enabled);
		}
		QueueRedraw();
	}

	private void DrawArrowHead()
	{
		Vector2 vector = targetPosition;
		if (!(vector.LengthSquared() < 1E-06f))
		{
			Vector2 vector2 = vector.Normalized();
			Vector2 vector3 = vector2.Orthogonal();
			Vector2 vector4 = targetPosition - vector2 * editorArrowSize;
			DrawPolygon(new Vector2[3]
			{
				targetPosition,
				vector4 + vector3 * editorArrowSize * 0.45f,
				vector4 - vector3 * editorArrowSize * 0.45f
			}, new Color[1] { editorRayColor });
		}
	}
}
