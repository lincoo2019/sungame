using System;
using Godot;

[Tool]
[GlobalClass]
public partial class AabbProbe2D : Node2D, IAabbCollisionPreview2D
{
	private bool _collisionPreviewDraw;

	[Export(PropertyHint.None, "")]
	public bool Enabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public AabbProbeShape2D Shape { get; set; }

	[Export(PropertyHint.None, "")]
	public Vector2 Size { get; set; } = new Vector2(80f, 80f);

	[Export(PropertyHint.None, "")]
	public float Radius { get; set; } = 40f;

	[Export(PropertyHint.None, "")]
	public Vector2 SegmentA { get; set; } = new Vector2(-40f, 0f);

	[Export(PropertyHint.None, "")]
	public Vector2 SegmentB { get; set; } = new Vector2(40f, 0f);

	public Rect2 WorldRect
	{
		get
		{
			if (!Enabled)
			{
				return AabbShapeUtil.RectFromCenter(base.GlobalPosition, Vector2.Zero);
			}
			return AabbShapeUtil.ComputeProbeWorldRect(this);
		}
	}

	[ExportGroup("Editor Debug", "")]
	[Export(PropertyHint.None, "")]
	public bool editorDraw { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color editorShapeColor { get; set; } = new Color(0.1f, 0.75f, 1f, 0.75f);

	[Export(PropertyHint.None, "")]
	public Color editorAabbColor { get; set; } = new Color(1f, 0.7f, 0.1f, 0.65f);

	[Export(PropertyHint.None, "")]
	public float editorLineWidth { get; set; } = 2f;

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

	public void SetCollisionPreviewDraw(bool enabled)
	{
		_collisionPreviewDraw = enabled;
		if (IsInsideTree())
		{
			SetProcess(Engine.IsEditorHint() || enabled);
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		if ((Engine.IsEditorHint() || _collisionPreviewDraw) && editorDraw)
		{
			DrawProbeShape();
			DrawProbeWorldRect();
		}
	}

	private void DrawProbeShape()
	{
		switch (Shape)
		{
		case AabbProbeShape2D.Circle:
			DrawProbeCircle();
			break;
		case AabbProbeShape2D.Segment:
			DrawLine(SegmentA, SegmentB, editorShapeColor, editorLineWidth);
			break;
		default:
			DrawProbeRectangle();
			break;
		}
	}

	private void DrawProbeRectangle()
	{
		Vector2 vector = Size * 0.5f;
		DrawPolyline(new Vector2[5]
		{
			new Vector2(0f - vector.X, 0f - vector.Y),
			new Vector2(vector.X, 0f - vector.Y),
			new Vector2(vector.X, vector.Y),
			new Vector2(0f - vector.X, vector.Y),
			new Vector2(0f - vector.X, 0f - vector.Y)
		}, editorShapeColor, editorLineWidth);
	}

	private void DrawProbeCircle()
	{
		Vector2[] array = new Vector2[33];
		for (int i = 0; i <= 32; i++)
		{
			float angle = (float)Math.PI * 2f * (float)i / 32f;
			array[i] = Vector2.FromAngle(angle) * Radius;
		}
		DrawPolyline(array, editorShapeColor, editorLineWidth);
	}

	private void DrawProbeWorldRect()
	{
		Rect2 worldRect = WorldRect;
		if (!(worldRect.Size == Vector2.Zero))
		{
			Vector2 position = worldRect.Position;
			Vector2 vector = worldRect.Position + worldRect.Size;
			DrawPolyline(new Vector2[5]
			{
				ToLocal(new Vector2(position.X, position.Y)),
				ToLocal(new Vector2(vector.X, position.Y)),
				ToLocal(new Vector2(vector.X, vector.Y)),
				ToLocal(new Vector2(position.X, vector.Y)),
				ToLocal(new Vector2(position.X, position.Y))
			}, editorAabbColor, editorLineWidth);
		}
	}
}
