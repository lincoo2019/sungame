using Godot;

public static class ProjectileZoneHelper
{
	public static Rect2 ComputeAreaWorldRect(Node2D area)
	{
		return AabbShapeUtil.ComputeAreaWorldRect(area);
	}
}
