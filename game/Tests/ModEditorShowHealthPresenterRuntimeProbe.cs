using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorShowHealthPresenterRuntimeProbe : Node
{
	private const string ResourcePath = "user://ModEditorShowHealthPresenterProbe/ShowHealthPresenter.tres";

	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	private XWCharacterComponentVisualResourceEditor _editor;

	public override async void _Ready()
	{
		try
		{
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish();
				return;
			}
			await WaitFrames(2);
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			bool f3 = await WaitForInterface(900);
			Require(f3, "F3 did not initialize the Mod editor interface.");
			if (!f3)
			{
				Finish();
				return;
			}
			string path = ProjectSettings.GlobalizePath("user://ModEditorShowHealthPresenterProbe");
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
			Directory.CreateDirectory(path);
			ShowHealthComponentDefinition definition = new ShowHealthComponentDefinition
			{
				ComponentTypeId = "ShowHealthComponent",
				DefinitionId = "probe.show_health",
				InstanceId = "probe.show_health.instance",
				textTemplate = "HP:{0}/{1}",
				roundHitpoints = true,
				decimalPlaces = 0,
				showBody = true,
				showSecondaryArmor = true,
				showHelmet = true
			};
			Require(ResourceSaver.Save(definition, "user://ModEditorShowHealthPresenterProbe/ShowHealthPresenter.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "ShowHealth probe resource could not be saved.");
			definition = ResourceLoader.Load<ShowHealthComponentDefinition>("user://ModEditorShowHealthPresenterProbe/ShowHealthPresenter.tres", "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(definition), "ShowHealth probe resource could not cache-ignore reload.");
			Node inspectorSentinel = new Node
			{
				Name = "ShowHealthInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			inspector?.EditObject(inspectorSentinel);
			await WaitFrames(2);
			XWVisualEditorDescriptor descriptor;
			bool route = XWResourceEditorRegistry.TryGetEditor(definition, "user://ModEditorShowHealthPresenterProbe/ShowHealthPresenter.tres", out descriptor) && descriptor.Category == "CharacterComponent" && descriptor.DockKey == "character_component_editor";
			Require(route, "ShowHealth definition did not route to character_component_editor.");
			XWEditorInterface.Instance.EditResource(definition, XWResourceEditContext.ForRoot(definition, "user://ModEditorShowHealthPresenterProbe/ShowHealthPresenter.tres", "character_component_editor"));
			XWEditorInterface.Instance.FocusPanel("character_component_editor");
			bool flag = await WaitForComponentEditor(definition, 900);
			Require(flag, "Character component editor did not mount the ShowHealth presenter.");
			if (!flag)
			{
				Finish();
				return;
			}
			XWShowHealthComponentPresenter presenter = _editor.ShowHealthPresenter;
			PanelContainer panelContainer = Find<PanelContainer>(_editor, "ShowHealthHudPresenter");
			LineEdit template = Find<LineEdit>(_editor, "ShowHealthTextTemplateEdit");
			Label instance = Find<Label>(_editor, "ShowHealthShieldHudRowPreviewText");
			Label instance2 = Find<Label>(_editor, "ShowHealthHelmetHudRowPreviewText");
			Label bodyPreview = Find<Label>(_editor, "ShowHealthBodyHudRowPreviewText");
			Button button = Find<Button>(_editor, "ShowHealthVisibility_showSecondaryArmor");
			Button helmetVisible = Find<Button>(_editor, "ShowHealthVisibility_showHelmet");
			Button button2 = Find<Button>(_editor, "ShowHealthVisibility_showBody");
			bool directHud = GodotObject.IsInstanceValid(presenter?.Root) && panelContainer == presenter.Root && HasAll(_editor, "ShowHealthHudStage", "ShowHealthShieldHudRow", "ShowHealthHelmetHudRow", "ShowHealthBodyHudRow", "ShowHealthCharacterPlate") && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance2) && GodotObject.IsInstanceValid(bodyPreview);
			bool inlineTemplate = GodotObject.IsInstanceValid(template) && template.GetParent() == bodyPreview?.GetParent() && template.Text == definition.textTemplate;
			Require(directHud && inlineTemplate, "ShowHealth presenter did not edit textTemplate at the three-row game HUD position.");
			bool visualControls = ButtonsHaveVisibleText(button, helmetVisible, button2) && HasAll(_editor, "ShowHealthRoundInteger", "ShowHealthRoundDecimal", "ShowHealthDecimal_0", "ShowHealthDecimal_1", "ShowHealthDecimal_2", "ShowHealthDecimal_3", "ShowHealthPriorityShieldFirst", "ShowHealthPriorityHeadCoverFirst", "ShowHealthColor_shieldColor", "ShowHealthColor_helmetColor", "ShowHealthColor_bodyColor");
			Require(visualControls, "ShowHealth image/puzzle controls or visible row captions were incomplete.");
			bool remainingDynamic = HasDynamicPropertyControl(_editor, "viewScene") && HasDynamicPropertyControl(_editor, "keepScreenAligned") && HasDynamicPropertyControl(_editor, "displayZIndex");
			bool noDuplicates = XWShowHealthComponentPresenter.SpecializedProperties.Count == 10 && CountGenericSpecializedControls(_editor) == 0;
			Require(remainingDynamic, "ShowHealth remaining fields were not retained by the metadata-driven editor.");
			Require(noDuplicates, "ShowHealth specialized fields were duplicated by generic property rows.");
			_history.ClearHistory();
			ulong templateId = template.GetInstanceId();
			template.EmitSignal(Control.SignalName.FocusEntered);
			template.Text = "生命 {0}/{1}";
			template.EmitSignal(LineEdit.SignalName.TextChanged, template.Text);
			await WaitFrames(2);
			string text = ShowHealthComponent.FormatHealthForDisplay(723.6, 1000.0, "生命 {0}/{1}", 0, roundHitpoints: true);
			bool livePreview = bodyPreview.Text == text && presenter.PreviewRevision > 0;
			template.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(3);
			bool applied = definition.textTemplate == "生命 {0}/{1}" && _history.HasUndo();
			bool undone = _history.Undo();
			await WaitFrames(3);
			bool num = undone;
			int num2;
			if (definition.textTemplate == "HP:{0}/{1}")
			{
				LineEdit lineEdit = Find<LineEdit>(_editor, "ShowHealthTextTemplateEdit");
				num2 = ((lineEdit != null && lineEdit.GetInstanceId() == templateId) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			undone = (byte)((num ? 1u : 0u) & (uint)num2) != 0;
			bool redone = _history.Redo();
			await WaitFrames(3);
			redone &= definition.textTemplate == "生命 {0}/{1}";
			bool undoRedo = applied && undone && redone;
			Require(livePreview && undoRedo, "ShowHealth inline template did not preview live and round-trip through leaf-only Undo/Redo.");
			Find<Button>(_editor, "ShowHealthRoundDecimal")?.EmitSignal(BaseButton.SignalName.Pressed);
			Find<Button>(_editor, "ShowHealthDecimal_2")?.EmitSignal(BaseButton.SignalName.Pressed);
			Find<Button>(_editor, "ShowHealthPriorityHeadCoverFirst")?.EmitSignal(BaseButton.SignalName.Pressed);
			helmetVisible?.EmitSignal(BaseButton.SignalName.Toggled, false);
			await WaitFrames(4);
			string text2 = ShowHealthComponent.FormatHealthForDisplay(723.6, 1000.0, definition.textTemplate, definition.decimalPlaces, definition.roundHitpoints);
			bodyPreview = Find<Label>(_editor, "ShowHealthBodyHudRowPreviewText");
			instance2 = Find<Label>(_editor, "ShowHealthHelmetHudRowPreviewText");
			livePreview &= definition.decimalPlaces == 2 && !definition.roundHitpoints && definition.secondaryArmorPriority == ShowHealthComponent.SecondaryArmorPriority.HeadCoverFirst && !definition.showHelmet && bodyPreview?.Text == text2 && instance2 != null && !instance2.Visible;
			bool runtimeFormat = text2 == "生命 723.60/1000.00" && bodyPreview?.Text == ShowHealthComponent.FormatHealthForDisplay(723.6, 1000.0, definition.textTemplate, definition.decimalPlaces, definition.roundHitpoints);
			Require(livePreview && runtimeFormat, "ShowHealth HUD preview drifted from runtime formatting/visibility behavior.");
			Color finalShieldColor = new Color("80deea");
			Color finalHelmetColor = new Color("ffd180");
			CommitColor(Find<ColorPickerButton>(_editor, "ShowHealthColor_shieldColor"), finalShieldColor);
			CommitColor(Find<ColorPickerButton>(_editor, "ShowHealthColor_helmetColor"), finalHelmetColor);
			await WaitFrames(3);
			instance = Find<Label>(_editor, "ShowHealthShieldHudRowPreviewText");
			instance2 = Find<Label>(_editor, "ShowHealthHelmetHudRowPreviewText");
			livePreview &= definition.shieldColor.IsEqualApprox(finalShieldColor) && definition.helmetColor.IsEqualApprox(finalHelmetColor) && instance.GetThemeColor("font_color").IsEqualApprox(finalShieldColor) && instance2.GetThemeColor("font_color").IsEqualApprox(finalHelmetColor);
			Require(livePreview, "Shield/helmet ColorPicker changes did not update their HUD rows in real time.");
			Color originalBodyColor = definition.bodyColor;
			Color finalBodyColor = new Color("81d4fa");
			ColorPickerButton colorPickerButton = Find<ColorPickerButton>(_editor, "ShowHealthColor_bodyColor");
			_history.ClearHistory();
			colorPickerButton?.EmitSignal(BaseButton.SignalName.Pressed);
			colorPickerButton?.EmitSignal(ColorPickerButton.SignalName.ColorChanged, new Color("ff8a80"));
			colorPickerButton?.EmitSignal(ColorPickerButton.SignalName.ColorChanged, new Color("b9f6ca"));
			colorPickerButton?.EmitSignal(ColorPickerButton.SignalName.ColorChanged, finalBodyColor);
			colorPickerButton?.EmitSignal(ColorPickerButton.SignalName.PopupClosed);
			await WaitFrames(3);
			bool colorApplied = definition.bodyColor.IsEqualApprox(finalBodyColor) && _history.HasUndo();
			bool colorUndone = _history.Undo();
			await WaitFrames(3);
			colorUndone &= definition.bodyColor.IsEqualApprox(originalBodyColor) && !_history.HasUndo();
			bool colorRedone = _history.Redo();
			await WaitFrames(3);
			colorRedone &= definition.bodyColor.IsEqualApprox(finalBodyColor);
			bool colorSingleUndo = colorApplied && colorUndone && colorRedone;
			Require(colorSingleUndo, "ColorPicker continuous preview did not collapse to exactly one Undo action.");
			bool saveReload = _editor.SaveActiveResource();
			await WaitFrames(6);
			bool oldPresenterReleased = presenter.Root == null;
			Require(oldPresenterReleased, "Save rebuild retained the disposed ShowHealth presenter root.");
			ShowHealthComponentDefinition showHealthComponentDefinition = ResourceLoader.Load<ShowHealthComponentDefinition>("user://ModEditorShowHealthPresenterProbe/ShowHealthPresenter.tres", "", ResourceLoader.CacheMode.Ignore);
			saveReload &= GodotObject.IsInstanceValid(showHealthComponentDefinition) && showHealthComponentDefinition.textTemplate == "生命 {0}/{1}" && showHealthComponentDefinition.decimalPlaces == 2 && !showHealthComponentDefinition.roundHitpoints && !showHealthComponentDefinition.showHelmet && showHealthComponentDefinition.secondaryArmorPriority == ShowHealthComponent.SecondaryArmorPriority.HeadCoverFirst && showHealthComponentDefinition.shieldColor.IsEqualApprox(finalShieldColor) && showHealthComponentDefinition.helmetColor.IsEqualApprox(finalHelmetColor) && showHealthComponentDefinition.bodyColor.IsEqualApprox(finalBodyColor);
			Require(saveReload, "ShowHealth specialized edits did not survive Save/cache-ignore reload.");
			PanelContainer panelContainer2 = _editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
			bool inspectorUntouched = GodotObject.IsInstanceValid(panelContainer2) && !panelContainer2.Visible && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null && inspector?.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "ShowHealth presenter exposed or replaced the raw Inspector.");
			presenter = _editor.ShowHealthPresenter;
			XWEditorInterface.Instance.FocusPanel("bp_editor");
			await WaitFrames(4);
			bool flag2 = GodotObject.IsInstanceValid(presenter?.Root) && !_editor.IsVisibleInTree() && _editor.ProcessMode == ProcessModeEnum.Disabled && presenter.IsProcessIdle;
			Require(flag2, $"Hidden ShowHealth presenter kept processing: editorMode={_editor.ProcessMode} rootValid={GodotObject.IsInstanceValid(presenter.Root)} rootProcess={presenter.Root?.IsProcessing()} rootPhysics={presenter.Root?.IsPhysicsProcessing()}.");
			GD.Print($"[MOD_EDITOR_SHOW_HEALTH_PRESENTER_PROBE] f3={f3} route={route} directHud={directHud} inlineTemplate={inlineTemplate} visualControls={visualControls} remainingDynamic={remainingDynamic} noDuplicates={noDuplicates} livePreview={livePreview} runtimeFormat={runtimeFormat} undoRedo={undoRedo} colorSingleUndo={colorSingleUndo} saveReload={saveReload} oldPresenterReleased={oldPresenterReleased} inspectorUntouched={inspectorUntouched} hiddenStopped={flag2} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> WaitForInterface(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			Control control = instance?.GetEditorPanel();
			Node instance2 = control?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(control) && !GodotObject.IsInstanceValid(instance2) && instance.GetInspector() is XWInspector)
			{
				(control.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				_history = instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForComponentEditor(ShowHealthComponentDefinition definition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			_editor = XWEditorInterface.Instance?.GetResourceEditor("character_component_editor") as XWCharacterComponentVisualResourceEditor;
			if (GodotObject.IsInstanceValid(_editor) && _editor.SelectedDefinition == definition && GodotObject.IsInstanceValid(_editor.ShowHealthPresenter?.Root))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static bool ButtonsHaveVisibleText(params Button[] buttons)
	{
		foreach (Button button in buttons)
		{
			if (!GodotObject.IsInstanceValid(button) || string.IsNullOrWhiteSpace(button.Text) || button.CustomMinimumSize.X <= 0f)
			{
				return false;
			}
		}
		return true;
	}

	private static void CommitColor(ColorPickerButton picker, Color color)
	{
		if (GodotObject.IsInstanceValid(picker))
		{
			picker.EmitSignal(BaseButton.SignalName.Pressed);
			picker.EmitSignal(ColorPickerButton.SignalName.ColorChanged, color);
			picker.EmitSignal(ColorPickerButton.SignalName.PopupClosed);
		}
	}

	private static bool HasDynamicPropertyControl(Node root, string property)
	{
		foreach (Node item in root.FindChildren("*", "", recursive: true, owned: false))
		{
			if (item is Control control && (control.TooltipText == property || control.TooltipText.StartsWith(property + " ·", StringComparison.Ordinal)))
			{
				return true;
			}
		}
		return false;
	}

	private static int CountGenericSpecializedControls(Node root)
	{
		int num = 0;
		foreach (string specializedProperty in XWShowHealthComponentPresenter.SpecializedProperties)
		{
			foreach (Node item in root.FindChildren("*", "", recursive: true, owned: false))
			{
				if (item is Control control && (control.TooltipText == specializedProperty || control.TooltipText.StartsWith(specializedProperty + " ·", StringComparison.Ordinal) || item.Name == (StringName)("EnumSource_" + specializedProperty)))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static bool HasAll(Node root, params string[] names)
	{
		foreach (string pattern in names)
		{
			if (!GodotObject.IsInstanceValid(root?.FindChild(pattern, recursive: true, owned: false)))
			{
				return false;
			}
		}
		return true;
	}

	private static T Find<T>(Node root, string name) where T : Node
	{
		return root?.FindChild(name, recursive: true, owned: false) as T;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_SHOW_HEALTH_PRESENTER_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SHOW_HEALTH_PRESENTER_FAILURE] " + failure);
		}
		string path = ProjectSettings.GlobalizePath("user://ModEditorShowHealthPresenterProbe");
		if (Directory.Exists(path))
		{
			try
			{
				Directory.Delete(path, recursive: true);
			}
			catch
			{
			}
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
