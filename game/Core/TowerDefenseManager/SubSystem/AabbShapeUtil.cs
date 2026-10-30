using System;
using Godot;

public static class AabbShapeUtil
{
	public static readonly Vector2 DefaultAreaSize = new Vector2(80f, 80f);

	public static Rect2 ComputeAreaWorldRect(Node2D area)
	{
		return ComputeAreaWorldRect(area, DefaultAreaSize);
	}

	public static Rect2 ComputeAreaWorldRect(Node2D area, Vector2 fallbackSize)
	{
		if (TryComputeAreaWorldRect(area, out var rect))
		{
			return rect;
		}
		if (!GodotObject.IsInstanceValid(area))
		{
			return new Rect2(Vector2.Zero, Vector2.Zero);
		}
		return RectFromCenter(area.GlobalPosition, fallbackSize);
	}

	public static bool TryComputeAreaWorldRect(Node2D area, out Rect2 rect)
	{
		rect = default(Rect2);
		if (!GodotObject.IsInstanceValid(area))
		{
			return false;
		}
		if (area is AabbArea2D { ShapeResources: not null } aabbArea2D && aabbArea2D.ShapeResources.Count > 0)
		{
			return aabbArea2D.TryComputeResourceWorldRect(out rect);
		}
		return TryComputeChildCollisionWorldRect(area, out rect);
	}

	private static Rect2 ComputeTransformedRectangleRect(Transform2D transform, Vector2 size)
	{
		Vector2 vector = size * 0.5f;
		return RectFromTransformedPoints(transform, new Vector2(0f - vector.X, 0f - vector.Y), new Vector2(vector.X, 0f - vector.Y), new Vector2(vector.X, vector.Y), new Vector2(0f - vector.X, vector.Y));
	}

	private static Rect2 ComputeTransformedCircleRect(Transform2D transform, float radius)
	{
		Vector2 center = TransformPoint(transform, Vector2.Zero);
		Vector2 vector = transform.X * radius;
		Vector2 vector2 = transform.Y * radius;
		float num = Mathf.Sqrt(vector.X * vector.X + vector2.X * vector2.X);
		float num2 = Mathf.Sqrt(vector.Y * vector.Y + vector2.Y * vector2.Y);
		return RectFromCenter(center, new Vector2(num * 2f, num2 * 2f));
	}

	private static bool TryComputeChildCollisionWorldRect(Node2D area, out Rect2 rect)
	{
		bool flag = false;
		Rect2 rect2 = default(Rect2);
		foreach (Node child in area.GetChildren())
		{
			if (child is CollisionShape2D collisionShape && TryComputeCollisionShapeWorldRect(collisionShape, out var rect3))
			{
				rect2 = (flag ? Union(rect2, rect3) : rect3);
				flag = true;
			}
		}
		rect = rect2;
		return flag;
	}

	public static bool TryComputeCollisionShapeWorldRect(CollisionShape2D collisionShape, out Rect2 rect)
	{
		rect = default(Rect2);
		if (!GodotObject.IsInstanceValid(collisionShape) || collisionShape.Disabled || collisionShape.Shape == null)
		{
			return false;
		}
		return TryComputeShapeWorldRect(collisionShape.Shape, collisionShape.GlobalTransform, out rect);
	}

	public static Rect2 RectFromCenter(Vector2 center, Vector2 size)
	{
		return new Rect2(center - size * 0.5f, size);
	}

	public static bool Intersects(Rect2 a, Rect2 b)
	{
		return a.Intersects(b, includeBorders: true);
	}

	public static Rect2 Union(Rect2 a, Rect2 b)
	{
		Vector2 vector = a.Position + a.Size;
		Vector2 vector2 = b.Position + b.Size;
		float num = Mathf.Min(a.Position.X, b.Position.X);
		float num2 = Mathf.Min(a.Position.Y, b.Position.Y);
		float num3 = Mathf.Max(vector.X, vector2.X);
		float num4 = Mathf.Max(vector.Y, vector2.Y);
		return new Rect2(new Vector2(num, num2), new Vector2(num3 - num, num4 - num2));
	}

	public static void AddShapeGeometryHash(ref HashCode hash, Shape2D shape)
	{
		if (!GodotObject.IsInstanceValid(shape))
		{
			hash.Add(0);
			return;
		}
		hash.Add(shape.GetInstanceId());
		if (!(shape is RectangleShape2D rectangleShape2D))
		{
			if (!(shape is CircleShape2D circleShape2D))
			{
				if (!(shape is SegmentShape2D segmentShape2D))
				{
					if (!(shape is CapsuleShape2D capsuleShape2D))
					{
						if (!(shape is SeparationRayShape2D separationRayShape2D))
						{
							if (!(shape is WorldBoundaryShape2D worldBoundaryShape2D))
							{
								if (!(shape is ConvexPolygonShape2D { Points: var points }))
								{
									if (shape is ConcavePolygonShape2D { Segments: var segments })
									{
										foreach (Vector2 value in segments)
										{
											hash.Add(value);
										}
									}
								}
								else
								{
									foreach (Vector2 value2 in points)
									{
										hash.Add(value2);
									}
								}
							}
							else
							{
								hash.Add(worldBoundaryShape2D.Normal);
								hash.Add(worldBoundaryShape2D.Distance);
							}
						}
						else
						{
							hash.Add(separationRayShape2D.Length);
							hash.Add(separationRayShape2D.SlideOnSlope);
						}
					}
					else
					{
						hash.Add(capsuleShape2D.Radius);
						hash.Add(capsuleShape2D.Height);
					}
				}
				else
				{
					hash.Add(segmentShape2D.A);
					hash.Add(segmentShape2D.B);
				}
			}
			else
			{
				hash.Add(circleShape2D.Radius);
			}
		}
		else
		{
			hash.Add(rectangleShape2D.Size);
		}
	}

	public static Rect2 ComputeProbeWorldRect(AabbProbe2D probe)
	{
		if (!GodotObject.IsInstanceValid(probe))
		{
			return new Rect2(Vector2.Zero, Vector2.Zero);
		}
		return probe.Shape switch
		{
			AabbProbeShape2D.Circle => ComputeTransformedCircleRect(probe.GlobalTransform, probe.Radius), 
			AabbProbeShape2D.Segment => RectFromTransformedPoints(probe.GlobalTransform, probe.SegmentA, probe.SegmentB), 
			_ => ComputeTransformedRectangleRect(probe.GlobalTransform, probe.Size), 
		};
	}

	public static Rect2 ComputeRectangleWorldRect(Transform2D transform, Vector2 size)
	{
		return ComputeTransformedRectangleRect(transform, size);
	}

	private static bool ClipSegmentAxis(float start, float direction, float min, float max, ref float tMin, ref float tMax)
	{
		if (Mathf.Abs(direction) < 1E-06f)
		{
			if (start >= min)
			{
				return start <= max;
			}
			return false;
		}
		float num = 1f / direction;
		float num2 = (min - start) * num;
		float num3 = (max - start) * num;
		if (num2 > num3)
		{
			float num4 = num3;
			num3 = num2;
			num2 = num4;
		}
		tMin = Mathf.Max(tMin, num2);
		tMax = Mathf.Min(tMax, num3);
		return tMin <= tMax;
	}

	public static bool SegmentIntersectsRect(Vector2 start, Vector2 end, Rect2 rect, out float enterT)
	{
		enterT = 0f;
		Vector2 vector = rect.Position + rect.Size;
		float min = Mathf.Min(rect.Position.X, vector.X);
		float min2 = Mathf.Min(rect.Position.Y, vector.Y);
		float max = Mathf.Max(rect.Position.X, vector.X);
		float max2 = Mathf.Max(rect.Position.Y, vector.Y);
		Vector2 vector2 = end - start;
		float tMin = 0f;
		float tMax = 1f;
		if (!ClipSegmentAxis(start.X, vector2.X, min, max, ref tMin, ref tMax))
		{
			return false;
		}
		if (!ClipSegmentAxis(start.Y, vector2.Y, min2, max2, ref tMin, ref tMax))
		{
			return false;
		}
		enterT = tMin;
		return true;
	}

	public static bool TryComputeShapeWorldRect(Shape2D shape, Transform2D transform, out Rect2 rect)
	{
		rect = default(Rect2);
		if (!GodotObject.IsInstanceValid(shape))
		{
			return false;
		}
		if (!(shape is RectangleShape2D rectangleShape2D))
		{
			if (!(shape is CircleShape2D circleShape2D))
			{
				if (!(shape is SegmentShape2D segmentShape2D))
				{
					if (!(shape is CapsuleShape2D capsuleShape2D))
					{
						if (!(shape is SeparationRayShape2D separationRayShape2D))
						{
							if (!(shape is ConvexPolygonShape2D convexPolygonShape2D))
							{
								if (shape is ConcavePolygonShape2D concavePolygonShape2D && concavePolygonShape2D.Segments.Length != 0)
								{
									rect = RectFromTransformedPoints(transform, concavePolygonShape2D.Segments);
									return true;
								}
							}
							else if (convexPolygonShape2D.Points.Length != 0)
							{
								rect = RectFromTransformedPoints(transform, convexPolygonShape2D.Points);
								return true;
							}
							return false;
						}
						rect = RectFromTransformedPoints(transform, Vector2.Zero, new Vector2(0f, separationRayShape2D.Length));
						return true;
					}
					rect = ComputeTransformedRectangleRect(transform, new Vector2(capsuleShape2D.Radius * 2f, capsuleShape2D.Height));
					return true;
				}
				rect = RectFromTransformedPoints(transform, segmentShape2D.A, segmentShape2D.B);
				return true;
			}
			rect = ComputeTransformedCircleRect(transform, circleShape2D.Radius);
			return true;
		}
		rect = ComputeTransformedRectangleRect(transform, rectangleShape2D.Size);
		return true;
	}

	public static bool TryComputeSegmentWorldRect(Transform2D transform, Vector2 start, Vector2 end, out Rect2 rect)
	{
		rect = RectFromTransformedPoints(transform, start, end);
		return true;
	}

	private static Rect2 RectFromTransformedPoints(Transform2D transform, Vector2[] points)
	{
		Vector2 vector = TransformPoint(transform, points[0]);
		float left = vector.X;
		float top = vector.Y;
		float right = vector.X;
		float bottom = vector.Y;
		for (int i = 1; i < points.Length; i++)
		{
			ExpandBounds(TransformPoint(transform, points[i]), ref left, ref top, ref right, ref bottom);
		}
		return RectFromMinMax(left, top, right, bottom);
	}

	private static Vector2 TransformPoint(Transform2D transform, Vector2 point)
	{
		return transform.Origin + transform.X * point.X + transform.Y * point.Y;
	}

	private static void ExpandBounds(Vector2 point, ref float left, ref float top, ref float right, ref float bottom)
	{
		left = Mathf.Min(left, point.X);
		top = Mathf.Min(top, point.Y);
		right = Mathf.Max(right, point.X);
		bottom = Mathf.Max(bottom, point.Y);
	}

	private static Rect2 RectFromMinMax(float left, float top, float right, float bottom)
	{
		return new Rect2(new Vector2(left, top), new Vector2(right - left, bottom - top));
	}

	private static Rect2 RectFromTransformedPoints(Transform2D transform, Vector2 p0, Vector2 p1)
	{
		Vector2 vector = TransformPoint(transform, p0);
		Vector2 vector2 = TransformPoint(transform, p1);
		return RectFromMinMax(Mathf.Min(vector.X, vector2.X), Mathf.Min(vector.Y, vector2.Y), Mathf.Max(vector.X, vector2.X), Mathf.Max(vector.Y, vector2.Y));
	}

	private static Rect2 RectFromTransformedPoints(Transform2D transform, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		Vector2 vector = TransformPoint(transform, p0);
		float left = vector.X;
		float top = vector.Y;
		float right = vector.X;
		float bottom = vector.Y;
		ExpandBounds(TransformPoint(transform, p1), ref left, ref top, ref right, ref bottom);
		ExpandBounds(TransformPoint(transform, p2), ref left, ref top, ref right, ref bottom);
		ExpandBounds(TransformPoint(transform, p3), ref left, ref top, ref right, ref bottom);
		return RectFromMinMax(left, top, right, bottom);
	}
}
