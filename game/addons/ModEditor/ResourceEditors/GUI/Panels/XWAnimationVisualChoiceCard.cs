using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAnimationVisualChoiceCard : Button
{
	public int ChoiceId { get; private set; } = -1;

	public event Action<int> ChoicePressed;

	public override void _Ready()
	{
		base.Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		base.Pressed -= OnPressed;
		base._ExitTree();
	}

	public void Configure(int choiceId, string title, string subtitle, Texture2D icon, Color accent)
	{
		ChoiceId = choiceId;
		base.Text = (string.IsNullOrWhiteSpace(subtitle) ? title : (title + "\n" + subtitle));
		base.TooltipText = base.Text.Replace('\n', ' ');
		base.Icon = icon;
		AddThemeConstantOverride("icon_max_width", 58);
		AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 0.88f));
		AddThemeColorOverride("font_hover_color", Colors.White);
		AddThemeColorOverride("font_pressed_color", Colors.White);
		AddThemeStyleboxOverride("normal", CreateStyle(accent, 0.13f, 0.42f));
		AddThemeStyleboxOverride("hover", CreateStyle(accent, 0.22f, 0.72f));
		AddThemeStyleboxOverride("pressed", CreateStyle(accent, 0.34f, 0.94f));
		AddThemeStyleboxOverride("focus", CreateStyle(accent, 0.28f, 1f));
	}

	public void SetSelected(bool selected)
	{
		base.ButtonPressed = selected;
		base.Modulate = (selected ? Colors.White : new Color(0.88f, 0.92f, 0.86f, 0.92f));
	}

	private void OnPressed()
	{
		this.ChoicePressed?.Invoke(ChoiceId);
	}

	private static StyleBoxFlat CreateStyle(Color accent, float fillAlpha, float borderAlpha)
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(accent.R * 0.42f, accent.G * 0.42f, accent.B * 0.42f, fillAlpha),
			BorderColor = new Color(accent.R, accent.G, accent.B, borderAlpha),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 7,
			CornerRadiusTopRight = 7,
			CornerRadiusBottomLeft = 7,
			CornerRadiusBottomRight = 7,
			ContentMarginLeft = 8f,
			ContentMarginTop = 6f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 6f
		};
	}
}
