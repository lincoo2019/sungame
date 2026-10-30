using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorDirectPropertySurfaceRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWGenericVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		bool undoRedo = false;
		bool enumUndoRedo = false;
		bool enumSavedReloaded = false;
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
			bool f3 = await WaitForEditor(900);
			Require(f3, "F3 did not initialize the real generic resource editor.");
			if (!f3)
			{
				Finish();
				return;
			}
			Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://DirectPropertySurfaceProbe"));
			string resourcePath = "user://DirectPropertySurfaceProbe/DirectPropertyProbe.tres";
			XWDirectPropertyProbeResource resource = new XWDirectPropertyProbeResource();
			Require(ResourceSaver.Save(resource, resourcePath, ResourceSaver.SaverFlags.None) == Error.Ok, "Probe resource could not be saved initially.");
			Node sentinel = new Node
			{
				Name = "DirectPropertyInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector seededInspector = await WaitForInspector(120);
			seededInspector?.EditObject(sentinel);
			await WaitFrames(2);
			Require(GodotObject.IsInstanceValid(seededInspector) && seededInspector.CurrentObject == sentinel, "Global Inspector sentinel could not be seeded before the direct-resource edit.");
			XWEditorInterface.Instance.EditResource(resource);
			XWEditorInterface.Instance.FocusPanel("resource_editor");
			await WaitFrames(10);
			XWDirectPropertySurface surface = _editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			string[] source = new string[11]
			{
				"Caption", "Enabled", "Count", "Speed", "SmallMode", "LargeMode", "Tint", "Offset", "Icon", "Tags",
				"Costs"
			};
			bool direct = GodotObject.IsInstanceValid(surface) && surface.MissingPropertyCount == 0 && surface.MountedPropertyCount == surface.EditablePropertyCount && source.All((string name) => surface.EditablePropertyNames.Contains(name));
			Require(direct, $"Direct surface coverage failed: editable={surface?.EditablePropertyCount ?? (-1)}; mounted={surface?.MountedPropertyCount ?? (-1)}; missing={surface?.MissingPropertyCount ?? (-1)}.");
			PanelContainer panelContainer = _editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
			Control control = _editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as Control;
			bool inspectorHidden = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(control) && control.GetChildCount() == 0 && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
			Require(inspectorHidden, "Generic resource editor exposed or instantiated the raw Inspector.");
			HFlowContainer obj = surface?.FindChild("DirectVisualEnum_SmallMode", recursive: true, owned: false) as HFlowContainer;
			HFlowContainer largeVisualHost = surface?.FindChild("DirectVisualEnum_LargeMode", recursive: true, owned: false) as HFlowContainer;
			Button button = obj?.FindChild("VisualOption2", recursive: true, owned: false) as Button;
			Button largeGallery = largeVisualHost?.FindChild("DirectEnumGallery_LargeMode", recursive: true, owned: false) as Button;
			bool visualEnums = GodotObject.IsInstanceValid(surface) && surface.VisualizedEnumPropertyCount == 2 && surface.SegmentedEnumPropertyCount == 1 && surface.GalleryEnumPropertyCount == 1 && surface.VisibleRawEnumOptionCount == 0 && GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(largeGallery);
			Require(visualEnums, $"Enum visual closure failed: visual={surface?.VisualizedEnumPropertyCount ?? (-1)}; segmented={surface?.SegmentedEnumPropertyCount ?? (-1)}; gallery={surface?.GalleryEnumPropertyCount ?? (-1)}; rawVisible={surface?.VisibleRawEnumOptionCount ?? (-1)}.");
			bool lazyGallery = GodotObject.IsInstanceValid(largeVisualHost) && largeVisualHost.FindChild("GalleryOption0", recursive: true, owned: false) == null;
			Require(lazyGallery, "Closed enum gallery eagerly built hidden option cards.");
			if (visualEnums)
			{
				_history.ClearHistory();
				button.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(4);
				bool smallApplied = resource.SmallMode == 2 && _history.HasUndo();
				bool smallUndone = _history.Undo();
				await WaitFrames(3);
				smallUndone &= resource.SmallMode == 1;
				bool smallRedone = _history.Redo();
				await WaitFrames(3);
				smallRedone &= resource.SmallMode == 2;
				_history.ClearHistory();
				largeGallery.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(3);
				Button button2 = largeVisualHost.FindChild("GalleryOption7", recursive: true, owned: false) as Button;
				Require(GodotObject.IsInstanceValid(button2), "Large enum gallery did not render the Repeater card.");
				button2?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(4);
				bool largeApplied = resource.LargeMode == 7 && _history.HasUndo();
				bool largeUndone = _history.Undo();
				await WaitFrames(3);
				largeUndone &= resource.LargeMode == 2;
				bool largeRedone = _history.Redo();
				await WaitFrames(3);
				largeRedone &= resource.LargeMode == 7;
				enumUndoRedo = smallApplied && smallUndone && smallRedone && largeApplied && largeUndone && largeRedone;
				Require(enumUndoRedo, "Segmented/gallery enum edits did not round-trip through shared Undo/Redo.");
				Require(ResourceSaver.Save(resource, resourcePath, ResourceSaver.SaverFlags.None) == Error.Ok, "Visual enum probe resource could not be saved.");
				XWDirectPropertyProbeResource xWDirectPropertyProbeResource = ResourceLoader.Load<XWDirectPropertyProbeResource>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
				enumSavedReloaded = GodotObject.IsInstanceValid(xWDirectPropertyProbeResource) && xWDirectPropertyProbeResource.SmallMode == 2 && xWDirectPropertyProbeResource.LargeMode == 7;
				Require(enumSavedReloaded, "Visual enum edits did not survive cache-ignore Save/Reload.");
			}
			LineEdit lineEdit = _editor.FindChild("Inline_Caption", recursive: true, owned: false) as LineEdit;
			Require(GodotObject.IsInstanceValid(lineEdit), "Caption did not mount in its unique inline game-text position.");
			if (GodotObject.IsInstanceValid(lineEdit))
			{
				_history.ClearHistory();
				lineEdit.EmitSignal(Control.SignalName.FocusEntered);
				lineEdit.Text = "Edited in game surface";
				lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, lineEdit.Text);
				lineEdit.EmitSignal(Control.SignalName.FocusExited);
				await WaitFrames(5);
				bool largeRedone = resource.Caption == "Edited in game surface" && _history.HasUndo();
				bool largeUndone = _history.Undo();
				await WaitFrames(3);
				largeUndone &= resource.Caption == "Original caption";
				bool largeApplied = _history.Redo();
				await WaitFrames(3);
				largeApplied &= resource.Caption == "Edited in game surface";
				undoRedo = largeRedone && largeUndone && largeApplied;
				Require(undoRedo, "Direct text control did not round-trip through shared Undo/Redo.");
			}
			string nativePath = "user://DirectPropertySurfaceProbe/DirectLabelSettings.tres";
			LabelSettings resource2 = new LabelSettings
			{
				FontSize = 24
			};
			Require(ResourceSaver.Save(resource2, nativePath, ResourceSaver.SaverFlags.None) == Error.Ok, "Native probe resource could not be saved initially.");
			LabelSettings labelSettings = ResourceLoader.Load<LabelSettings>(nativePath, "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(labelSettings) && !string.IsNullOrWhiteSpace(labelSettings.ResourcePath), "Native probe resource did not reload with a save path.");
			XWEditorInterface.Instance.EditResource(labelSettings);
			XWEditorInterface.Instance.FocusPanel("resource_editor");
			await WaitFrames(8);
			surface = _editor.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			SpinBox spinBox = (surface?.FindChild("Direct_font_size", recursive: true, owned: false))?.FindChild("SpinBox", recursive: true, owned: false) as SpinBox;
			Require(GodotObject.IsInstanceValid(spinBox), "LabelSettings font_size did not mount a direct number control.");
			if (GodotObject.IsInstanceValid(spinBox))
			{
				spinBox.EmitSignal(Control.SignalName.FocusEntered);
				spinBox.Value = 37.0;
				spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 37.0);
				spinBox.EmitSignal(Control.SignalName.FocusExited);
				await WaitFrames(4);
			}
			Button button3 = FindButtonByText(_editor, "保存");
			Require(GodotObject.IsInstanceValid(button3), "Generic resource toolbar has no Save button.");
			GD.Print($"[MOD_EDITOR_DIRECT_PROPERTY_SAVE_DIAGNOSTIC] beforeSave={labelSettings.FontSize} resourcePath={labelSettings.ResourcePath} expectedPath={nativePath} button={GodotObject.IsInstanceValid(button3)}");
			button3?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(8);
			LabelSettings labelSettings2 = ResourceLoader.Load<LabelSettings>(nativePath, "", ResourceLoader.CacheMode.Ignore);
			GD.Print($"[MOD_EDITOR_DIRECT_PROPERTY_SAVE_DIAGNOSTIC] afterSave={labelSettings.FontSize} reloaded={GodotObject.IsInstanceValid(labelSettings2)} reloadedValue={labelSettings2?.FontSize ?? (-1)}");
			bool savedReloaded = GodotObject.IsInstanceValid(labelSettings2) && labelSettings2.FontSize == 37;
			Require(savedReloaded, "Direct property edit did not survive cache-ignore Save/Reload.");
			(int, int, int) obj2 = await AuditAllAuthorResourceTypes();
			int item = obj2.Item1;
			int item2 = obj2.Item2;
			int item3 = obj2.Item3;
			bool flag = item > 0 && item2 == item && item3 == 0;
			Require(flag, $"All-type direct control audit failed: authorTypes={item}; audited={item2}; missingTypes={item3}.");
			XWInspector xWInspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool flag2 = xWInspector == null || xWInspector.CurrentObject == sentinel;
			GD.Print($"[MOD_EDITOR_DIRECT_PROPERTY_INSPECTOR_DIAGNOSTIC] currentType={xWInspector?.CurrentObject?.GetType().FullName ?? "<null>"} currentName={(xWInspector?.CurrentObject as Node)?.Name.ToString() ?? "<not-node>"} sentinelValid={GodotObject.IsInstanceValid(sentinel)} same={flag2}");
			Require(flag2, "Direct resource surface replaced the global Inspector selection.");
			GD.Print($"[MOD_EDITOR_DIRECT_PROPERTY_SURFACE_PROBE] f3={f3} direct={direct} inspectorHidden={inspectorHidden} visualEnums={visualEnums} lazyGallery={lazyGallery} enumUndoRedo={enumUndoRedo} enumSavedReloaded={enumSavedReloaded} undoRedo={undoRedo} savedReloaded={savedReloaded} authorTypes={item} auditedTypes={item2} missingTypes={item3} allTypes={flag} inspectorUntouched={flag2} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(int AuthorTypes, int AuditedTypes, int MissingTypes)> AuditAllAuthorResourceTypes()
	{
		List<Type> types = (from type2 in typeof(TowerDefenseBackgroundMusicConfig).Assembly.GetTypes()
			where !type2.IsAbstract && typeof(Resource).IsAssignableFrom(type2) && type2.GetCustomAttributes(typeof(GlobalClassAttribute), inherit: false).Length != 0 && IsAuthorableType(type2)
			select type2).OrderBy<Type, string>((Type type2) => type2.FullName, StringComparer.Ordinal).ToList();
		XWDirectPropertySurface auditSurface = new XWDirectPropertySurface
		{
			Visible = false
		};
		AddChild(auditSurface, forceReadableName: false, InternalMode.Disabled);
		int audited = 0;
		int missing = 0;
		for (int index = 0; index < types.Count; index++)
		{
			Type type = types[index];
			Resource resource;
			try
			{
				resource = Activator.CreateInstance(type) as Resource;
			}
			catch (Exception ex)
			{
				Require(condition: false, $"{type.FullName} could not be instantiated: {ex.GetType().Name}: {ex.Message}");
				missing++;
				continue;
			}
			if (!GodotObject.IsInstanceValid(resource))
			{
				Require(condition: false, type.FullName + " did not create a valid Resource.");
				missing++;
				continue;
			}
			auditSurface.BindResource(resource, _history);
			audited++;
			if (auditSurface.MissingPropertyCount != 0 || auditSurface.MountedPropertyCount != auditSurface.EditablePropertyCount)
			{
				missing++;
				Require(condition: false, type.FullName + " missing direct controls: " + string.Join(",", auditSurface.MissingPropertyNames));
			}
			if ((index + 1) % 12 == 0)
			{
				await WaitFrames(1);
			}
		}
		auditSurface.QueueFree();
		await WaitFrames(2);
		return (AuthorTypes: types.Count, AuditedTypes: audited, MissingTypes: missing);
	}

	private static bool IsAuthorableType(Type type)
	{
		foreach (XWVisualEditorDescriptor allEditor in XWResourceEditorRegistry.GetAllEditors())
		{
			if (allEditor.Category == "General")
			{
				continue;
			}
			Type type2 = type;
			while (type2 != null)
			{
				if (allEditor.ResourceClassNames.Contains(type2.Name))
				{
					return true;
				}
				type2 = type2.BaseType;
			}
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance?.GetResourceEditor("resource_editor") is XWGenericVisualResourceEditor editor)
			{
				_editor = editor;
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<XWInspector> WaitForInspector(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetInspector() is XWInspector result)
			{
				return result;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static Button FindButtonByText(Node root, string text)
	{
		foreach (Node item in root.FindChildren("*", "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.Text == text)
			{
				return button;
			}
		}
		return null;
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
			GD.PrintErr("[MOD_EDITOR_DIRECT_PROPERTY_SURFACE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_DIRECT_PROPERTY_SURFACE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
