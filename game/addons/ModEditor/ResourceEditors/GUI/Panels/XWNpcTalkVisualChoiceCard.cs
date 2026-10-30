using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWNpcTalkVisualChoiceCard : Button
{
	public int ChoiceIndex { get; private set; } = -1;

	public void Configure(int choiceIndex, string title, string detail, Texture2D icon, bool selected)
	{
		ChoiceIndex = choiceIndex;
		base.Text = (string.IsNullOrWhiteSpace(detail) ? title : (title + "\n" + detail));
		base.Icon = icon;
		SetPressedNoSignal(selected);
		base.TooltipText = base.Text;
	}

	public void SetSelected(bool selected)
	{
		SetPressedNoSignal(selected);
	}
}
