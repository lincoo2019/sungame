using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorAnimationAtlasProfileRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private string _projectRoot = "";

	public override async void _Ready()
	{
		bool action = false;
		bool undoRedo = false;
		bool hiddenStopped = false;
		bool resumed = false;
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
			Require(f3, "F3 did not initialize the real ModEditor.");
			if (!f3)
			{
				Finish();
				return;
			}
			_projectRoot = ProjectSettings.GlobalizePath($"user://AnimationAtlasProfileProbe-{Guid.NewGuid():N}");
			XWModProjectLayout.EnsureProjectLayout(_projectRoot);
			string directoryPath = Path.Combine(_projectRoot, "Resources", "AnimationAtlasProfiles");
			foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(directoryPath, _projectRoot))
			{
				action |= item.Id == "new-animation-atlas-profile";
			}
			Require(action, "Animation atlas profile directory has no strong create action.");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-animation-atlas-profile", directoryPath, "ProbeAtlas");
			string resourcePath = (templateCreateResult.Success ? ProjectSettings.LocalizePath(templateCreateResult.CreatedPath).Replace('\\', '/') : "");
			AdobeAnimateAtlasProfile profile = LoadProfile(resourcePath);
			bool created = templateCreateResult.Success && GodotObject.IsInstanceValid(profile) && profile.ProfileId == "ProbeAtlas" && profile.ManifestPath == "" && !profile.StartupOnly;
			Require(created, $"Animation atlas profile template failed: success={templateCreateResult.Success}; error={templateCreateResult.Error}");
			XWVisualEditorDescriptor descriptor;
			bool route = GodotObject.IsInstanceValid(profile) && XWResourceEditorRegistry.TryGetEditor(profile, resourcePath, out descriptor) && descriptor.Category == "AnimationAtlas" && descriptor.DockKey == "animation_atlas_editor";
			Require(route, "Animation atlas profile did not route to animation_atlas_editor.");
			Node sentinel = new Node
			{
				Name = "AtlasProfileInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(sentinel);
			XWEditorInterface.Instance.EditResource(profile);
			XWEditorInterface.Instance.FocusPanel("animation_atlas_editor");
			XWGenericVisualResourceEditor editor = await WaitForEditor("animation_atlas_editor", 240);
			(XWEditorInterface.Instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
			XWEditorInterface.Instance.FocusPanel("animation_atlas_editor");
			await WaitFrames(8);
			XWDirectPropertySurface surface = editor?.FindChild("DirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			GD.Print($"[MOD_EDITOR_ANIMATION_ATLAS_PROFILE_DIAGNOSTIC] editor={editor?.GetType().Name ?? "null"} active={editor?.ActiveResource?.GetType().Name ?? "null"} editable={editor?.DirectEditablePropertyCount ?? (-1)} mounted={editor?.DirectMountedPropertyCount ?? (-1)} missing={editor?.DirectMissingPropertyCount ?? (-1)} names={string.Join(",", surface?.EditablePropertyNames ?? Array.Empty<string>())} visible={editor?.IsVisibleInTree() ?? false}");
			XWInlineTextSurface instance = editor?.FindChild("InlineTextSurface", recursive: true, owned: false) as XWInlineTextSurface;
			bool direct = GodotObject.IsInstanceValid(editor) && GodotObject.IsInstanceValid(surface) && GodotObject.IsInstanceValid(instance) && editor.DirectEditablePropertyCount >= 3 && editor.DirectMissingPropertyCount == 0 && editor.InlineTechnicalTextCount >= 2 && surface.EditablePropertyNames.Contains("ProfileId") && surface.EditablePropertyNames.Contains("ManifestPath") && surface.EditablePropertyNames.Contains("StartupOnly") && InspectorIsHidden(editor);
			Require(direct, "Animation atlas profile did not expose all properties on the main visual surface.");
			XWUndoRedoManager history = XWEditorInterface.Instance.GetUndoRedoManager();
			LineEdit lineEdit = editor?.FindChild("Inline_ProfileId", recursive: true, owned: false) as LineEdit;
			Require(GodotObject.IsInstanceValid(lineEdit), "ProfileId direct control was not found.");
			if (GodotObject.IsInstanceValid(lineEdit) && GodotObject.IsInstanceValid(history))
			{
				history.ClearHistory();
				lineEdit.EmitSignal(Control.SignalName.FocusEntered);
				lineEdit.Text = "ProbeAtlasEdited";
				lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, "ProbeAtlasEdited");
				await WaitFrames(4);
				bool applied = profile.ProfileId == "ProbeAtlasEdited" && history.HasUndo();
				bool undone = history.Undo();
				await WaitFrames(4);
				undone &= profile.ProfileId == "ProbeAtlas";
				bool redone = history.Redo();
				await WaitFrames(4);
				redone &= profile.ProfileId == "ProbeAtlasEdited";
				undoRedo = applied && undone && redone;
				GD.Print($"[MOD_EDITOR_ANIMATION_ATLAS_PROFILE_EDIT_DIAGNOSTIC] applied={applied} undone={undone} redone={redone} value={profile.ProfileId}");
				Require(undoRedo, "ProfileId edit did not round-trip through shared Undo/Redo.");
			}
			CheckBox checkBox = FindFirst<CheckBox>(FindPropertyEditor(surface, "StartupOnly"));
			Require(GodotObject.IsInstanceValid(checkBox), "StartupOnly direct toggle was not found.");
			if (GodotObject.IsInstanceValid(checkBox))
			{
				checkBox.ButtonPressed = true;
				await WaitFrames(4);
			}
			bool saved = GodotObject.IsInstanceValid(editor) && editor.SaveActiveResource();
			await WaitFrames(8);
			AdobeAnimateAtlasProfile adobeAnimateAtlasProfile = LoadProfile(resourcePath);
			bool saveReload = saved && GodotObject.IsInstanceValid(adobeAnimateAtlasProfile) && adobeAnimateAtlasProfile.ProfileId == "ProbeAtlasEdited" && adobeAnimateAtlasProfile.StartupOnly;
			Require(saveReload, "Atlas profile direct edits did not survive Save/cache-ignore reload.");
			if (GodotObject.IsInstanceValid(editor))
			{
				editor.Hide();
				await WaitFrames(3);
				hiddenStopped = editor.ProcessMode == ProcessModeEnum.Disabled && !editor.IsProcessing();
				editor.Show();
				XWEditorInterface.Instance.FocusPanel("animation_atlas_editor");
				await WaitFrames(3);
				resumed = editor.ProcessMode != ProcessModeEnum.Disabled;
			}
			Require(hiddenStopped, "Hidden animation atlas editor still processes.");
			Require(resumed, "Animation atlas editor did not resume after becoming visible.");
			ulong before = Time.GetTicksMsec();
			await WaitFrames(6);
			bool flag = Time.GetTicksMsec() - before < 1000;
			Require(flag, "Animation atlas authoring blocked the main thread.");
			XWInspector xWInspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool flag2 = xWInspector == null || xWInspector.CurrentObject == sentinel;
			GD.Print($"[MOD_EDITOR_ANIMATION_ATLAS_PROFILE_INSPECTOR_DIAGNOSTIC] current={xWInspector?.CurrentObject?.GetType().Name ?? "null"} sentinelValid={GodotObject.IsInstanceValid(sentinel)}");
			Require(flag2, "Animation atlas editor replaced the global Inspector object.");
			GD.Print($"[MOD_EDITOR_ANIMATION_ATLAS_PROFILE_PROBE] f3={f3} action={action} created={created} route={route} direct={direct} undoRedo={undoRedo} saveReload={saveReload} hiddenStopped={hiddenStopped} resumed={resumed} responsive={flag} inspectorUntouched={flag2} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static AdobeAnimateAtlasProfile LoadProfile(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return ResourceLoader.Load<AdobeAnimateAtlasProfile>(path, "", ResourceLoader.CacheMode.Ignore);
		}
		return null;
	}

	private static async Task<bool> WaitForInterface(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (GodotObject.IsInstanceValid(XWEditorInterface.Instance?.GetEditorPanel()) && GodotObject.IsInstanceValid(XWEditorInterface.Instance?.GetInspector()))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static async Task<XWGenericVisualResourceEditor> WaitForEditor(string dockKey, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor(dockKey) is XWGenericVisualResourceEditor xWGenericVisualResourceEditor && GodotObject.IsInstanceValid(xWGenericVisualResourceEditor.FindChild("DirectPropertySurface", recursive: true, owned: false)))
			{
				return xWGenericVisualResourceEditor;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static XWInspectorPropertyEditorBase FindPropertyEditor(Node root, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase && xWInspectorPropertyEditorBase.Property?.PropName.ToString() == propertyName)
		{
			return xWInspectorPropertyEditorBase;
		}
		foreach (Node child in root.GetChildren())
		{
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase2 = FindPropertyEditor(child, propertyName);
			if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase2))
			{
				return xWInspectorPropertyEditorBase2;
			}
		}
		return null;
	}

	private static T FindFirst<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren("*", typeof(T).Name, recursive: true, owned: false))
		{
			if (item is T result)
			{
				return result;
			}
		}
		return null;
	}

	private static bool InspectorIsHidden(Control editor)
	{
		Control control = editor?.FindChild("InspectorPanel", recursive: true, owned: false) as Control;
		Control control2 = editor?.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as Control;
		if (!GodotObject.IsInstanceValid(control) || !control.IsVisibleInTree())
		{
			if (GodotObject.IsInstanceValid(control2))
			{
				return !control2.IsVisibleInTree();
			}
			return true;
		}
		return false;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private static async Task WaitFrames(int frames)
	{
		SceneTree tree = Engine.GetMainLoop() as SceneTree;
		for (int index = 0; index < frames; index++)
		{
			await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError(failure);
		}
		if (!string.IsNullOrWhiteSpace(_projectRoot) && Directory.Exists(_projectRoot))
		{
			Directory.Delete(_projectRoot, recursive: true);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
