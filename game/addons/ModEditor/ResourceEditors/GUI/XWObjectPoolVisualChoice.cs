using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWObjectPoolVisualChoice
{
	public int Id { get; }

	public string Title { get; }

	public string Detail { get; }

	public Texture2D Icon { get; }

	public XWObjectPoolVisualChoice(int id, string title, string detail, Texture2D icon)
	{
		Id = id;
		Title = title ?? string.Empty;
		Detail = detail ?? string.Empty;
		Icon = icon;
	}
}
