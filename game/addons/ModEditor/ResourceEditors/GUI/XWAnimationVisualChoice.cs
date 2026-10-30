using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public readonly struct XWAnimationVisualChoice(int id, string title, string subtitle, Texture2D icon, Color accent)
{
	public int Id { get; } = id;

	public string Title { get; } = title ?? "";

	public string Subtitle { get; } = subtitle ?? "";

	public Texture2D Icon { get; } = icon;

	public Color Accent { get; } = accent;
}
