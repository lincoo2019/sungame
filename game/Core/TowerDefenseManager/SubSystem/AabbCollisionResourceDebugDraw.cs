using System;
using Godot;

public static class AabbCollisionResourceDebugDraw
{
	private static void DrawCapsule(Node2D canvas, AabbShape2DResource resource, Transform2D local, CapsuleShape2D capsule, float width)
	{
		float halfBody = Mathf.Max(0f, capsule.Height * 0.5f - capsule.Radius);
		Vector2[] points = new Vector2[50];
		FillCapsuleTopPoints(points, local, capsule, halfBody, 24);
		FillCapsuleBottomPoints(points, local, capsule, halfBody, 24);
		DrawFilledAndClosed(canvas, points, resource, width);
	}

	private static void FillCapsuleTopPoints(Vector2[] points, Transform2D local, CapsuleShape2D capsule, float halfBody, int halfSteps)
	{
		for (int i = 0; i <= halfSteps; i++)
		{
			float angle = (float)Math.PI + (float)Math.PI * (float)i / (float)halfSteps;
			points[i] = local * (new Vector2(0f, 0f - halfBody) + Vector2.FromAngle(angle) * capsule.Radius);
		}
	}

	private static void FillCapsuleBottomPoints(Vector2[] points, Transform2D local, CapsuleShape2D capsule, float halfBody, int halfSteps)
	{
		for (int i = 0; i <= halfSteps; i++)
		{
			float angle = (float)Math.PI * (float)i / (float)halfSteps;
			points[halfSteps + 1 + i] = local * (new Vector2(0f, halfBody) + Vector2.FromAngle(angle) * capsule.Radius);
		}
	}

	private static void DrawConcavePolygonSegments(Node2D canvas, AabbShape2DResource resource, Transform2D local, ConcavePolygonShape2D concave, float width)
	{
		for (int i = 0; i + 1 < concave.Segments.Length; i += 2)
		{
			canvas.DrawLine(local * concave.Segments[i], local * concave.Segments[i + 1], resource.DebugOutlineColor, width, antialiased: true);
		}
	}

	public static void DrawShape(Node2D canvas, AabbShape2DResource resource)
	{
		DrawEffectiveShape(canvas, resource, resource?.Enabled ?? false, resource?.LocalTransform ?? Transform2D.Identity, hasRectangleSizeOverride: false, default(Vector2), hasSegmentEndOverride: false, default(Vector2));
	}

	public static void DrawEffectiveShape(Node2D canvas, AabbShape2DResource resource, bool enabled, Transform2D localTransform, bool hasRectangleSizeOverride, Vector2 rectangleSizeOverride, bool hasSegmentEndOverride, Vector2 segmentEndOverride)
	{
		if (CanDrawShape(canvas, resource, enabled))
		{
			float width = Mathf.Max(0.5f, resource.DebugLineWidth);
			DrawGeometry(canvas, resource, localTransform, width, hasRectangleSizeOverride, rectangleSizeOverride, hasSegmentEndOverride, segmentEndOverride);
		}
	}

	private static bool CanDrawShape(Node2D canvas, AabbShape2DResource resource, bool enabled)
	{
		if (GodotObject.IsInstanceValid(canvas) && GodotObject.IsInstanceValid(resource) && enabled && resource.DebugDraw)
		{
			return GodotObject.IsInstanceValid(resource.Geometry);
		}
		return false;
	}

	private static void DrawEllipse(Node2D canvas, AabbShape2DResource resource, Transform2D local, float radiusX, float radiusY, float width)
	{
		Vector2[] array = new Vector2[48];
		for (int i = 0; i < 48; i++)
		{
			float s = (float)Math.PI * 2f * (float)i / 48f;
			array[i] = local * new Vector2(Mathf.Cos(s) * radiusX, Mathf.Sin(s) * radiusY);
		}
		DrawFilledAndClosed(canvas, array, resource, width);
	}

	private static void DrawFilledAndClosed(Node2D canvas, Vector2[] points, AabbShape2DResource resource, float width)
	{
		if (points.Length >= 2)
		{
			if (points.Length >= 3 && resource.DebugFillColor.A > 0f)
			{
				canvas.DrawColoredPolygon(points, resource.DebugFillColor);
			}
			Vector2[] array = new Vector2[points.Length + 1];
			Array.Copy(points, array, points.Length);
			array[^1] = points[0];
			canvas.DrawPolyline(array, resource.DebugOutlineColor, width, antialiased: true);
		}
	}

	private static void DrawGeometry(Node2D canvas, AabbShape2DResource resource, Transform2D local, float width, bool hasRectangleSizeOverride, Vector2 rectangleSizeOverride, bool hasSegmentEndOverride, Vector2 segmentEndOverride)
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
						if (!(geometry is SeparationRayShape2D ray))
						{
							if (!(geometry is ConvexPolygonShape2D convexPolygonShape2D))
							{
								if (geometry is ConcavePolygonShape2D concave)
								{
									DrawConcavePolygonSegments(canvas, resource, local, concave, width);
								}
							}
							else if (convexPolygonShape2D.Points.Length > 1)
							{
								DrawPolygon(canvas, resource, local, convexPolygonShape2D.Points, width);
							}
						}
						else
						{
							DrawSeparationRayShape(canvas, resource, local, ray, width);
						}
					}
					else
					{
						DrawCapsule(canvas, resource, local, capsule, width);
					}
				}
				else
				{
					DrawSegmentShape(canvas, resource, local, segment, width, hasSegmentEndOverride, segmentEndOverride);
				}
			}
			else
			{
				DrawEllipse(canvas, resource, local, circleShape2D.Radius, circleShape2D.Radius, width);
			}
		}
		else
		{
			DrawRectangleShape(canvas, resource, local, rectangle, width, hasRectangleSizeOverride, rectangleSizeOverride);
		}
	}

	private static void DrawPolygon(Node2D canvas, AabbShape2DResource resource, Transform2D local, Vector2[] source, float width)
	{
		Vector2[] array = new Vector2[source.Length];
		for (int i = 0; i < source.Length; i++)
		{
			array[i] = local * source[i];
		}
		DrawFilledAndClosed(canvas, array, resource, width);
	}

	public static void DrawRay(Node2D canvas, AabbRay2DResource resource)
	{
		if (CanDrawRay(canvas, resource))
		{
			Vector2 origin = resource.LocalTransform.Origin;
			Vector2 vector = resource.LocalTransform * resource.TargetPosition;
			Vector2 delta = vector - origin;
			if (!(delta.LengthSquared() < 1E-06f))
			{
				DrawRayLineAndArrow(canvas, resource, origin, vector, delta);
			}
		}
	}

	private static bool CanDrawRay(Node2D canvas, AabbRay2DResource resource)
	{
		if (GodotObject.IsInstanceValid(canvas) && GodotObject.IsInstanceValid(resource) && resource.Enabled && resource.DebugDraw)
		{
			return resource.TargetPosition.IsFinite();
		}
		return false;
	}

	private static void DrawRayLineAndArrow(Node2D canvas, AabbRay2DResource resource, Vector2 origin, Vector2 target, Vector2 delta)
	{
		float width = Mathf.Max(0.5f, resource.DebugLineWidth);
		canvas.DrawLine(origin, target, resource.DebugColor, width, antialiased: true);
		canvas.DrawCircle(target, Mathf.Max(1f, resource.DebugEndpointRadius), resource.DebugColor);
		Vector2 vector = -delta.Normalized() * Mathf.Max(1f, resource.DebugArrowSize);
		Vector2 vector2 = vector.Orthogonal() * 0.5f;
		canvas.DrawLine(target, target + vector + vector2, resource.DebugColor, width, antialiased: true);
		canvas.DrawLine(target, target + vector - vector2, resource.DebugColor, width, antialiased: true);
	}

	private static void DrawRectangleShape(Node2D canvas, AabbShape2DResource resource, Transform2D local, RectangleShape2D rectangle, float width, bool hasRectangleSizeOverride, Vector2 rectangleSizeOverride)
	{
		Vector2 vector = (hasRectangleSizeOverride ? rectangleSizeOverride : rectangle.Size) * 0.5f;
		DrawPolygon(canvas, resource, local, new Vector2[4]
		{
			new Vector2(0f - vector.X, 0f - vector.Y),
			new Vector2(vector.X, 0f - vector.Y),
			new Vector2(vector.X, vector.Y),
			new Vector2(0f - vector.X, vector.Y)
		}, width);
	}

	private static void DrawSegmentShape(Node2D canvas, AabbShape2DResource resource, Transform2D local, SegmentShape2D segment, float width, bool hasSegmentEndOverride, Vector2 segmentEndOverride)
	{
		canvas.DrawLine(local * segment.A, local * (hasSegmentEndOverride ? segmentEndOverride : segment.B), resource.DebugOutlineColor, width, antialiased: true);
	}

	private static void DrawSeparationRayShape(Node2D canvas, AabbShape2DResource resource, Transform2D local, SeparationRayShape2D ray, float width)
	{
		canvas.DrawLine(local.Origin, local * new Vector2(0f, ray.Length), resource.DebugOutlineColor, width, antialiased: true);
	}
}
