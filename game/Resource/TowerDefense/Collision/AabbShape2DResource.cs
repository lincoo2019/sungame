using System;
using Godot;

[Tool]
[GlobalClass]
public partial class AabbShape2DResource : Resource
{
	[Export(PropertyHint.None, "")]
	public bool Enabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Shape2D Geometry { get; set; }

	[Export(PropertyHint.None, "")]
	public Transform2D LocalTransform { get; set; } = Transform2D.Identity;

	[ExportGroup("Collision Visualization", "")]
	[Export(PropertyHint.None, "")]
	public bool DebugDraw { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color DebugFillColor { get; set; } = new Color(0.1f, 0.75f, 1f, 0.18f);

	[Export(PropertyHint.None, "")]
	public Color DebugOutlineColor { get; set; } = new Color(0.1f, 0.75f, 1f, 0.85f);

	[Export(PropertyHint.Range, "0.5,12,0.5,or_greater")]
	public float DebugLineWidth { get; set; } = 2f;

	public bool TryGetWorldRect(Transform2D ownerTransform, out Rect2 rect)
	{
		if (!Enabled || !GodotObject.IsInstanceValid(Geometry))
		{
			rect = default(Rect2);
			return false;
		}
		return AabbShapeUtil.TryComputeShapeWorldRect(Geometry, ownerTransform * LocalTransform, out rect);
	}

	public int GetGeometryHash()
	{
		HashCode hash = default(HashCode);
		hash.Add(Enabled);
		hash.Add(LocalTransform);
		AabbShapeUtil.AddShapeGeometryHash(ref hash, Geometry);
		return hash.ToHashCode();
	}
}
