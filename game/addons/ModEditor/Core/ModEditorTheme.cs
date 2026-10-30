using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Core;

public static class ModEditorTheme
{
	private const float DefaultContrast = 0.3f;

	private const float Contrast = 0.3f;

	private const int CornerRadius = 8;

	private const int BaseSpacing = 4;

	private const int MainFontSize = 14;

	private const int CodeFontSize = 14;

	private const int OutputFontSize = 13;

	private const int TitleFontSize = 15;

	private const int MainScreenFontSize = 16;

	public static readonly Color BaseColor = new Color(0.086f, 0.145f, 0.094f);

	public static readonly Color AccentColor = new Color(0.945f, 0.733f, 0.18f);

	public static readonly Color GrassColor = new Color(0.105f, 0.235f, 0.125f);

	public static readonly Color LeafColor = new Color(0.286f, 0.565f, 0.247f);

	public static readonly Color SunGoldColor = new Color(0.945f, 0.733f, 0.18f);

	public static readonly Color DirtColor = new Color(0.275f, 0.18f, 0.102f);

	private static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

	private static readonly Color MonoColor = Colors.White;

	private static readonly Color DarkColor1 = ClampColor(LerpColor(BaseColor, Colors.Black, 0.345f));

	private static readonly Color DarkColor2 = new Color(0f, 0f, 0f, 0.3f);

	private static readonly Color DarkColor3 = GetBaseColor(0.8f, 0.9f);

	private static readonly Color ContrastColor1 = ClampColor(LerpColor(BaseColor, MonoColor, Max(0.345f, 0.345f)));

	private static readonly Color ContrastColor2 = ClampColor(LerpColor(BaseColor, MonoColor, Max(0.51750004f, 0.51750004f)));

	private static readonly Color HighlightColor = WithAlpha(AccentColor, 0.275f);

	private static readonly Color SelectionColor = WithAlpha(AccentColor, 0.4f);

	private static readonly Color SeparatorColor = new Color(0f, 0f, 0f, 0.4f);

	private static readonly Color FontColor = WithAlpha(MonoColor, 0.75f);

	private static readonly Color FontSecondaryColor = WithAlpha(MonoColor, 0.55f);

	private static readonly Color FontFocusColor = MonoColor;

	private static readonly Color FontHoverColor = WithAlpha(MonoColor, 0.85f);

	private static readonly Color FontPressedColor = WithAlpha(MonoColor, 0.85f);

	private static readonly Color FontHoverPressedColor = MonoColor;

	private static readonly Color FontDisabledColor = WithAlpha(MonoColor, 0.35f);

	private static readonly Color FontReadonlyColor = WithAlpha(MonoColor, 0.65f);

	private static readonly Color SurfacePopupColor = GetBaseColor(1.9f, 0.9f);

	private static readonly Color SurfaceLowestColor = GetBaseColor(1.7f, 0.9f);

	private static readonly Color SurfaceLowerColor = GetBaseColor(1.1f, 0.9f);

	private static readonly Color SurfaceLowColor = GetBaseColor(0.8f);

	private static readonly Color SurfaceBaseColor = GetBaseColor();

	private static readonly Color SurfaceHighColor = GetBaseColor(-1.3f, 0.8f);

	private static readonly Color SurfaceHigherColor = GetBaseColor(-1.5f, 0.8f);

	private static readonly Color SurfaceHighestColor = GetBaseColor(-2.2f, 0.6f);

	private static readonly Color ButtonNormalColor = GetBaseColor(-2f, 0.85f);

	private static readonly Color ButtonHoverColor = GetBaseColor(-2.9f, 0.75f);

	private static readonly Color ButtonPressedColor = GetBaseColor(-3.2f, 0.75f);

	private static readonly Color ButtonDisabledColor = GetBaseColor(-1.4f, 0.75f);

	private static readonly Color ButtonBorderNormalColor = GetBaseColor(-2.5f, 0.75f);

	private static readonly Color ButtonBorderHoverColor = GetBaseColor(-3.4f, 0.75f);

	private static readonly Color ButtonBorderPressedColor = GetBaseColor(-3.7f, 0.75f);

	private static readonly Color FlatButtonHoverColor = GetBaseColor(-1.2f, 0.75f);

	private static readonly Color FlatButtonPressedColor = GetBaseColor(-2f, 0.75f);

	private static readonly Color FlatButtonHoverPressedColor = GetBaseColor(-2.4f, 0.75f);

	public static readonly Color BackgroundColor = SurfaceLowestColor;

	public static readonly Color PanelColor = SurfaceLowColor;

	public static readonly Color DarkPanelColor = SurfaceLowerColor;

	public static readonly Color BorderColor = WithAlpha(ContrastColor1, 0.22f);

	public static readonly Color HoverColor = FlatButtonHoverColor;

	public static readonly Color PressedColor = FlatButtonPressedColor;

	public static readonly Color TextColor = FontColor;

	public static readonly Color DimTextColor = FontSecondaryColor;

	public static readonly Color ErrorColor = new Color(1f, 0.47f, 0.42f);

	public static readonly Color SuccessColor = new Color(0.45f, 0.95f, 0.5f);

	public static readonly Color WarningColor = new Color(0.83f, 0.78f, 0.62f);

	private static Font _defaultFont;

	private static Font _boldFont;

	private static Font _codeFont;

	public static Font DefaultFont => _defaultFont ?? (_defaultFont = LoadEditorFont("Inter_Regular.woff2"));

	public static Font BoldFont => _boldFont ?? (_boldFont = LoadEditorFont("Inter_Bold.woff2"));

	public static Font CodeFont => _codeFont ?? (_codeFont = LoadEditorFont("JetBrainsMono_Regular.woff2"));

	private static Font LoadEditorFont(string fontFile)
	{
		string path = "res://addons/ModEditor/Fonts/" + fontFile;
		FontFile fontFile2 = (ResourceLoader.Exists(path) ? ResourceLoader.Load<FontFile>(path, null, ResourceLoader.CacheMode.Reuse) : null);
		if (fontFile2 == null)
		{
			return LoadLegacyFallbackFont();
		}
		Array<Font> fallbacks = new Array<Font>();
		AddFallbackFont(fallbacks, "DroidSansFallback.woff2");
		AddFallbackFont(fallbacks, "DroidSansJapanese.woff2");
		fontFile2.SetFallbacks(fallbacks);
		return fontFile2;
	}

	private static void AddFallbackFont(Array<Font> fallbacks, string fontFile)
	{
		string path = "res://addons/ModEditor/Fonts/" + fontFile;
		if (ResourceLoader.Exists(path))
		{
			FontFile fontFile2 = ResourceLoader.Load<FontFile>(path, null, ResourceLoader.CacheMode.Reuse);
			if (fontFile2 != null)
			{
				fallbacks.Add(fontFile2);
			}
		}
	}

	private static Font LoadLegacyFallbackFont()
	{
		string[] array = new string[2] { "res://Asset/Font/SIMHEI.ttf", "res://Asset/Font/SIMHEI.TTF" };
		foreach (string path in array)
		{
			if (ResourceLoader.Exists(path))
			{
				return ResourceLoader.Load<FontFile>(path, null, ResourceLoader.CacheMode.Reuse);
			}
		}
		return null;
	}

	public static Theme Create()
	{
		Theme theme = new Theme();
		ApplyEditorColors(theme);
		ApplyFonts(theme);
		ApplyControlColors(theme);
		ApplyContainers(theme);
		ApplyButtons(theme);
		ApplyHudTheme(theme);
		ApplyTextInputs(theme);
		ApplyLists(theme);
		ApplyTabs(theme);
		ApplyMenus(theme);
		ApplyScrollbars(theme);
		ApplyGraphTheme(theme);
		ApplyMisc(theme);
		ApplyIcons(theme);
		return theme;
	}

	private static void ApplyEditorColors(Theme theme)
	{
		theme.SetColor("accent_color", "Editor", AccentColor);
		theme.SetColor("highlight_color", "Editor", HighlightColor);
		theme.SetColor("base_color", "Editor", BaseColor);
		theme.SetColor("dark_color_1", "Editor", DarkColor1);
		theme.SetColor("dark_color_2", "Editor", DarkColor2);
		theme.SetColor("dark_color_3", "Editor", DarkColor3);
		theme.SetColor("contrast_color_1", "Editor", ContrastColor1);
		theme.SetColor("contrast_color_2", "Editor", ContrastColor2);
		theme.SetColor("box_selection_fill_color", "Editor", WithAlpha(AccentColor, 0.22f));
		theme.SetColor("box_selection_stroke_color", "Editor", WithAlpha(AccentColor, 0.7f));
		theme.SetColor("success_color", "Editor", SuccessColor);
		theme.SetColor("warning_color", "Editor", WarningColor);
		theme.SetColor("error_color", "Editor", ErrorColor);
	}

	private static void ApplyFonts(Theme theme)
	{
		if (DefaultFont != null)
		{
			theme.SetDefaultFont(DefaultFont);
			theme.SetDefaultFontSize(14);
			theme.SetFont("main", "EditorFonts", DefaultFont);
			theme.SetFont("main_msdf", "EditorFonts", DefaultFont);
			theme.SetFontSize("main_size", "EditorFonts", 14);
			theme.SetFont("bold", "EditorFonts", BoldFont ?? DefaultFont);
			theme.SetFont("main_bold_msdf", "EditorFonts", BoldFont ?? DefaultFont);
			theme.SetFontSize("bold_size", "EditorFonts", 14);
			theme.SetFont("italic", "EditorFonts", DefaultFont);
			theme.SetFontSize("italic_size", "EditorFonts", 14);
			theme.SetFont("title", "EditorFonts", BoldFont ?? DefaultFont);
			theme.SetFontSize("title_size", "EditorFonts", 15);
			theme.SetFont("source", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFontSize("source_size", "EditorFonts", 14);
			theme.SetFont("expression", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFontSize("expression_size", "EditorFonts", 13);
			theme.SetFont("output_source", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFont("output_source_bold", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFont("output_source_italic", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFont("output_source_bold_italic", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFont("output_source_mono", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFontSize("output_source_size", "EditorFonts", 13);
			theme.SetFont("status_source", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFontSize("status_source_size", "EditorFonts", 14);
			theme.SetFont("doc", "EditorFonts", DefaultFont);
			theme.SetFont("doc_bold", "EditorFonts", BoldFont ?? DefaultFont);
			theme.SetFont("doc_italic", "EditorFonts", DefaultFont);
			theme.SetFont("doc_title", "EditorFonts", BoldFont ?? DefaultFont);
			theme.SetFont("doc_source", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFont("doc_keyboard", "EditorFonts", CodeFont ?? DefaultFont);
			theme.SetFontSize("doc_size", "EditorFonts", 16);
			theme.SetFontSize("doc_title_size", "EditorFonts", 23);
			theme.SetFontSize("doc_source_size", "EditorFonts", 15);
			theme.SetFontSize("doc_keyboard_size", "EditorFonts", 14);
			theme.SetFont("rulers", "EditorFonts", DefaultFont);
			theme.SetFontSize("rulers_size", "EditorFonts", 8);
			theme.SetFont("rotation_control", "EditorFonts", DefaultFont);
			theme.SetFontSize("rotation_control_size", "EditorFonts", 13);
			string[] array = new string[20]
			{
				"Control", "Label", "Button", "MenuButton", "OptionButton", "CheckBox", "CheckButton", "LineEdit", "Tree", "ItemList",
				"PopupMenu", "MenuBar", "TabBar", "TabContainer", "ProgressBar", "SpinBox", "FileDialog", "AcceptDialog", "ConfirmationDialog", "TooltipLabel"
			};
			foreach (string text in array)
			{
				theme.SetFont("font", text, DefaultFont);
				theme.SetFontSize("font_size", text, 14);
			}
			array = new string[2] { "TextEdit", "CodeEdit" };
			foreach (string text2 in array)
			{
				theme.SetFont("font", text2, CodeFont ?? DefaultFont);
				theme.SetFontSize("font_size", text2, 14);
			}
			theme.SetFont("normal_font", "RichTextLabel", DefaultFont);
			theme.SetFont("bold_font", "RichTextLabel", BoldFont ?? DefaultFont);
			theme.SetFont("italics_font", "RichTextLabel", DefaultFont);
			theme.SetFont("bold_italics_font", "RichTextLabel", BoldFont ?? DefaultFont);
			theme.SetFontSize("normal_font_size", "RichTextLabel", 14);
			theme.SetFontSize("bold_font_size", "RichTextLabel", 14);
			theme.SetFontSize("italics_font_size", "RichTextLabel", 14);
			theme.SetFontSize("bold_italics_font_size", "RichTextLabel", 14);
			theme.SetTypeVariation("MainScreenButton", "Button");
			theme.SetFont("font", "MainScreenButton", BoldFont ?? DefaultFont);
			theme.SetFontSize("font_size", "MainScreenButton", 16);
			theme.SetTypeVariation("GameNavCard", "Button");
			theme.SetFont("font", "GameNavCard", BoldFont ?? DefaultFont);
			theme.SetFontSize("font_size", "GameNavCard", 16);
			theme.SetTypeVariation("HeaderSmall", "Label");
			theme.SetFont("font", "HeaderSmall", BoldFont ?? DefaultFont);
			theme.SetFontSize("font_size", "HeaderSmall", 14);
			theme.SetTypeVariation("HeaderMedium", "Label");
			theme.SetFont("font", "HeaderMedium", BoldFont ?? DefaultFont);
			theme.SetFontSize("font_size", "HeaderMedium", 15);
			theme.SetTypeVariation("HeaderLarge", "Label");
			theme.SetFont("font", "HeaderLarge", BoldFont ?? DefaultFont);
			theme.SetFontSize("font_size", "HeaderLarge", 17);
			theme.SetFont("title_font", "Window", BoldFont ?? DefaultFont);
			theme.SetFontSize("title_font_size", "Window", 15);
			theme.SetFont("font", "FoldableContainer", BoldFont ?? DefaultFont);
			theme.SetFontSize("font_size", "FoldableContainer", 14);
		}
	}

	private static void ApplyControlColors(Theme theme)
	{
		string[] array = new string[3] { "Control", "Label", "RichTextLabel" };
		foreach (string text in array)
		{
			theme.SetColor("font_color", text, FontColor);
			theme.SetColor("font_shadow_color", text, Transparent);
			theme.SetColor("font_outline_color", text, Transparent);
		}
		theme.SetColor("font_hover_color", "Control", FontHoverColor);
		theme.SetColor("font_pressed_color", "Control", FontPressedColor);
		theme.SetColor("font_disabled_color", "Control", FontDisabledColor);
		theme.SetColor("font_focus_color", "Control", FontFocusColor);
	}

	private static void ApplyContainers(Theme theme)
	{
		theme.SetStylebox("panel", "Panel", MakeStyleBox(DarkColor1, Transparent, 8, 6, 4, 6, 4, 0));
		theme.SetStylebox("panel", "PanelContainer", MakeStyleBox(SurfaceLowColor, Transparent, 0, 4, 4, 4, 4, 0));
		theme.SetStylebox("panel", "PopupPanel", MakeStyleBox(SurfacePopupColor, WithAlpha(ContrastColor1, 0.14f), 8, 4, 4, 4, 4));
		theme.SetStylebox("panel", "TooltipPanel", MakeStyleBox(SurfacePopupColor, WithAlpha(ContrastColor1, 0.14f), 8, 6, 4, 6, 4));
		theme.SetColor("font_color", "TooltipLabel", FontColor);
		ApplyFoldableContainer(theme);
		ApplySplitContainers(theme);
		theme.SetConstant("separation", "HBoxContainer", 4);
		theme.SetConstant("separation", "VBoxContainer", 4);
		theme.SetConstant("h_separation", "GridContainer", 4);
		theme.SetConstant("v_separation", "GridContainer", 4);
	}

	private static void ApplySplitContainers(Theme theme)
	{
		StyleBoxFlat texture = MakeStyleBox(WithAlpha(ContrastColor1, 0.1f), Transparent, 0, 0, 0, 0, 0, 0);
		string[] array = new string[3] { "SplitContainer", "HSplitContainer", "VSplitContainer" };
		foreach (string text in array)
		{
			theme.SetStylebox("split_bar_background", text, texture);
			theme.SetConstant("separation", text, 6);
			theme.SetConstant("minimum_grab_thickness", text, 8);
			theme.SetConstant("autohide", text, 1);
			theme.SetColor("touch_dragger_color", text, WithAlpha(MonoColor, 0.3f));
			theme.SetColor("touch_dragger_hover_color", text, WithAlpha(MonoColor, 0.55f));
			theme.SetColor("touch_dragger_pressed_color", text, WithAlpha(MonoColor, 0.85f));
		}
	}

	private static void ApplyFoldableContainer(Theme theme)
	{
		theme.SetStylebox("title_panel", "FoldableContainer", MakeStyleBox(DarkColor1, Transparent, 8, 4, 4, 4, 4, 0));
		theme.SetStylebox("title_hover_panel", "FoldableContainer", MakeStyleBox(LerpColor(DarkColor1, BaseColor, 0.4f), Transparent, 8, 4, 4, 4, 4, 0));
		theme.SetStylebox("title_collapsed_panel", "FoldableContainer", MakeStyleBox(LerpColor(DarkColor1, Colors.Black, 0.125f), Transparent, 8, 4, 4, 4, 4, 0));
		theme.SetStylebox("title_collapsed_hover_panel", "FoldableContainer", MakeStyleBox(LerpColor(DarkColor1, BaseColor, 0.4f), Transparent, 8, 4, 4, 4, 4, 0));
		theme.SetStylebox("panel", "FoldableContainer", MakeStyleBox(SurfaceLowestColor, Transparent, 0, 4, 4, 4, 4, 0));
		theme.SetStylebox("focus", "FoldableContainer", MakeStyleBox(Transparent, WithAlpha(AccentColor, 0.8f), 8, 2, 2, 2, 2, 2));
		theme.SetColor("font_color", "FoldableContainer", FontColor);
		theme.SetColor("hover_font_color", "FoldableContainer", FontHoverColor);
		theme.SetColor("collapsed_font_color", "FoldableContainer", FontPressedColor);
		theme.SetColor("font_outline_color", "FoldableContainer", Transparent);
		theme.SetConstant("outline_size", "FoldableContainer", 0);
		theme.SetConstant("h_separation", "FoldableContainer", 4);
	}

	private static void ApplyButtons(Theme theme)
	{
		ApplyButtonSet(theme, "Button");
		ApplyButtonSet(theme, "MenuButton");
		ApplyButtonSet(theme, "OptionButton");
		ApplyMainScreenButton(theme);
		ApplyCheckBoxSet(theme, "CheckBox");
		ApplyCheckBoxSet(theme, "CheckButton");
		theme.SetConstant("h_separation", "Button", 6);
		theme.SetConstant("arrow_margin", "OptionButton", 6);
	}

	private static void ApplyCheckBoxSet(Theme theme, string type)
	{
		StyleBoxEmpty texture = new StyleBoxEmpty();
		theme.SetStylebox("normal", type, texture);
		theme.SetStylebox("hover", type, texture);
		theme.SetStylebox("pressed", type, texture);
		theme.SetStylebox("hover_pressed", type, texture);
		theme.SetStylebox("disabled", type, texture);
		theme.SetStylebox("focus", type, texture);
		theme.SetColor("font_color", type, FontColor);
		theme.SetColor("font_hover_color", type, FontHoverColor);
		theme.SetColor("font_pressed_color", type, FontPressedColor);
		theme.SetColor("font_hover_pressed_color", type, FontHoverPressedColor);
		theme.SetColor("font_disabled_color", type, FontDisabledColor);
		theme.SetColor("icon_normal_color", type, FontColor);
		theme.SetColor("icon_hover_color", type, FontHoverColor);
		theme.SetColor("icon_pressed_color", type, FontPressedColor);
		theme.SetColor("icon_hover_pressed_color", type, FontHoverPressedColor);
		theme.SetColor("icon_disabled_color", type, FontDisabledColor);
		theme.SetConstant("h_separation", type, 6);
		theme.SetConstant("check_v_offset", type, 0);
	}

	private static void ApplyButtonSet(Theme theme, string type)
	{
		theme.SetStylebox("normal", type, MakeStyleBox(ButtonNormalColor, ButtonBorderNormalColor, 8, 8, 5, 8, 5));
		theme.SetStylebox("hover", type, MakeStyleBox(ButtonHoverColor, ButtonBorderHoverColor, 8, 8, 5, 8, 5));
		theme.SetStylebox("pressed", type, MakeStyleBox(ButtonPressedColor, ButtonBorderPressedColor, 8, 8, 5, 8, 5));
		theme.SetStylebox("hover_pressed", type, MakeStyleBox(FlatButtonHoverPressedColor, ButtonBorderPressedColor, 8, 8, 5, 8, 5));
		theme.SetStylebox("disabled", type, MakeStyleBox(ButtonDisabledColor, Transparent, 8, 8, 5, 8, 5, 0));
		theme.SetStylebox("focus", type, MakeStyleBox(Transparent, WithAlpha(AccentColor, 0.8f), 8, 2, 2, 2, 2, 2));
		theme.SetColor("font_color", type, FontColor);
		theme.SetColor("font_hover_color", type, FontHoverColor);
		theme.SetColor("font_pressed_color", type, FontHoverPressedColor);
		theme.SetColor("font_hover_pressed_color", type, FontHoverPressedColor);
		theme.SetColor("font_disabled_color", type, FontDisabledColor);
		theme.SetColor("font_focus_color", type, FontFocusColor);
		theme.SetColor("icon_normal_color", type, FontColor);
		theme.SetColor("icon_hover_color", type, FontHoverColor);
		theme.SetColor("icon_pressed_color", type, FontHoverPressedColor);
		theme.SetColor("icon_disabled_color", type, FontDisabledColor);
	}

	private static void ApplyMainScreenButton(Theme theme)
	{
		StyleBoxFlat texture = MakeStyleBox(WithAlpha(DirtColor, 0.76f), WithAlpha(SunGoldColor, 0.16f), 8, 10, 6, 10, 6);
		StyleBoxFlat texture2 = MakeStyleBox(WithAlpha(GrassColor.Lightened(0.12f), 0.96f), WithAlpha(LeafColor, 0.72f), 8, 10, 6, 10, 6, 2);
		StyleBoxFlat texture3 = MakeStyleBox(WithAlpha(GrassColor.Lightened(0.06f), 1f), SunGoldColor, 8, 10, 6, 10, 6, 2);
		StyleBoxFlat texture4 = MakeStyleBox(WithAlpha(DirtColor, 0.35f), Transparent, 8, 10, 6, 10, 6, 0);
		theme.SetStylebox("normal", "MainScreenButton", texture);
		theme.SetStylebox("normal_mirrored", "MainScreenButton", texture);
		theme.SetStylebox("hover", "MainScreenButton", texture2);
		theme.SetStylebox("hover_mirrored", "MainScreenButton", texture2);
		theme.SetStylebox("pressed", "MainScreenButton", texture3);
		theme.SetStylebox("pressed_mirrored", "MainScreenButton", texture3);
		theme.SetStylebox("hover_pressed", "MainScreenButton", texture3);
		theme.SetStylebox("hover_pressed_mirrored", "MainScreenButton", texture3);
		theme.SetStylebox("disabled", "MainScreenButton", texture4);
		theme.SetStylebox("disabled_mirrored", "MainScreenButton", texture4);
		theme.SetStylebox("focus", "MainScreenButton", MakeStyleBox(Transparent, WithAlpha(AccentColor, 0.6f), 8, 2, 2, 2, 2));
		theme.SetColor("font_color", "MainScreenButton", FontColor);
		theme.SetColor("font_hover_color", "MainScreenButton", FontFocusColor);
		theme.SetColor("font_pressed_color", "MainScreenButton", SunGoldColor);
		theme.SetColor("font_hover_pressed_color", "MainScreenButton", SunGoldColor);
		theme.SetColor("font_disabled_color", "MainScreenButton", FontDisabledColor);
		theme.SetColor("icon_normal_color", "MainScreenButton", FontColor);
		theme.SetColor("icon_hover_color", "MainScreenButton", FontFocusColor);
		theme.SetColor("icon_pressed_color", "MainScreenButton", AccentColor);
		theme.SetColor("icon_hover_pressed_color", "MainScreenButton", FontFocusColor);
		theme.SetColor("icon_disabled_color", "MainScreenButton", FontDisabledColor);
		theme.SetConstant("h_separation", "MainScreenButton", 6);
		StyleBoxFlat texture5 = AddGameShadow(MakeStyleBox(WithAlpha(DirtColor, 0.82f), WithAlpha(SunGoldColor, 0.22f), 8, 11, 7, 11, 7), new Color(0f, 0f, 0f, 0.34f), 5, 2);
		StyleBoxFlat texture6 = AddGameShadow(MakeStyleBox(WithAlpha(GrassColor.Lightened(0.15f), 0.98f), WithAlpha(LeafColor.Lightened(0.18f), 0.9f), 8, 11, 7, 11, 7, 2), WithAlpha(LeafColor, 0.32f), 9, 3);
		StyleBoxFlat texture7 = AddGameShadow(MakeStyleBox(WithAlpha(GrassColor.Lightened(0.07f), 1f), SunGoldColor, 8, 11, 7, 11, 7, 2), WithAlpha(SunGoldColor, 0.28f), 10, 3);
		StyleBoxFlat texture8 = AddGameShadow(MakeStyleBox(WithAlpha(DirtColor, 0.32f), Transparent, 8, 11, 7, 11, 7, 0), new Color(0f, 0f, 0f, 0.18f), 3, 1);
		theme.SetStylebox("normal", "GameNavCard", texture5);
		theme.SetStylebox("hover", "GameNavCard", texture6);
		theme.SetStylebox("pressed", "GameNavCard", texture7);
		theme.SetStylebox("hover_pressed", "GameNavCard", texture7);
		theme.SetStylebox("disabled", "GameNavCard", texture8);
		theme.SetStylebox("focus", "GameNavCard", MakeStyleBox(Transparent, WithAlpha(SunGoldColor, 0.86f), 8, 2, 2, 2, 2, 2));
		theme.SetColor("font_color", "GameNavCard", FontColor);
		theme.SetColor("font_hover_color", "GameNavCard", FontFocusColor);
		theme.SetColor("font_pressed_color", "GameNavCard", SunGoldColor);
		theme.SetColor("font_hover_pressed_color", "GameNavCard", SunGoldColor);
		theme.SetColor("font_disabled_color", "GameNavCard", FontDisabledColor);
		theme.SetColor("icon_normal_color", "GameNavCard", FontColor);
		theme.SetColor("icon_hover_color", "GameNavCard", Colors.White);
		theme.SetColor("icon_pressed_color", "GameNavCard", AccentColor);
		theme.SetColor("icon_hover_pressed_color", "GameNavCard", Colors.White);
		theme.SetColor("icon_disabled_color", "GameNavCard", FontDisabledColor);
		theme.SetConstant("h_separation", "GameNavCard", 6);
	}

	private static void ApplyHudTheme(Theme theme)
	{
		theme.SetTypeVariation("HudTopBar", "PanelContainer");
		theme.SetStylebox("panel", "HudTopBar", MakeStyleBox(new Color(0.055f, 0.105f, 0.06f, 0.98f), WithAlpha(DirtColor.Lightened(0.25f), 0.9f), 10, 10, 6, 10, 6, 2));
		theme.SetTypeVariation("HudWorkspaceBar", "PanelContainer");
		theme.SetStylebox("panel", "HudWorkspaceBar", MakeStyleBox(WithAlpha(DirtColor, 0.92f), WithAlpha(SunGoldColor, 0.28f), 10, 10, 5, 10, 5));
		theme.SetTypeVariation("HudCluster", "PanelContainer");
		theme.SetStylebox("panel", "HudCluster", MakeStyleBox(WithAlpha(GrassColor, 0.68f), WithAlpha(LeafColor, 0.36f), 9, 6, 3, 6, 3));
		ApplyHudStatusStyle(theme, "HudStatusIdle", new Color(0.2f, 0.23f, 0.18f), new Color(0.52f, 0.58f, 0.45f));
		ApplyHudStatusStyle(theme, "HudStatusReady", GrassColor.Lightened(0.08f), LeafColor.Lightened(0.18f));
		ApplyHudStatusStyle(theme, "HudStatusRunning", new Color(0.12f, 0.31f, 0.15f), SunGoldColor);
		ApplyHudStatusStyle(theme, "HudStatusPaused", DirtColor.Lightened(0.08f), new Color(1f, 0.63f, 0.25f));
		theme.SetTypeVariation("HudResourceOption", "OptionButton");
		theme.SetStylebox("normal", "HudResourceOption", MakeStyleBox(WithAlpha(GrassColor, 0.82f), WithAlpha(SunGoldColor, 0.32f), 8, 10, 6, 10, 6));
		theme.SetStylebox("hover", "HudResourceOption", MakeStyleBox(GrassColor.Lightened(0.1f), WithAlpha(SunGoldColor, 0.78f), 8, 10, 6, 10, 6, 2));
		theme.SetStylebox("pressed", "HudResourceOption", MakeStyleBox(GrassColor.Lightened(0.04f), SunGoldColor, 8, 10, 6, 10, 6, 2));
		theme.SetStylebox("focus", "HudResourceOption", MakeStyleBox(Transparent, SunGoldColor, 8, 2, 2, 2, 2, 2));
		theme.SetColor("font_color", "HudResourceOption", FontColor);
		theme.SetColor("font_hover_color", "HudResourceOption", Colors.White);
		theme.SetColor("font_pressed_color", "HudResourceOption", SunGoldColor);
		theme.SetColor("icon_normal_color", "HudResourceOption", SunGoldColor);
		theme.SetColor("icon_hover_color", "HudResourceOption", Colors.White);
	}

	private static void ApplyHudStatusStyle(Theme theme, string type, Color background, Color border)
	{
		theme.SetTypeVariation(type, "PanelContainer");
		theme.SetStylebox("panel", type, MakeStyleBox(WithAlpha(background, 0.92f), WithAlpha(border, 0.84f), 12, 9, 4, 9, 4));
	}

	private static void ApplyTextInputs(Theme theme)
	{
		string[] array = new string[3] { "LineEdit", "TextEdit", "CodeEdit" };
		foreach (string text in array)
		{
			theme.SetStylebox("normal", text, MakeStyleBox(SurfaceLowestColor, WithAlpha(ContrastColor1, 0.16f), 2, 6, 4, 6, 4));
			theme.SetStylebox("focus", text, MakeStyleBox(SurfaceLowestColor, WithAlpha(AccentColor, 0.8f), 2, 6, 4, 6, 4, 2));
			theme.SetStylebox("read_only", text, MakeStyleBox(SurfaceLowerColor, Transparent, 2, 6, 4, 6, 4, 0));
			theme.SetColor("font_color", text, FontColor);
			theme.SetColor("font_readonly_color", text, FontReadonlyColor);
			theme.SetColor("font_uneditable_color", text, FontReadonlyColor);
			theme.SetColor("font_placeholder_color", text, FontDisabledColor);
			theme.SetColor("caret_color", text, MonoColor);
			theme.SetColor("selection_color", text, SelectionColor);
			theme.SetColor("current_line_color", text, WithAlpha(MonoColor, 0.035f));
			theme.SetColor("line_number_color", text, FontDisabledColor);
			theme.SetColor("safe_line_number_color", text, WithAlpha(SuccessColor, 0.7f));
			theme.SetColor("word_highlighted_color", text, WithAlpha(AccentColor, 0.18f));
		}
	}

	private static void ApplyLists(Theme theme)
	{
		StyleBoxFlat texture = MakeStyleBox(SelectionColor, Transparent, 8, 4, 2, 4, 2, 0);
		StyleBoxFlat texture2 = MakeStyleBox(SelectionColor, WithAlpha(AccentColor, 0.8f), 8, 4, 2, 4, 2);
		StyleBoxFlat texture3 = MakeStyleBox(FlatButtonHoverColor, Transparent, 8, 4, 2, 4, 2, 0);
		StyleBoxFlat texture4 = MakeStyleBox(Transparent, WithAlpha(AccentColor, 0.8f), 8, 2, 1, 2, 1);
		string[] array = new string[2] { "Tree", "ItemList" };
		foreach (string text in array)
		{
			theme.SetStylebox("panel", text, MakeStyleBox(SurfaceLowestColor, Transparent, 0, 4, 4, 4, 4, 0));
			theme.SetStylebox("selected", text, texture);
			theme.SetStylebox("selected_focus", text, texture2);
			theme.SetStylebox("hovered", text, texture3);
			theme.SetStylebox("hovered_selected", text, MakeStyleBox(WithAlpha(AccentColor, 0.5f), Transparent, 8, 4, 2, 4, 2, 0));
			theme.SetStylebox("cursor", text, texture4);
			theme.SetStylebox("cursor_unfocused", text, texture4);
			theme.SetColor("font_color", text, FontColor);
			theme.SetColor("font_selected_color", text, MonoColor);
			theme.SetColor("font_hovered_color", text, FontHoverColor);
			theme.SetColor("font_uneditable_color", text, FontDisabledColor);
			theme.SetColor("guide_color", text, WithAlpha(MonoColor, 0.12f));
			theme.SetColor("relationship_line_color", text, WithAlpha(MonoColor, 0.18f));
			theme.SetColor("drop_position_color", text, AccentColor);
			theme.SetColor("selected_color", text, SelectionColor);
			theme.SetColor("hovered_color", text, FlatButtonHoverColor);
			theme.SetConstant("item_margin", text, 8);
			theme.SetConstant("inner_item_margin_left", text, 4);
			theme.SetConstant("inner_item_margin_right", text, 4);
			theme.SetConstant("inner_item_margin_top", text, 2);
			theme.SetConstant("inner_item_margin_bottom", text, 2);
			theme.SetConstant("h_separation", text, 4);
			theme.SetConstant("v_separation", text, 2);
			theme.SetConstant("button_margin", text, 2);
			theme.SetConstant("scroll_border", text, 0);
			theme.SetConstant("scroll_speed", text, 12);
		}
	}

	private static void ApplyTabs(Theme theme)
	{
		string[] array = new string[2] { "TabContainer", "TabBar" };
		foreach (string text in array)
		{
			theme.SetStylebox("tabbar_background", text, MakeStyleBox(SurfaceLowestColor, Transparent, 0, 0, 0, 0, 0, 0));
			theme.SetStylebox("tab_selected", text, MakeStyleBox(SurfaceBaseColor, WithAlpha(ContrastColor1, 0.12f), 8, 8, 5, 8, 5));
			theme.SetStylebox("tab_hovered", text, MakeStyleBox(SurfaceHighColor, Transparent, 8, 8, 5, 8, 5, 0));
			theme.SetStylebox("tab_unselected", text, MakeStyleBox(SurfaceLowerColor, Transparent, 8, 8, 5, 8, 5, 0));
			theme.SetStylebox("tab_disabled", text, MakeStyleBox(SurfaceLowestColor, Transparent, 8, 8, 5, 8, 5, 0));
			theme.SetStylebox("tab_focus", text, MakeStyleBox(Transparent, WithAlpha(AccentColor, 0.8f), 8, 2, 2, 2, 2, 2));
			theme.SetColor("font_selected_color", text, FontFocusColor);
			theme.SetColor("font_hovered_color", text, FontHoverColor);
			theme.SetColor("font_unselected_color", text, FontSecondaryColor);
			theme.SetColor("font_disabled_color", text, FontDisabledColor);
			theme.SetColor("icon_selected_color", text, FontFocusColor);
			theme.SetColor("icon_hovered_color", text, FontHoverColor);
			theme.SetColor("icon_unselected_color", text, FontSecondaryColor);
			theme.SetColor("icon_disabled_color", text, FontDisabledColor);
			theme.SetConstant("h_separation", text, 6);
		}
		theme.SetStylebox("panel", "TabContainer", MakeStyleBox(SurfaceLowColor, Transparent, 0, 4, 4, 4, 4, 0));
	}

	private static void ApplyMenus(Theme theme)
	{
		theme.SetStylebox("normal", "MenuBar", MakeStyleBox(Transparent, Transparent, 0, 8, 4, 8, 4, 0));
		theme.SetStylebox("hover", "MenuBar", MakeStyleBox(FlatButtonHoverColor, Transparent, 8, 8, 4, 8, 4, 0));
		theme.SetStylebox("pressed", "MenuBar", MakeStyleBox(FlatButtonPressedColor, Transparent, 8, 8, 4, 8, 4, 0));
		theme.SetStylebox("disabled", "MenuBar", MakeStyleBox(Transparent, Transparent, 0, 8, 4, 8, 4, 0));
		theme.SetColor("font_color", "MenuBar", FontColor);
		theme.SetColor("font_hover_color", "MenuBar", FontHoverColor);
		theme.SetColor("font_pressed_color", "MenuBar", FontHoverPressedColor);
		theme.SetColor("font_disabled_color", "MenuBar", FontDisabledColor);
		theme.SetStylebox("panel", "PopupMenu", MakeStyleBox(SurfacePopupColor, WithAlpha(ContrastColor1, 0.14f), 8, 4, 4, 4, 4));
		theme.SetStylebox("hover", "PopupMenu", MakeStyleBox(FlatButtonHoverColor, Transparent, 8, 4, 3, 4, 3, 0));
		theme.SetStylebox("separator", "PopupMenu", MakeStyleBox(Transparent, WithAlpha(MonoColor, 0.075f), 0, 0, 1, 0, 1));
		theme.SetColor("font_color", "PopupMenu", FontColor);
		theme.SetColor("font_hover_color", "PopupMenu", FontHoverPressedColor);
		theme.SetColor("font_accelerator_color", "PopupMenu", FontSecondaryColor);
		theme.SetColor("font_disabled_color", "PopupMenu", FontDisabledColor);
		theme.SetColor("font_separator_color", "PopupMenu", FontSecondaryColor);
		theme.SetConstant("h_separation", "PopupMenu", 8);
		theme.SetConstant("v_separation", "PopupMenu", 4);
		theme.SetConstant("item_start_padding", "PopupMenu", 6);
		theme.SetConstant("item_end_padding", "PopupMenu", 6);
	}

	private static void ApplyScrollbars(Theme theme)
	{
		string[] array = new string[3] { "ScrollBar", "HScrollBar", "VScrollBar" };
		foreach (string text in array)
		{
			theme.SetStylebox("scroll", text, MakeStyleBox(Transparent, Transparent, 0, 0, 0, 0, 0, 0));
			theme.SetStylebox("scroll_focus", text, MakeStyleBox(Transparent, Transparent, 0, 0, 0, 0, 0, 0));
			theme.SetStylebox("grabber", text, MakeStyleBox(WithAlpha(MonoColor, 0.225f), Transparent, 8, 0, 0, 0, 0, 0));
			theme.SetStylebox("grabber_highlight", text, MakeStyleBox(WithAlpha(MonoColor, 0.3f), Transparent, 8, 0, 0, 0, 0, 0));
			theme.SetStylebox("grabber_pressed", text, MakeStyleBox(WithAlpha(MonoColor, 0.4f), Transparent, 8, 0, 0, 0, 0, 0));
			theme.SetConstant("scroll_width", text, 10);
		}
		theme.SetStylebox("scroll", "ScrollContainer", MakeStyleBox(SurfaceLowestColor, Transparent, 0, 0, 0, 0, 0, 0));
	}

	private static void ApplyGraphTheme(Theme theme)
	{
		StyleBoxFlat texture = MakeStyleBox(SurfaceLowestColor, Transparent, 0, 0, 0, 0, 0, 0);
		theme.SetStylebox("bg", "GraphEdit", texture);
		theme.SetStylebox("panel", "GraphEdit", texture);
		theme.SetColor("grid_major", "GraphEdit", WithAlpha(MonoColor, 0.1f));
		theme.SetColor("grid_minor", "GraphEdit", WithAlpha(MonoColor, 0.05f));
		theme.SetColor("activity_color", "GraphEdit", MonoColor);
		theme.SetColor("selection_fill", "GraphEdit", WithAlpha(AccentColor, 0.18f));
		theme.SetColor("selection_stroke", "GraphEdit", WithAlpha(AccentColor, 0.8f));
		theme.SetColor("connection_hover_tint_color", "GraphEdit", WithAlpha(MonoColor, 0.25f));
		theme.SetStylebox("titlebar", "GraphNode", MakeStyleBox(SurfaceHighColor, Transparent, 8, 8, 5, 8, 5, 0));
		theme.SetStylebox("titlebar_selected", "GraphNode", MakeStyleBox(SurfaceHigherColor, WithAlpha(AccentColor, 0.8f), 8, 8, 5, 8, 5));
		theme.SetStylebox("panel", "GraphNode", MakeStyleBox(SurfaceLowerColor, WithAlpha(ContrastColor1, 0.08f), 0, 6, 6, 6, 4));
		theme.SetStylebox("panel_selected", "GraphNode", MakeStyleBox(SurfaceLowerColor, WithAlpha(AccentColor, 0.8f), 0, 6, 6, 6, 4, 2));
		theme.SetColor("title_color", "GraphNode", FontFocusColor);
		theme.SetColor("close_color", "GraphNode", FontSecondaryColor);
		theme.SetColor("resizer_color", "GraphNode", WithAlpha(MonoColor, 0.25f));
		theme.SetConstant("title_offset", "GraphNode", 12);
		theme.SetConstant("title_h_offset", "GraphNode", 0);
		theme.SetConstant("port_offset", "GraphNode", 6);
	}

	private static void ApplyMisc(Theme theme)
	{
		theme.SetStylebox("background", "ProgressBar", MakeStyleBox(SurfaceLowestColor, WithAlpha(ContrastColor1, 0.1f), 2, 0, 0, 0, 0));
		theme.SetStylebox("fill", "ProgressBar", MakeStyleBox(AccentColor, Transparent, 2, 0, 0, 0, 0, 0));
		theme.SetColor("font_color", "ProgressBar", FontFocusColor);
		theme.SetColor("font_outline_color", "ProgressBar", Transparent);
		theme.SetStylebox("embedded_border", "Window", MakeStyleBox(SurfacePopupColor, WithAlpha(ContrastColor1, 0.18f), 8, 2, 2, 2, 2));
		theme.SetStylebox("embedded_unfocused_border", "Window", MakeStyleBox(SurfacePopupColor, WithAlpha(ContrastColor1, 0.12f), 8, 2, 2, 2, 2));
		theme.SetStylebox("embedded_unfocus_border", "Window", MakeStyleBox(SurfacePopupColor, WithAlpha(ContrastColor1, 0.12f), 8, 2, 2, 2, 2));
		theme.SetColor("title_color", "Window", FontFocusColor);
		theme.SetColor("close_color", "Window", FontSecondaryColor);
		theme.SetColor("close_hover_color", "Window", FontHoverColor);
		theme.SetStylebox("separator", "HSeparator", MakeStyleBox(Transparent, SeparatorColor, 0, 0, 1, 0, 1));
		theme.SetStylebox("separator", "VSeparator", MakeStyleBox(Transparent, SeparatorColor, 0, 1, 0, 1, 0));
		theme.SetColor("font_color", "FileDialog", FontColor);
		theme.SetColor("folder_icon_color", "FileDialog", AccentColor);
		theme.SetColor("file_icon_color", "FileDialog", FontSecondaryColor);
		theme.SetColor("files_disabled", "FileDialog", FontDisabledColor);
		theme.SetColor("clear_button_color", "LineEdit", FontSecondaryColor);
		theme.SetColor("clear_button_color_pressed", "LineEdit", FontFocusColor);
	}

	private static void ApplyIcons(Theme theme)
	{
		SetIconIfExists(theme, "checked", "CheckBox", "GuiChecked");
		SetIconIfExists(theme, "unchecked", "CheckBox", "GuiUnchecked");
		SetIconIfExists(theme, "radio_checked", "CheckBox", "GuiRadioChecked");
		SetIconIfExists(theme, "radio_unchecked", "CheckBox", "GuiRadioUnchecked");
		SetIconIfExists(theme, "checked_disabled", "CheckBox", "GuiCheckedDisabled");
		SetIconIfExists(theme, "unchecked_disabled", "CheckBox", "GuiUncheckedDisabled");
		SetIconIfExists(theme, "radio_checked_disabled", "CheckBox", "GuiRadioCheckedDisabled");
		SetIconIfExists(theme, "radio_unchecked_disabled", "CheckBox", "GuiRadioUncheckedDisabled");
		SetIconIfExists(theme, "checked", "CheckButton", "GuiToggleOn");
		SetIconIfExists(theme, "unchecked", "CheckButton", "GuiToggleOff");
		SetIconIfExists(theme, "arrow", "OptionButton", "GuiOptionArrow");
		SetIconIfExists(theme, "updown", "SpinBox", "GuiSpinboxUpdown");
		SetIconIfExists(theme, "arrow", "MenuButton", "GuiOptionArrow");
		SetIconIfExists(theme, "arrow", "PopupMenu", "GuiOptionArrow");
		SetIconIfExists(theme, "checked", "PopupMenu", "GuiChecked");
		SetIconIfExists(theme, "unchecked", "PopupMenu", "GuiUnchecked");
		SetIconIfExists(theme, "select_arrow", "Tree", "GuiTreeArrowDown");
		SetIconIfExists(theme, "select_option", "Tree", "GuiOptionArrow");
		SetIconIfExists(theme, "arrow", "Tree", "GuiTreeArrowDown");
		SetIconIfExists(theme, "arrow_collapsed", "Tree", "GuiTreeArrowRight");
		SetIconIfExists(theme, "arrow_collapsed_mirrored", "Tree", "GuiTreeArrowLeft");
		SetIconIfExists(theme, "arrow", "TabBar", "GuiTabMenuHl");
		SetIconIfExists(theme, "close", "TabBar", "GuiClose");
		SetIconIfExists(theme, "increment", "HScrollBar", "GuiScrollArrowRight");
		SetIconIfExists(theme, "decrement", "HScrollBar", "GuiScrollArrowLeft");
		SetIconIfExists(theme, "increment", "VScrollBar", "GuiScrollArrowDown");
		SetIconIfExists(theme, "decrement", "VScrollBar", "GuiScrollArrowUp");
		SetIconIfExists(theme, "expanded_arrow", "FoldableContainer", "GuiTreeArrowDown");
		SetIconIfExists(theme, "expanded_arrow_mirrored", "FoldableContainer", "GuiArrowUp");
		SetIconIfExists(theme, "folded_arrow", "FoldableContainer", "GuiTreeArrowRight");
		SetIconIfExists(theme, "folded_arrow_mirrored", "FoldableContainer", "GuiTreeArrowLeft");
		SetIconIfExists(theme, "folder", "FileDialog", "Folder");
		SetIconIfExists(theme, "file", "FileDialog", "File");
		SetIconIfExists(theme, "folder_thumbnail", "FileDialog", "FolderBigThumb");
		SetIconIfExists(theme, "file_thumbnail", "FileDialog", "FileBigThumb");
		SetIconIfExists(theme, "parent_folder", "FileDialog", "ArrowUp");
		SetIconIfExists(theme, "back_folder", "FileDialog", "Back");
		SetIconIfExists(theme, "forward_folder", "FileDialog", "Forward");
		SetIconIfExists(theme, "reload", "FileDialog", "Reload");
		SetIconIfExists(theme, "toggle_hidden", "FileDialog", "GuiVisibilityVisible");
		SetIconIfExists(theme, "toggle_filename_filter", "FileDialog", "FilenameFilter");
		SetIconIfExists(theme, "thumbnail_mode", "FileDialog", "FileThumbnail");
		SetIconIfExists(theme, "list_mode", "FileDialog", "FileList");
		SetIconIfExists(theme, "sort", "FileDialog", "Sort");
		SetIconIfExists(theme, "favorite", "FileDialog", "Favorites");
		SetIconIfExists(theme, "favorite_up", "FileDialog", "MoveUp");
		SetIconIfExists(theme, "favorite_down", "FileDialog", "MoveDown");
		SetIconIfExists(theme, "create_folder", "FileDialog", "FolderCreate");
		SetIconIfExists(theme, "menu_copy_path", "FileDialog", "ActionCopy");
		SetIconIfExists(theme, "menu_delete", "FileDialog", "Remove");
		SetIconIfExists(theme, "menu_refresh", "FileDialog", "Reload");
		SetIconIfExists(theme, "menu_new_folder", "FileDialog", "Folder");
		SetIconIfExists(theme, "menu_show_in_file_manager", "FileDialog", "Filesystem");
		SetIconIfExists(theme, "menu_open_bundle", "FileDialog", "FolderBrowse");
	}

	private static Texture2D LoadIcon(string name)
	{
		string[] array = new string[2]
		{
			"res://addons/ModEditor/Icons/" + name + ".svg",
			"res://addons/ModEditor/Icons/ClassIcon/" + name + ".svg"
		};
		foreach (string path in array)
		{
			if (ResourceLoader.Exists(path))
			{
				return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
			}
		}
		return null;
	}

	private static void SetIconIfExists(Theme theme, string name, string type, string iconName)
	{
		Texture2D texture2D = LoadIcon(iconName);
		if (texture2D != null)
		{
			theme.SetIcon(name, type, texture2D);
		}
	}

	private static StyleBoxFlat MakeStyleBox(Color bg, Color border, int cornerRadius, int marginLeft, int marginTop, int marginRight, int marginBottom, int borderWidth = 1)
	{
		return new StyleBoxFlat
		{
			BgColor = bg,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = cornerRadius,
			CornerRadiusTopRight = cornerRadius,
			CornerRadiusBottomLeft = cornerRadius,
			CornerRadiusBottomRight = cornerRadius,
			ContentMarginLeft = marginLeft,
			ContentMarginRight = marginRight,
			ContentMarginTop = marginTop,
			ContentMarginBottom = marginBottom
		};
	}

	private static StyleBoxFlat AddGameShadow(StyleBoxFlat style, Color color, int size, int offsetY)
	{
		style.ShadowColor = color;
		style.ShadowSize = size;
		style.ShadowOffset = new Vector2(0f, offsetY);
		return style;
	}

	private static Color GetBaseColor(float dimnessOffset = 0f, float saturationMultiplier = 1f)
	{
		float num = ((dimnessOffset < 0f) ? Mathf.Clamp(0.3f, -0.1f, 0.5f) : 0.3f);
		float value = Mathf.Clamp(Lerp(BaseColor.V, 0f, num * dimnessOffset), 0f, 1f);
		float saturation = Mathf.Clamp(BaseColor.S * saturationMultiplier, 0f, 1f);
		return Color.FromHsv(BaseColor.H, saturation, value, BaseColor.A);
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		return new Color(color.R, color.G, color.B, alpha);
	}

	private static Color LerpColor(Color from, Color to, float weight)
	{
		return new Color(Lerp(from.R, to.R, weight), Lerp(from.G, to.G, weight), Lerp(from.B, to.B, weight), Lerp(from.A, to.A, weight));
	}

	private static float Lerp(float from, float to, float weight)
	{
		return from + (to - from) * weight;
	}

	private static float Max(float a, float b)
	{
		if (!(a > b))
		{
			return b;
		}
		return a;
	}

	private static Color ClampColor(Color color)
	{
		return new Color(Mathf.Clamp(color.R, 0f, 1f), Mathf.Clamp(color.G, 0f, 1f), Mathf.Clamp(color.B, 0f, 1f), Mathf.Clamp(color.A, 0f, 1f));
	}

	public static void ApplyTo(Control root)
	{
		root.Theme = Create();
		root.Modulate = Colors.White;
	}
}
