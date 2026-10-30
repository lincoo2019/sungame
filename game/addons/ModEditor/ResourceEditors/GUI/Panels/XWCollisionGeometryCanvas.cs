using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWCollisionGeometryCanvas : Control
{
	public enum GeometryKind
	{
		HitBox,
		Shape,
		Ray
	}

	private const float Zoom = 1f;

	private const float HandleRadius = 8f;

	private GeometryKind _kind;

	private Transform2D _transform = Transform2D.Identity;

	private Vector2 _geometrySize = new Vector2(80f, 80f);

	private Vector2 _rayTarget = new Vector2(200f, 0f);

	private Shape2D _shape;

	private Color _fill = new Color(0.1f, 0.75f, 1f, 0.2f);

	private Color _outline = new Color(0.2f, 0.9f, 1f, 0.95f);

	private float _lineWidth = 2f;

	private bool _enabled = true;

	private bool _dragOrigin;

	private bool _dragGeometry;

	private Vector2 _previewOrigin;

	private Vector2 _previewGeometry;

	public Action<Vector2> OriginCommitted { get; set; }

	public Action<Vector2> GeometryCommitted { get; set; }

	public XWCollisionGeometryCanvas()
	{
		base.CustomMinimumSize = new Vector2(380f, 330f);
		base.MouseDefaultCursorShape = CursorShape.Cross;
		base.FocusMode = FocusModeEnum.Click;
		base.ClipContents = true;
	}

	public void DisplayHitBox(CharacterHitBoxDefinition resource)
	{
		_kind = GeometryKind.HitBox;
		_transform = resource?.LocalTransform ?? Transform2D.Identity;
		_geometrySize = resource?.Size ?? new Vector2(80f, 80f);
		_shape = null;
		_enabled = resource?.DefaultEnabled ?? false;
		_fill = resource?.DebugFillColor ?? new Color(0.1f, 0.75f, 1f, 0.2f);
		_outline = resource?.DebugOutlineColor ?? new Color(0.2f, 0.9f, 1f, 0.95f);
		_lineWidth = resource?.DebugLineWidth ?? 2f;
		ResetPreview();
	}

	public void DisplayShape(AabbShape2DResource resource)
	{
		_kind = GeometryKind.Shape;
		_transform = resource?.LocalTransform ?? Transform2D.Identity;
		_shape = resource?.Geometry;
		_geometrySize = GetShapeSize(_shape);
		_enabled = resource?.Enabled ?? false;
		_fill = resource?.DebugFillColor ?? new Color(0.1f, 0.75f, 1f, 0.2f);
		_outline = resource?.DebugOutlineColor ?? new Color(0.2f, 0.9f, 1f, 0.95f);
		_lineWidth = resource?.DebugLineWidth ?? 2f;
		ResetPreview();
	}

	public void DisplayRay(AabbRay2DResource resource)
	{
		_kind = GeometryKind.Ray;
		_transform = resource?.LocalTransform ?? Transform2D.Identity;
		_rayTarget = resource?.TargetPosition ?? new Vector2(200f, 0f);
		_enabled = resource?.Enabled ?? false;
		_fill = resource?.DebugColor ?? new Color(0.2f, 1f, 0.25f, 0.8f);
		_outline = _fill;
		_lineWidth = resource?.DebugLineWidth ?? 2f;
		_shape = null;
		ResetPreview();
	}

	public override void _Draw()
	{
		DrawBackground();
		Vector2 vector = base.Size * 0.5f;
		Transform2D transform = _transform;
		transform.Origin = _previewOrigin;
		Color fill = (_enabled ? _fill : new Color(0.55f, 0.24f, 0.2f, 0.16f));
		Color color = (_enabled ? _outline : new Color(0.95f, 0.36f, 0.28f, 0.85f));
		if (_kind == GeometryKind.Ray)
		{
			Vector2 vector2 = vector + transform.Origin * 1f;
			Vector2 vector3 = vector + transform * _previewGeometry * 1f;
			DrawLine(vector2, vector3, color, Mathf.Max(1f, _lineWidth), antialiased: true);
			Vector2 vector4 = (vector3 - vector2).Normalized();
			Vector2 vector5 = new Vector2(0f - vector4.Y, vector4.X);
			DrawColoredPolygon(new Vector2[3]
			{
				vector3,
				vector3 - vector4 * 16f + vector5 * 7f,
				vector3 - vector4 * 16f - vector5 * 7f
			}, color);
			DrawHandle(vector2, new Color(1f, 0.68f, 0.18f));
			DrawHandle(vector3, new Color(0.2f, 0.92f, 1f));
		}
		else
		{
			Vector2 position = vector + transform.Origin * 1f;
			DrawSetTransform(position, transform.Rotation, transform.Scale * 1f);
			DrawGeometry(_previewGeometry, fill, color);
			DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
			DrawHandle(position, new Color(1f, 0.68f, 0.18f));
			Vector2 position2 = vector + transform * (_previewGeometry * 0.5f) * 1f;
			DrawHandle(position2, new Color(0.2f, 0.92f, 1f));
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			if (inputEventMouseButton.Pressed)
			{
				Vector2 originHandle = GetOriginHandle();
				Vector2 geometryHandle = GetGeometryHandle();
				_dragOrigin = inputEventMouseButton.Position.DistanceTo(originHandle) <= 13f;
				_dragGeometry = !_dragOrigin && inputEventMouseButton.Position.DistanceTo(geometryHandle) <= 16f;
				if (_dragOrigin || _dragGeometry)
				{
					AcceptEvent();
				}
			}
			else if (_dragOrigin || _dragGeometry)
			{
				if (_dragOrigin)
				{
					OriginCommitted?.Invoke(_previewOrigin);
				}
				else
				{
					GeometryCommitted?.Invoke(_previewGeometry);
				}
				_dragOrigin = false;
				_dragGeometry = false;
				AcceptEvent();
			}
		}
		else if (@event is InputEventMouseMotion inputEventMouseMotion && (_dragOrigin || _dragGeometry))
		{
			Vector2 vector = (inputEventMouseMotion.Position - base.Size * 0.5f) / 1f;
			if (_dragOrigin)
			{
				_previewOrigin = vector;
			}
			else
			{
				Transform2D transform = _transform;
				transform.Origin = _previewOrigin;
				Vector2 vector2 = transform.AffineInverse() * vector;
				_previewGeometry = ((_kind == GeometryKind.Ray) ? vector2 : new Vector2(Mathf.Max(1f, Mathf.Abs(vector2.X) * 2f), Mathf.Max(1f, Mathf.Abs(vector2.Y) * 2f)));
			}
			QueueRedraw();
			AcceptEvent();
		}
	}

	private void DrawBackground()
	{
		DrawRect(new Rect2(Vector2.Zero, base.Size), new Color(0.025f, 0.035f, 0.028f));
		Vector2 vector = base.Size * 0.5f;
		for (float num = vector.X % 32f; num < base.Size.X; num += 32f)
		{
			DrawLine(new Vector2(num, 0f), new Vector2(num, base.Size.Y), new Color(0.18f, 0.24f, 0.18f, 0.55f));
		}
		for (float num2 = vector.Y % 32f; num2 < base.Size.Y; num2 += 32f)
		{
			DrawLine(new Vector2(0f, num2), new Vector2(base.Size.X, num2), new Color(0.18f, 0.24f, 0.18f, 0.55f));
		}
		DrawLine(new Vector2(0f, vector.Y), new Vector2(base.Size.X, vector.Y), new Color(0.62f, 0.32f, 0.24f, 0.7f), 2f);
		DrawLine(new Vector2(vector.X, 0f), new Vector2(vector.X, base.Size.Y), new Color(0.3f, 0.62f, 0.3f, 0.7f), 2f);
	}

	private void DrawGeometry(Vector2 size, Color fill, Color outline)
	{
		if (_kind == GeometryKind.HitBox || _shape is RectangleShape2D || _shape == null)
		{
			Rect2 rect = new Rect2(-size * 0.5f, size);
			DrawRect(rect, fill);
			DrawRect(rect, outline, filled: false, Mathf.Max(1f, _lineWidth));
		}
		else if (_shape is CircleShape2D)
		{
			float radius = Mathf.Max(0.5f, size.X * 0.5f);
			DrawCircle(Vector2.Zero, radius, fill);
			DrawArc(Vector2.Zero, radius, 0f, (float)Math.PI * 2f, 48, outline, Mathf.Max(1f, _lineWidth), antialiased: true);
		}
		else if (_shape is CapsuleShape2D)
		{
			float num = Mathf.Max(0.5f, size.X * 0.5f);
			float num2 = Mathf.Max(0f, size.Y - num * 2f);
			DrawRect(new Rect2(0f - num, (0f - num2) * 0.5f, num * 2f, num2), fill);
			DrawCircle(new Vector2(0f, (0f - num2) * 0.5f), num, fill);
			DrawCircle(new Vector2(0f, num2 * 0.5f), num, fill);
			DrawArc(new Vector2(0f, (0f - num2) * 0.5f), num, (float)Math.PI, (float)Math.PI * 2f, 24, outline, _lineWidth, antialiased: true);
			DrawArc(new Vector2(0f, num2 * 0.5f), num, 0f, (float)Math.PI, 24, outline, _lineWidth, antialiased: true);
			DrawLine(new Vector2(0f - num, (0f - num2) * 0.5f), new Vector2(0f - num, num2 * 0.5f), outline, _lineWidth);
			DrawLine(new Vector2(num, (0f - num2) * 0.5f), new Vector2(num, num2 * 0.5f), outline, _lineWidth);
		}
		else
		{
			Rect2 rect2 = new Rect2(-size * 0.5f, size);
			DrawRect(rect2, fill);
			DrawRect(rect2, outline, filled: false, Mathf.Max(1f, _lineWidth));
		}
	}

	private static Vector2 GetShapeSize(Shape2D shape)
	{
		if (!(shape is RectangleShape2D { Size: var size }))
		{
			if (!(shape is CircleShape2D circleShape2D))
			{
				if (!(shape is CapsuleShape2D capsuleShape2D))
				{
					if (shape is SegmentShape2D segmentShape2D)
					{
						return (segmentShape2D.B - segmentShape2D.A).Abs();
					}
					return new Vector2(80f, 80f);
				}
				return new Vector2(capsuleShape2D.Radius * 2f, capsuleShape2D.Height);
			}
			return Vector2.One * circleShape2D.Radius * 2f;
		}
		return size;
	}

	private void DrawHandle(Vector2 position, Color color)
	{
		DrawCircle(position, 10f, new Color(0f, 0f, 0f, 0.7f));
		DrawCircle(position, 8f, color);
	}

	private Vector2 GetOriginHandle()
	{
		return base.Size * 0.5f + _previewOrigin * 1f;
	}

	private Vector2 GetGeometryHandle()
	{
		Transform2D transform = _transform;
		transform.Origin = _previewOrigin;
		return base.Size * 0.5f + transform * ((_kind == GeometryKind.Ray) ? _previewGeometry : (_previewGeometry * 0.5f)) * 1f;
	}

	private void ResetPreview()
	{
		_previewOrigin = _transform.Origin;
		_previewGeometry = ((_kind == GeometryKind.Ray) ? _rayTarget : _geometrySize);
		QueueRedraw();
	}
}
