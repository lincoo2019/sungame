using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWGameVisualChoiceCard : Button
{
	public string ChoiceKey { get; private set; } = string.Empty;

	public void Configure(string choiceKey, string title, string detail, Texture2D icon, bool selected)
	{
		ChoiceKey = choiceKey ?? string.Empty;
		base.Text = (string.IsNullOrWhiteSpace(detail) ? title : (title + "\n" + detail));
		base.Icon = icon;
		base.TooltipText = base.Text;
		SetPressedNoSignal(selected);
	}

	public void SetSelected(bool selected)
	{
		SetPressedNoSignal(selected);
	}
}
