using System;
using Godot;

[Tool]
[GlobalClass]
public partial class CharacterHitBoxDefinition : Resource
{
	[Export(PropertyHint.None, "")]
	public Vector2 Size { get; set; } = AabbShapeUtil.DefaultAreaSize;

	[Export(PropertyHint.None, "")]
	public Transform2D LocalTransform { get; set; } = Transform2D.Identity;

	[Export(PropertyHint.None, "")]
	public bool DefaultEnabled { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool DefaultMonitorable { get; set; } = true;

	[ExportGroup("Collision Visualization", "")]
	[Export(PropertyHint.None, "")]
	public bool DebugDraw { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public Color DebugFillColor { get; set; } = new Color(0.1f, 0.75f, 1f, 0.18f);

	[Export(PropertyHint.None, "")]
	public Color DebugOutlineColor { get; set; } = new Color(0.1f, 0.75f, 1f, 0.85f);

	[Export(PropertyHint.Range, "0.5,12,0.5,or_greater")]
	public float DebugLineWidth { get; set; } = 2f;

	public bool HasValidGeometry
	{
		get
		{
			if (float.IsFinite(Size.X) && float.IsFinite(Size.Y) && Size.X > 0f)
			{
				return Size.Y > 0f;
			}
			return false;
		}
	}

	public bool TryGetWorldRect(Transform2D ownerTransform, out Rect2 rect)
	{
		if (!HasValidGeometry)
		{
			rect = default(Rect2);
			return false;
		}
		rect = AabbShapeUtil.ComputeRectangleWorldRect(ownerTransform * LocalTransform, Size);
		return true;
	}

	public int GetGeometryHash()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(Size);
		hashCode.Add(LocalTransform);
		return hashCode.ToHashCode();
	}
}
