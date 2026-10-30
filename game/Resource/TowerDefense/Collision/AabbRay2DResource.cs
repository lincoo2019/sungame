using System;
using Godot;

[Tool]
[GlobalClass]
public partial class AabbRay2DResource : Resource
{
	[Export(PropertyHint.None, "")]
	public bool Enabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Transform2D LocalTransform { get; set; } = Transform2D.Identity;

	[Export(PropertyHint.None, "")]
	public Vector2 TargetPosition { get; set; } = new Vector2(2000f, 0f);

	[ExportGroup("Collision Visualization", "")]
	[Export(PropertyHint.None, "")]
	public bool DebugDraw { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color DebugColor { get; set; } = new Color(0.2f, 1f, 0.25f, 0.8f);

	[Export(PropertyHint.Range, "0.5,12,0.5,or_greater")]
	public float DebugLineWidth { get; set; } = 2f;

	[Export(PropertyHint.Range, "1,64,1,or_greater")]
	public float DebugEndpointRadius { get; set; } = 5f;

	[Export(PropertyHint.Range, "1,128,1,or_greater")]
	public float DebugArrowSize { get; set; } = 14f;

	public bool TryGetWorldSegment(Transform2D ownerTransform, out Vector2 origin, out Vector2 target)
	{
		return TryGetWorldSegment(ownerTransform, TargetPosition, out origin, out target);
	}

	public bool TryGetWorldSegment(Transform2D ownerTransform, Vector2 runtimeTargetPosition, out Vector2 origin, out Vector2 target)
	{
		return TryGetWorldSegment(ownerTransform, LocalTransform, runtimeTargetPosition, out origin, out target);
	}

	public bool TryGetWorldSegment(Transform2D ownerTransform, Transform2D runtimeLocalTransform, Vector2 runtimeTargetPosition, out Vector2 origin, out Vector2 target)
	{
		Transform2D transform2D = ownerTransform * runtimeLocalTransform;
		origin = transform2D.Origin;
		target = transform2D * runtimeTargetPosition;
		if (Enabled && runtimeTargetPosition.IsFinite())
		{
			return (target - origin).LengthSquared() >= 1E-06f;
		}
		return false;
	}

	public int GetGeometryHash()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(Enabled);
		hashCode.Add(LocalTransform);
		hashCode.Add(TargetPosition);
		return hashCode.ToHashCode();
	}
}
