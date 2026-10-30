using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AabbArea2D : Node2D, IAabbCollisionPreview2D, IAabbAreaQuery2D
{
	private bool _collisionPreviewDraw;

	private System.Collections.Generic.Dictionary<int, Vector2> _runtimeRectangleSizes;

	private System.Collections.Generic.Dictionary<int, Vector2> _runtimeSegmentEnds;

	private System.Collections.Generic.Dictionary<int, Transform2D> _runtimeShapeLocalTransforms;

	[Export(PropertyHint.None, "")]
	public Array<AabbShape2DResource> ShapeResources { get; set; } = new Array<AabbShape2DResource>();

	[Export(PropertyHint.None, "")]
	public bool Enabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public uint collision_layer { get; set; } = 1u;

	[Export(PropertyHint.None, "")]
	public uint collision_mask { get; set; } = 1u;

	[Export(PropertyHint.None, "")]
	public bool monitoring { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool monitorable { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool input_pickable { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public float priority { get; set; }

	[ExportGroup("Editor Debug", "")]
	[Export(PropertyHint.None, "")]
	public bool editorDraw { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color editorShapeColor { get; set; } = new Color(0.1f, 0.75f, 1f, 0.75f);

	[Export(PropertyHint.None, "")]
	public Color editorAabbColor { get; set; } = new Color(1f, 0.7f, 0.1f, 0.65f);

	[Export(PropertyHint.None, "")]
	public float editorLineWidth { get; set; } = 2f;

	public uint CollisionLayer
	{
		get
		{
			return collision_layer;
		}
		set
		{
			collision_layer = value;
		}
	}

	public uint CollisionMask
	{
		get
		{
			return collision_mask;
		}
		set
		{
			collision_mask = value;
		}
	}

	public bool Monitoring
	{
		get
		{
			return monitoring;
		}
		set
		{
			monitoring = value;
		}
	}

	public bool Monitorable
	{
		get
		{
			return monitorable;
		}
		set
		{
			monitorable = value;
		}
	}

	public bool InputPickable
	{
		get
		{
			return input_pickable;
		}
		set
		{
			input_pickable = value;
		}
	}

	public float Priority
	{
		get
		{
			return priority;
		}
		set
		{
			priority = value;
		}
	}

	public Node RegistryOwner => this;

	public Rect2 WorldRect => AabbShapeUtil.ComputeAreaWorldRect(this);

	private void DrawAreaShape(CollisionShape2D collisionShape)
	{
		if (!GodotObject.IsInstanceValid(collisionShape) || collisionShape.Disabled || collisionShape.Shape == null)
		{
			return;
		}
		Shape2D shape = collisionShape.Shape;
		if (!(shape is RectangleShape2D rectangle))
		{
			if (!(shape is CircleShape2D circle))
			{
				if (shape is SegmentShape2D segment)
				{
					DrawSegmentShape(collisionShape, segment);
				}
			}
			else
			{
				DrawCircleShape(collisionShape, circle);
			}
		}
		else
		{
			DrawRectangleShape(collisionShape, rectangle);
		}
	}

	private void DrawRectangleShape(CollisionShape2D collisionShape, RectangleShape2D rectangle)
	{
		Vector2 vector = rectangle.Size * 0.5f;
		DrawClosedPolyline(ShapePoint(collisionShape, new Vector2(0f - vector.X, 0f - vector.Y)), ShapePoint(collisionShape, new Vector2(vector.X, 0f - vector.Y)), ShapePoint(collisionShape, new Vector2(vector.X, vector.Y)), ShapePoint(collisionShape, new Vector2(0f - vector.X, vector.Y)), editorShapeColor);
	}

	private void DrawCircleShape(CollisionShape2D collisionShape, CircleShape2D circle)
	{
		Vector2[] array = new Vector2[33];
		for (int i = 0; i <= 32; i++)
		{
			float angle = (float)Math.PI * 2f * (float)i / 32f;
			array[i] = ShapePoint(collisionShape, Vector2.FromAngle(angle) * circle.Radius);
		}
		DrawPolyline(array, editorShapeColor, editorLineWidth);
	}

	private void DrawSegmentShape(CollisionShape2D collisionShape, SegmentShape2D segment)
	{
		DrawLine(ShapePoint(collisionShape, segment.A), ShapePoint(collisionShape, segment.B), editorShapeColor, editorLineWidth);
	}

	private Vector2 ShapePoint(CollisionShape2D collisionShape, Vector2 point)
	{
		return ToLocal(collisionShape.ToGlobal(point));
	}

	private void DrawClosedPolyline(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
	{
		DrawPolyline(new Vector2[5] { a, b, c, d, a }, color, editorLineWidth);
	}

	public void SetEnabled(bool enabled)
	{
		Enabled = enabled;
		QueueRedraw();
	}

	public bool TryGetWorldRect(out Rect2 rect)
	{
		if (!Enabled)
		{
			rect = default(Rect2);
			return false;
		}
		return AabbShapeUtil.TryComputeAreaWorldRect(this, out rect);
	}

	public override void _Ready()
	{
		SetProcess(Engine.IsEditorHint() || _collisionPreviewDraw);
	}

	public override void _EnterTree()
	{
		AabbAreaLayerRegistry.Register(this);
	}

	public override void _ExitTree()
	{
		AabbAreaLayerRegistry.Unregister(this);
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
		if ((!Engine.IsEditorHint() && !_collisionPreviewDraw) || !editorDraw)
		{
			return;
		}
		if (ShapeResources != null && ShapeResources.Count > 0)
		{
			for (int i = 0; i < ShapeResources.Count; i++)
			{
				DrawResourceShape(i, ShapeResources[i]);
			}
		}
		else
		{
			foreach (Node child in GetChildren())
			{
				if (child is CollisionShape2D collisionShape)
				{
					DrawAreaShape(collisionShape);
				}
			}
		}
		DrawWorldRect();
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

	private void DrawResourceRectangle(int shapeIndex, AabbShape2DResource resource, Transform2D localTransform, RectangleShape2D rectangle, float width)
	{
		Vector2 size;
		Vector2 vector = (TryGetRuntimeRectangleSize(shapeIndex, out size) ? size : rectangle.Size) * 0.5f;
		DrawResourcePolygon(resource, localTransform, new Vector2[4]
		{
			new Vector2(0f - vector.X, 0f - vector.Y),
			new Vector2(vector.X, 0f - vector.Y),
			new Vector2(vector.X, vector.Y),
			new Vector2(0f - vector.X, vector.Y)
		}, width);
	}

	private void DrawResourceSegment(int shapeIndex, AabbShape2DResource resource, Transform2D localTransform, SegmentShape2D segment, float width)
	{
		Vector2 endpoint;
		Vector2 vector = (TryGetRuntimeSegmentEnd(shapeIndex, out endpoint) ? endpoint : segment.B);
		DrawLine(localTransform * segment.A, localTransform * vector, resource.DebugOutlineColor, width);
	}

	private void DrawResourceConcave(AabbShape2DResource resource, Transform2D localTransform, ConcavePolygonShape2D concave, float width)
	{
		for (int i = 0; i + 1 < concave.Segments.Length; i += 2)
		{
			DrawLine(localTransform * concave.Segments[i], localTransform * concave.Segments[i + 1], resource.DebugOutlineColor, width);
		}
	}

	private void DrawResourceCapsule(AabbShape2DResource resource, Transform2D localTransform, CapsuleShape2D capsule, float width)
	{
		float num = Mathf.Max(0f, capsule.Height * 0.5f - capsule.Radius);
		Vector2[] array = new Vector2[50];
		for (int i = 0; i <= 24; i++)
		{
			float angle = (float)Math.PI + (float)Math.PI * (float)i / 24f;
			array[i] = localTransform * (new Vector2(0f, 0f - num) + Vector2.FromAngle(angle) * capsule.Radius);
		}
		for (int j = 0; j <= 24; j++)
		{
			float angle2 = (float)Math.PI * (float)j / 24f;
			array[25 + j] = localTransform * (new Vector2(0f, num) + Vector2.FromAngle(angle2) * capsule.Radius);
		}
		DrawFilledAndClosed(array, resource.DebugFillColor, resource.DebugOutlineColor, width);
	}

	private void DrawResourceShape(int shapeIndex, AabbShape2DResource resource)
	{
		if (GodotObject.IsInstanceValid(resource) && resource.Enabled && resource.DebugDraw && GodotObject.IsInstanceValid(resource.Geometry))
		{
			float width = Mathf.Max(0.5f, resource.DebugLineWidth);
			if (TryGetRuntimeShapeLocalTransform(shapeIndex, out var localTransform))
			{
				DrawResourceGeometry(shapeIndex, resource, localTransform, width);
			}
		}
	}

	private void DrawResourceGeometry(int shapeIndex, AabbShape2DResource resource, Transform2D localTransform, float width)
	{
		Shape2D geometry = resource.Geometry;
		if (!(geometry is RectangleShape2D rectangle))
		{
			if (!(geometry is CircleShape2D circleShape2D))
			{
				if (!(geometry is SegmentShape2D segment))
				{
					if (!(geometry is CapsuleShape2D capsule))
					{
						if (!(geometry is SeparationRayShape2D separationRayShape2D))
						{
							if (!(geometry is ConvexPolygonShape2D convexPolygonShape2D))
							{
								if (geometry is ConcavePolygonShape2D concave)
								{
									DrawResourceConcave(resource, localTransform, concave, width);
								}
							}
							else if (convexPolygonShape2D.Points.Length > 1)
							{
								DrawResourcePolygon(resource, localTransform, convexPolygonShape2D.Points, width);
							}
						}
						else
						{
							DrawLine(localTransform.Origin, localTransform * new Vector2(0f, separationRayShape2D.Length), resource.DebugOutlineColor, width);
						}
					}
					else
					{
						DrawResourceCapsule(resource, localTransform, capsule, width);
					}
				}
				else
				{
					DrawResourceSegment(shapeIndex, resource, localTransform, segment, width);
				}
			}
			else
			{
				DrawResourceEllipse(resource, localTransform, circleShape2D.Radius, circleShape2D.Radius, width);
			}
		}
		else
		{
			DrawResourceRectangle(shapeIndex, resource, localTransform, rectangle, width);
		}
	}

	private void DrawResourceEllipse(AabbShape2DResource resource, Transform2D localTransform, float radiusX, float radiusY, float width)
	{
		Vector2[] array = new Vector2[48];
		for (int i = 0; i < 48; i++)
		{
			float s = (float)Math.PI * 2f * (float)i / 48f;
			array[i] = localTransform * new Vector2(Mathf.Cos(s) * radiusX, Mathf.Sin(s) * radiusY);
		}
		DrawFilledAndClosed(array, resource.DebugFillColor, resource.DebugOutlineColor, width);
	}

	public bool TryComputeResourceWorldRect(out Rect2 rect)
	{
		rect = default(Rect2);
		if (!Enabled || ShapeResources == null || ShapeResources.Count == 0)
		{
			return false;
		}
		bool hasRect = false;
		for (int i = 0; i < ShapeResources.Count; i++)
		{
			MergeResourceWorldRect(i, ref rect, ref hasRect);
		}
		return hasRect;
	}

	public int GetResourceGeometryHash()
	{
		if (ShapeResources == null || ShapeResources.Count == 0)
		{
			return 0;
		}
		HashCode hash = default(HashCode);
		for (int i = 0; i < ShapeResources.Count; i++)
		{
			AddResourceGeometryHash(i, ref hash);
		}
		return hash.ToHashCode();
	}

	private void AddResourceGeometryHash(int index, ref HashCode hash)
	{
		AabbShape2DResource aabbShape2DResource = ShapeResources[index];
		hash.Add(GodotObject.IsInstanceValid(aabbShape2DResource) ? aabbShape2DResource.GetGeometryHash() : 0);
		AddRuntimeSegmentHash(index, ref hash);
		AddRuntimeRectangleHash(index, ref hash);
		AddRuntimeTransformHash(index, ref hash);
	}

	private void AddRuntimeSegmentHash(int index, ref HashCode hash)
	{
		Vector2 endpoint;
		bool flag = TryGetRuntimeSegmentEnd(index, out endpoint);
		hash.Add(flag);
		if (flag)
		{
			hash.Add(endpoint);
		}
	}

	private void AddRuntimeRectangleHash(int index, ref HashCode hash)
	{
		Vector2 size;
		bool flag = TryGetRuntimeRectangleSize(index, out size);
		hash.Add(flag);
		if (flag)
		{
			hash.Add(size);
		}
	}

	private void AddRuntimeTransformHash(int index, ref HashCode hash)
	{
		Transform2D value = default(Transform2D);
		bool flag = _runtimeShapeLocalTransforms != null && _runtimeShapeLocalTransforms.TryGetValue(index, out value);
		hash.Add(flag);
		if (flag)
		{
			hash.Add(value);
		}
	}

	private void MergeResourceWorldRect(int index, ref Rect2 rect, ref bool hasRect)
	{
		AabbShape2DResource aabbShape2DResource = ShapeResources[index];
		if (GodotObject.IsInstanceValid(aabbShape2DResource) && TryComputeResourceShapeWorldRect(index, aabbShape2DResource, out var shapeRect))
		{
			rect = (hasRect ? AabbShapeUtil.Union(rect, shapeRect) : shapeRect);
			hasRect = true;
		}
	}

	private bool TryComputeResourceShapeWorldRect(int index, AabbShape2DResource resource, out Rect2 shapeRect)
	{
		shapeRect = default(Rect2);
		if (!resource.Enabled || !GodotObject.IsInstanceValid(resource.Geometry) || !TryGetRuntimeShapeLocalTransform(index, out var localTransform))
		{
			return false;
		}
		Transform2D worldTransform = base.GlobalTransform * localTransform;
		return TryComputeResourceGeometryWorldRect(index, resource, worldTransform, out shapeRect);
	}

	private bool TryComputeResourceGeometryWorldRect(int index, AabbShape2DResource resource, Transform2D worldTransform, out Rect2 shapeRect)
	{
		if (resource.Geometry is RectangleShape2D && TryGetRuntimeRectangleSize(index, out var size))
		{
			shapeRect = AabbShapeUtil.ComputeRectangleWorldRect(worldTransform, size);
			return true;
		}
		if (resource.Geometry is SegmentShape2D segmentShape2D && TryGetRuntimeSegmentEnd(index, out var endpoint))
		{
			return AabbShapeUtil.TryComputeSegmentWorldRect(worldTransform, segmentShape2D.A, endpoint, out shapeRect);
		}
		return AabbShapeUtil.TryComputeShapeWorldRect(resource.Geometry, worldTransform, out shapeRect);
	}

	private void DrawResourcePolygon(AabbShape2DResource resource, Transform2D localTransform, Vector2[] source, float width)
	{
		Vector2[] array = new Vector2[source.Length];
		for (int i = 0; i < source.Length; i++)
		{
			array[i] = localTransform * source[i];
		}
		DrawFilledAndClosed(array, resource.DebugFillColor, resource.DebugOutlineColor, width);
	}

	private void DrawFilledAndClosed(Vector2[] points, Color fill, Color outline, float width)
	{
		if (points.Length >= 2)
		{
			if (points.Length >= 3 && fill.A > 0f)
			{
				DrawColoredPolygon(points, fill);
			}
			Vector2[] array = new Vector2[points.Length + 1];
			System.Array.Copy(points, array, points.Length);
			array[^1] = points[0];
			DrawPolyline(array, outline, width, antialiased: true);
		}
	}

	public bool SetRuntimeRectangleSize(int shapeIndex, Vector2 size)
	{
		if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X < 0f || size.Y < 0f || !TryGetRectangleResource(shapeIndex, out var _))
		{
			return false;
		}
		if (_runtimeRectangleSizes == null)
		{
			_runtimeRectangleSizes = new System.Collections.Generic.Dictionary<int, Vector2>();
		}
		_runtimeRectangleSizes[shapeIndex] = size;
		QueueRedraw();
		return true;
	}

	public bool TryGetRuntimeRectangleSize(int shapeIndex, out Vector2 size)
	{
		if (_runtimeRectangleSizes != null && _runtimeRectangleSizes.TryGetValue(shapeIndex, out size))
		{
			return true;
		}
		size = default(Vector2);
		return false;
	}

	public bool ClearRuntimeRectangleSize(int shapeIndex)
	{
		if (_runtimeRectangleSizes == null || !_runtimeRectangleSizes.Remove(shapeIndex))
		{
			return false;
		}
		if (_runtimeRectangleSizes.Count == 0)
		{
			_runtimeRectangleSizes = null;
		}
		QueueRedraw();
		return true;
	}

	public bool SetRuntimeRectangleWidth(int shapeIndex, float width)
	{
		if (!float.IsFinite(width) || width < 0f || !TryGetEffectiveRectangleSize(shapeIndex, out var size))
		{
			return false;
		}
		size.X = width;
		return SetRuntimeRectangleSize(shapeIndex, size);
	}

	private bool TryGetEffectiveRectangleSize(int shapeIndex, out Vector2 size)
	{
		if (TryGetRuntimeRectangleSize(shapeIndex, out size))
		{
			return true;
		}
		if (TryGetRectangleResource(shapeIndex, out var rectangle))
		{
			size = rectangle.Size;
			return true;
		}
		size = default(Vector2);
		return false;
	}

	public bool SetRuntimeSegmentLengthX(int shapeIndex, float length)
	{
		if (!TryGetSegmentResource(shapeIndex, out var segment))
		{
			return false;
		}
		return SetRuntimeSegmentEnd(shapeIndex, new Vector2((float)Mathf.Sign(segment.B.X) * Mathf.Max(0f, length), segment.B.Y));
	}

	public bool SetRuntimeSegmentEnd(int shapeIndex, Vector2 endpoint)
	{
		if (!TryGetSegmentResource(shapeIndex, out var _))
		{
			return false;
		}
		if (_runtimeSegmentEnds == null)
		{
			_runtimeSegmentEnds = new System.Collections.Generic.Dictionary<int, Vector2>();
		}
		_runtimeSegmentEnds[shapeIndex] = endpoint;
		QueueRedraw();
		return true;
	}

	public bool TryGetRuntimeSegmentEnd(int shapeIndex, out Vector2 endpoint)
	{
		if (_runtimeSegmentEnds != null && _runtimeSegmentEnds.TryGetValue(shapeIndex, out endpoint))
		{
			return true;
		}
		endpoint = default(Vector2);
		return false;
	}

	public bool ClearRuntimeSegmentEnd(int shapeIndex)
	{
		if (_runtimeSegmentEnds == null || !_runtimeSegmentEnds.Remove(shapeIndex))
		{
			return false;
		}
		if (_runtimeSegmentEnds.Count == 0)
		{
			_runtimeSegmentEnds = null;
		}
		QueueRedraw();
		return true;
	}

	public bool SetRuntimeShapeLocalOrigin(int shapeIndex, Vector2 localOrigin)
	{
		if (!TryGetRuntimeShapeLocalTransform(shapeIndex, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = localOrigin;
		return SetRuntimeShapeLocalTransform(shapeIndex, localTransform);
	}

	public bool SetRuntimeShapeLocalOriginY(int shapeIndex, float localY)
	{
		if (!float.IsFinite(localY) || !TryGetRuntimeShapeLocalTransform(shapeIndex, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = new Vector2(localTransform.Origin.X, localY);
		return SetRuntimeShapeLocalTransform(shapeIndex, localTransform);
	}

	public bool SetRuntimeShapeLocalTransform(int shapeIndex, Transform2D localTransform)
	{
		if (!TryGetShapeResource(shapeIndex, out var _))
		{
			return false;
		}
		if (_runtimeShapeLocalTransforms == null)
		{
			_runtimeShapeLocalTransforms = new System.Collections.Generic.Dictionary<int, Transform2D>();
		}
		_runtimeShapeLocalTransforms[shapeIndex] = localTransform;
		QueueRedraw();
		return true;
	}

	public bool TryGetRuntimeShapeLocalTransform(int shapeIndex, out Transform2D localTransform)
	{
		if (!TryGetShapeResource(shapeIndex, out var resource))
		{
			localTransform = Transform2D.Identity;
			return false;
		}
		if (_runtimeShapeLocalTransforms != null && _runtimeShapeLocalTransforms.TryGetValue(shapeIndex, out localTransform))
		{
			return true;
		}
		localTransform = resource.LocalTransform;
		return true;
	}

	public bool ClearRuntimeShapeLocalTransform(int shapeIndex)
	{
		if (_runtimeShapeLocalTransforms == null || !_runtimeShapeLocalTransforms.Remove(shapeIndex))
		{
			return false;
		}
		if (_runtimeShapeLocalTransforms.Count == 0)
		{
			_runtimeShapeLocalTransforms = null;
		}
		QueueRedraw();
		return true;
	}

	private bool TryGetShapeResource(int shapeIndex, out AabbShape2DResource resource)
	{
		resource = null;
		if (ShapeResources == null || shapeIndex < 0 || shapeIndex >= ShapeResources.Count)
		{
			return false;
		}
		resource = ShapeResources[shapeIndex];
		if (GodotObject.IsInstanceValid(resource))
		{
			return GodotObject.IsInstanceValid(resource.Geometry);
		}
		return false;
	}

	private bool TryGetRectangleResource(int shapeIndex, out RectangleShape2D rectangle)
	{
		rectangle = null;
		if (!TryGetShapeResource(shapeIndex, out var resource) || !(resource.Geometry is RectangleShape2D rectangleShape2D))
		{
			return false;
		}
		rectangle = rectangleShape2D;
		return true;
	}

	private bool TryGetSegmentResource(int shapeIndex, out SegmentShape2D segment)
	{
		segment = null;
		if (!TryGetShapeResource(shapeIndex, out var resource) || !(resource.Geometry is SegmentShape2D segmentShape2D))
		{
			return false;
		}
		segment = segmentShape2D;
		return true;
	}

	private void DrawWorldRect()
	{
		if (!AabbShapeUtil.TryComputeAreaWorldRect(this, out var rect))
		{
			rect = WorldRect;
		}
		if (!(rect.Size == Vector2.Zero))
		{
			Vector2 position = rect.Position;
			Vector2 vector = rect.Position + rect.Size;
			DrawClosedPolyline(ToLocal(new Vector2(position.X, position.Y)), ToLocal(new Vector2(vector.X, position.Y)), ToLocal(new Vector2(vector.X, vector.Y)), ToLocal(new Vector2(position.X, vector.Y)), editorAabbColor);
		}
	}
}
