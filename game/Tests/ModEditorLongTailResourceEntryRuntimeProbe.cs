using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorLongTailResourceEntryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 4;
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
			Require(f3, "F3 did not initialize all long-tail resource editors.");
			if (!f3)
			{
				Finish();
				return;
			}
			string text = ProjectSettings.GlobalizePath("user://LongTailResourceEntryProbe");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			string text2 = Path.Combine(text, "Resources", "BuffVisuals");
			string text3 = Path.Combine(text, "Resources", "AwardSettlements");
			string text4 = Path.Combine(text, "Resources", "CollisionGeometry");
			bool actions = HasAction(text2, text, "new-buff-visual") && HasAction(text3, text, "new-award-settlement") && HasAction(text4, text, "new-character-hitbox") && HasAction(text4, text, "new-aabb-shape") && HasAction(text4, text, "new-aabb-ray");
			Require(actions, "One or more long-tail resource directories have no create action.");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-buff-visual", text2, "FrozenAura");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = XWResourceCreateRoute.CreateFromAction("new-award-settlement", text3, "ChapterReward");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult3 = XWResourceCreateRoute.CreateFromAction("new-character-hitbox", text4, "PlantHitBox");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult4 = XWResourceCreateRoute.CreateFromAction("new-aabb-shape", text4, "AttackArea");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult5 = XWResourceCreateRoute.CreateFromAction("new-aabb-ray", text4, "FireCheckRay");
			string path = LocalPath(templateCreateResult);
			string awardPath = LocalPath(templateCreateResult2);
			string hitBoxPath = LocalPath(templateCreateResult3);
			string path2 = LocalPath(templateCreateResult4);
			string path3 = LocalPath(templateCreateResult5);
			BuffVisualDefinition buffVisualDefinition = Load<BuffVisualDefinition>(path);
			AwardSettlementConfig award = Load<AwardSettlementConfig>(awardPath);
			CharacterHitBoxDefinition hitBox = Load<CharacterHitBoxDefinition>(hitBoxPath);
			AabbShape2DResource shape = Load<AabbShape2DResource>(path2);
			AabbRay2DResource ray = Load<AabbRay2DResource>(path3);
			bool created = GodotObject.IsInstanceValid(buffVisualDefinition) && buffVisualDefinition.ResourceName == "FrozenAura" && buffVisualDefinition.buffKey == new StringName("FrozenAura") && buffVisualDefinition.nodeName == new StringName("FrozenAuraVisual") && buffVisualDefinition.enabled && buffVisualDefinition.scale.IsEqualApprox(Vector2.One) && buffVisualDefinition.drawBand == AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation && GodotObject.IsInstanceValid(award) && award.ResourceName == "ChapterReward" && GodotObject.IsInstanceValid(hitBox) && hitBox.Size.IsEqualApprox(new Vector2(80f, 80f)) && hitBox.DefaultEnabled && hitBox.DefaultMonitorable && GodotObject.IsInstanceValid(shape) && shape.Geometry is RectangleShape2D { Size: var size } && size.IsEqualApprox(new Vector2(80f, 80f)) && GodotObject.IsInstanceValid(ray) && ray.TargetPosition.IsEqualApprox(new Vector2(2000f, 0f));
			Require(created, $"Strong templates did not load with their expected defaults: buff={templateCreateResult.Error}, award={templateCreateResult2.Error}, hitbox={templateCreateResult3.Error}, shape={templateCreateResult4.Error}, ray={templateCreateResult5.Error}");
			bool routes = RoutesTo(buffVisualDefinition, path, "BuffVisual", "buff_visual_editor") && RoutesTo(award, awardPath, "Collectable", "collectable_editor") && RoutesTo(hitBox, hitBoxPath, "CollisionGeometry", "collision_geometry_editor") && RoutesTo(shape, path2, "CollisionGeometry", "collision_geometry_editor") && RoutesTo(ray, path3, "CollisionGeometry", "collision_geometry_editor");
			Require(routes, "One or more created long-tail resources did not route to a dedicated editor.");
			Node inspectorSentinel = new Node
			{
				Name = "LongTailInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool inspectorBaseline = InspectorTargets(inspector, inspectorSentinel);
			Require(inspectorBaseline, "Long-tail resource probe could not establish the raw Inspector sentinel.");
			(bool, bool, bool) tuple = await ProbeBuff(buffVisualDefinition, path);
			bool buffDirect = tuple.Item1;
			bool buffUndoRedo = tuple.Item2;
			bool buffSave = tuple.Item3;
			tuple = await ProbeAward(award, awardPath);
			bool awardDirect = tuple.Item1;
			bool awardUndoRedo = tuple.Item2;
			bool awardSave = tuple.Item3;
			(bool, bool, bool, bool) obj = await ProbeCollision(hitBox, hitBoxPath, shape, ray);
			bool item = obj.Item1;
			bool item2 = obj.Item2;
			bool item3 = obj.Item3;
			bool item4 = obj.Item4;
			bool flag = InspectorTargets(inspector, inspectorSentinel);
			Require(flag, "Long-tail resource surfaces replaced the raw Inspector object.");
			GD.Print($"[MOD_EDITOR_LONG_TAIL_RESOURCE_ENTRY_PROBE] f3={f3} actions={actions} created={created} routes={routes} buffDirect={buffDirect} buffUndoRedo={buffUndoRedo} buffSave={buffSave} awardDirect={awardDirect} awardUndoRedo={awardUndoRedo} awardSave={awardSave} collisionDirect={item} collisionUndoRedo={item2} collisionSave={item3} variantsDirect={item4} inspectorBaseline={inspectorBaseline} inspectorUntouched={flag} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeBuff(BuffVisualDefinition buff, string path)
	{
		XWEditorInterface.Instance.EditResource(buff);
		XWEditorInterface.Instance.FocusPanel("buff_visual_editor");
		await WaitFrames(5);
		XWBuffVisualResourceEditor editor = XWEditorInterface.Instance.GetResourceEditor("buff_visual_editor") as XWBuffVisualResourceEditor;
		SpinBox spinBox = Find<SpinBox>(editor, "BuffPositionX");
		bool flag = HasAll(editor, "BuffResourceName", "BuffLocalToScene", "BuffKeyEdit", "BuffEnabled", "BuffTexturePicker", "BuffScenePicker", "BuffNodeName", "BuffPositionX", "BuffPositionY", "BuffScaleX", "BuffScaleY", "BuffRotation", "BuffZIndex", "BuffZAsRelative", "BuffCentered", "BuffOffsetX", "BuffOffsetY", "BuffDrawBand", "BuffVisualStage", "SaveBuffVisualButton");
		bool direct = GodotObject.IsInstanceValid(editor) && flag && InspectorIsHidden(editor);
		Require(direct, "BUFF visual resource did not mount its complete inspector-free game surface.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.SetValueNoSignal(42.0);
		spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 42.0);
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		bool applied = Math.Abs(buff.position.X - 42f) < 0.001f && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && Math.Abs(buff.position.X) < 0.001f;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && Math.Abs(buff.position.X - 42f) < 0.001f;
		bool undoRedo = applied && undone && redone;
		Require(undoRedo, "BUFF visual position did not round-trip through shared UndoRedo.");
		Find<Button>(editor, "SaveBuffVisualButton").EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		BuffVisualDefinition buffVisualDefinition = Load<BuffVisualDefinition>(path);
		bool flag2 = GodotObject.IsInstanceValid(buffVisualDefinition) && Math.Abs(buffVisualDefinition.position.X - 42f) < 0.001f && buffVisualDefinition.buffKey == new StringName("FrozenAura");
		Require(flag2, "BUFF visual direct edit did not survive editor save/reload.");
		return (Direct: direct, UndoRedo: undoRedo, Save: flag2);
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeAward(AwardSettlementConfig award, string path)
	{
		XWEditorInterface.Instance.EditResource(award);
		XWEditorInterface.Instance.FocusPanel("collectable_editor");
		await WaitFrames(5);
		XWCollectableVisualResourceEditor xWCollectableVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("collectable_editor") as XWCollectableVisualResourceEditor;
		LineEdit lineEdit = Find<LineEdit>(xWCollectableVisualResourceEditor, "ResourceNameEdit");
		bool direct = GodotObject.IsInstanceValid(xWCollectableVisualResourceEditor) && GodotObject.IsInstanceValid(lineEdit) && HasAll(xWCollectableVisualResourceEditor, "BackgroundPicker", "ImagePicker", "TexturePicker", "LocalToSceneCheck", "Preview") && InspectorIsHidden(xWCollectableVisualResourceEditor);
		Require(direct, "Award settlement resource did not mount its complete inspector-free visual window.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		lineEdit.EmitSignal(Control.SignalName.FocusEntered);
		lineEdit.Text = "EditedChapterReward";
		lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, lineEdit.Text);
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		await WaitFrames(2);
		bool applied = award.ResourceName == "EditedChapterReward" && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && award.ResourceName == "ChapterReward";
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && award.ResourceName == "EditedChapterReward";
		bool flag = applied && undone && redone;
		Require(flag, "Award settlement direct name edit did not round-trip through shared UndoRedo.");
		AwardSettlementConfig awardSettlementConfig = ((ResourceSaver.Save(award, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? Load<AwardSettlementConfig>(path) : null);
		bool flag2 = GodotObject.IsInstanceValid(awardSettlementConfig) && awardSettlementConfig.ResourceName == "EditedChapterReward";
		Require(flag2, "Award settlement edit did not survive save/reload.");
		return (Direct: direct, UndoRedo: flag, Save: flag2);
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save, bool VariantsDirect)> ProbeCollision(CharacterHitBoxDefinition hitBox, string path, AabbShape2DResource shape, AabbRay2DResource ray)
	{
		XWEditorInterface.Instance.EditResource(hitBox);
		XWEditorInterface.Instance.FocusPanel("collision_geometry_editor");
		await WaitFrames(5);
		XWCollisionGeometryVisualResourceEditor editor = XWEditorInterface.Instance.GetResourceEditor("collision_geometry_editor") as XWCollisionGeometryVisualResourceEditor;
		SpinBox spinBox = Find<SpinBox>(editor, "CollisionOriginX");
		bool direct = GodotObject.IsInstanceValid(editor) && GodotObject.IsInstanceValid(spinBox) && HasAll(editor, "CollisionGeometryCanvas", "SaveCollisionGeometryButton") && InspectorIsHidden(editor);
		Require(direct, "Character hitbox did not mount its inspector-free collision canvas.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false, VariantsDirect: false);
		}
		_history.ClearHistory();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.SetValueNoSignal(35.0);
		spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 35.0);
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		bool applied = Math.Abs(hitBox.LocalTransform.Origin.X - 35f) < 0.001f && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && Math.Abs(hitBox.LocalTransform.Origin.X) < 0.001f;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && Math.Abs(hitBox.LocalTransform.Origin.X - 35f) < 0.001f;
		bool undoRedo = applied && undone && redone;
		Require(undoRedo, "Collision origin did not round-trip through shared UndoRedo.");
		CharacterHitBoxDefinition characterHitBoxDefinition = ((ResourceSaver.Save(hitBox, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? Load<CharacterHitBoxDefinition>(path) : null);
		bool save = GodotObject.IsInstanceValid(characterHitBoxDefinition) && Math.Abs(characterHitBoxDefinition.LocalTransform.Origin.X - 35f) < 0.001f;
		Require(save, "Collision geometry edit did not survive save/reload.");
		XWEditorInterface.Instance.EditResource(shape);
		XWEditorInterface.Instance.FocusPanel("collision_geometry_editor");
		await WaitFrames(4);
		bool shapeDirect = HasAll(editor, "CollisionGeometryCanvas") && InspectorIsHidden(editor);
		XWEditorInterface.Instance.EditResource(ray);
		XWEditorInterface.Instance.FocusPanel("collision_geometry_editor");
		await WaitFrames(4);
		bool flag = HasAll(editor, "CollisionGeometryCanvas") && InspectorIsHidden(editor);
		bool flag2 = shapeDirect && flag;
		Require(flag2, "AABB shape or ray did not open in the direct collision canvas.");
		return (Direct: direct, UndoRedo: undoRedo, Save: save, VariantsDirect: flag2);
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetInspector() is XWInspector && instance.GetResourceEditor("buff_visual_editor") is XWBuffVisualResourceEditor && instance.GetResourceEditor("collectable_editor") is XWCollectableVisualResourceEditor && instance.GetResourceEditor("collision_geometry_editor") is XWCollisionGeometryVisualResourceEditor)
			{
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static bool InspectorTargets(XWInspector inspector, GodotObject expected)
	{
		if (GodotObject.IsInstanceValid(inspector) && GodotObject.IsInstanceValid(inspector.CurrentObject) && GodotObject.IsInstanceValid(expected))
		{
			return inspector.CurrentObject.GetInstanceId() == expected.GetInstanceId();
		}
		return false;
	}

	private static bool RoutesTo(Resource resource, string path, string category, string dockKey)
	{
		if (GodotObject.IsInstanceValid(resource) && XWResourceEditorRegistry.TryGetEditor(resource, path, out var descriptor) && descriptor?.Category == category)
		{
			return descriptor.DockKey == dockKey;
		}
		return false;
	}

	private static T Load<T>(string path) where T : Resource
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Ignore);
		}
		return null;
	}

	private static bool HasAction(string directory, string root, string actionId)
	{
		foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(directory, root))
		{
			if (item.Id == actionId)
			{
				return true;
			}
		}
		return false;
	}

	private static string LocalPath(XWTemplateLibrary.TemplateCreateResult result)
	{
		if (!result.Success || string.IsNullOrWhiteSpace(result.CreatedPath))
		{
			return "";
		}
		return ProjectSettings.LocalizePath(result.CreatedPath).Replace('\\', '/');
	}

	private static bool InspectorIsHidden(XWGenericVisualResourceEditor editor)
	{
		PanelContainer panelContainer = editor?.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		if (GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible)
		{
			return editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
		}
		return false;
	}

	private static bool HasAll(Node root, params string[] names)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		foreach (string pattern in names)
		{
			if (!GodotObject.IsInstanceValid(root.FindChild(pattern, recursive: true, owned: false)))
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
			GD.PrintErr("[MOD_EDITOR_LONG_TAIL_RESOURCE_ENTRY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_LONG_TAIL_RESOURCE_ENTRY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
