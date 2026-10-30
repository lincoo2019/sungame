using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorSlotPositionPresenterRuntimeProbe : Node
{
	private const string ResourcePath = "user://ModEditorSlotPositionPresenterProbe/SlotPositionPresenter.tres";

	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	private XWCharacterComponentVisualResourceEditor _editor;

	private static readonly string[] BaseDirectFields = new string[8] { "ComponentTypeId", "DefinitionId", "InstanceId", "WireIndex", "SchemaVersion", "InitiallyAlive", "StateMachineDefinition", "LegacyNodeNames" };

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
			Require(GodotObject.IsInstanceValid(modEditorManager), "无法创建 ModEditorManager。");
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
			Require(f3, "F3 未初始化真实 Mod 编辑器。");
			if (!f3)
			{
				Finish();
				return;
			}
			string builtInDirectory = "res://Script/Component/TowerDefense/Character/SlotComponent/Definitions";
			string[] array = (from file in DirAccess.GetFilesAt(builtInDirectory)
				where file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
				select builtInDirectory + "/" + file).OrderBy<string, string>((string result) => result, StringComparer.Ordinal).ToArray();
			SlotComponentDefinition[] array2 = array.Select(LoadDefinition).Where(GodotObject.IsInstanceValid).ToArray();
			bool builtIns15 = array.Length == 15 && array2.Length == 15 && array2.All((SlotComponentDefinition slotComponentDefinition2) => !slotComponentDefinition2.posMarkPath.IsEmpty);
			string[] padIds = new string[3] { "builtin.component.slot.lily_pad", "builtin.component.slot.pea_lily_pad", "builtin.component.slot.sun_pad" };
			bool padPotDifference = array2.Where((SlotComponentDefinition slotComponentDefinition2) => padIds.Contains<string>(slotComponentDefinition2.DefinitionId, StringComparer.Ordinal)).All((SlotComponentDefinition slotComponentDefinition2) => slotComponentDefinition2.heightFollow && !slotComponentDefinition2.hideShadow) && array2.Where((SlotComponentDefinition slotComponentDefinition2) => !padIds.Contains<string>(slotComponentDefinition2.DefinitionId, StringComparer.Ordinal)).All((SlotComponentDefinition slotComponentDefinition2) => !slotComponentDefinition2.heightFollow && slotComponentDefinition2.hideShadow);
			Require(builtIns15 && padPotDifference, "15 个内置 Slot 资源或 Pad/Pot heightFollow/hideShadow 差异发生漂移。");
			string path = ProjectSettings.GlobalizePath("user://ModEditorSlotPositionPresenterProbe");
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
			Directory.CreateDirectory(path);
			SlotComponentDefinition definition = new SlotComponentDefinition
			{
				ResourceName = "SlotPositionProbe",
				ComponentTypeId = "SlotComponent",
				DefinitionId = "probe.slot.position",
				InstanceId = "probe.slot.position.instance",
				WireIndex = 2,
				SchemaVersion = 1,
				InitiallyAlive = true,
				LegacyNodeNames = new Array<StringName>
				{
					new StringName("SlotComponent")
				},
				posMarkPath = new NodePath("MarkerA"),
				heightFollow = false,
				hideShadow = true,
				occupantRefreshInterval = 3,
				restoreGroundHeightOnRelease = true
			};
			Require(ResourceSaver.Save(definition, "user://ModEditorSlotPositionPresenterProbe/SlotPositionPresenter.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "Slot 探针资源保存失败。");
			definition = LoadDefinition("user://ModEditorSlotPositionPresenterProbe/SlotPositionPresenter.tres");
			Require(GodotObject.IsInstanceValid(definition), "Slot 探针资源无法重载。");
			Node inspectorSentinel = new Node
			{
				Name = "SlotInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			inspector?.EditObject(inspectorSentinel);
			await WaitFrames(2);
			XWVisualEditorDescriptor descriptor;
			bool route = XWResourceEditorRegistry.TryGetEditor(definition, "user://ModEditorSlotPositionPresenterProbe/SlotPositionPresenter.tres", out descriptor) && descriptor.Category == "CharacterComponent" && descriptor.DockKey == "character_component_editor";
			Require(route, "Slot 定义未路由到角色组件编辑器。");
			XWEditorInterface.Instance.EditResource(definition, XWResourceEditContext.ForRoot(definition, "user://ModEditorSlotPositionPresenterProbe/SlotPositionPresenter.tres", "character_component_editor"));
			XWEditorInterface.Instance.FocusPanel("character_component_editor");
			bool flag = await WaitForComponentEditor(definition, 900);
			Require(flag, "角色组件编辑器未挂载 SlotPositionPresenter。");
			if (!flag)
			{
				Finish();
				return;
			}
			_editor.SetWorkbenchPage(0);
			await WaitFrames(3);
			XWSlotPositionPresenter presenter = _editor.SlotPositionPresenter;
			PanelContainer presenterRoot = Find<PanelContainer>(_editor, "SlotPositionPresenter");
			Node2D node2D = Find<Node2D>(_editor, "PreviewWorld");
			XWSlotPositionOverlay overlay = Find<XWSlotPositionOverlay>(_editor, "SlotPositionPreviewOverlay");
			int num = CountBaseDirectFields(_editor);
			string value = MissingBaseDirectFields(_editor);
			bool flag2 = GodotObject.IsInstanceValid(Find<LineEdit>(_editor, "SlotPositionMarkerPath"));
			bool flag3 = GodotObject.IsInstanceValid(Find<SpinBox>(_editor, "SlotOccupantRefreshIntervalValue"));
			bool flag4 = GodotObject.IsInstanceValid(Find<Button>(_editor, "SlotHeightModes_fixed"));
			bool flag5 = GodotObject.IsInstanceValid(Find<Button>(_editor, "SlotHeightModes_follow"));
			bool flag6 = GodotObject.IsInstanceValid(Find<Button>(_editor, "SlotShadowModes_hide"));
			bool flag7 = GodotObject.IsInstanceValid(Find<Button>(_editor, "SlotReleaseModes_restore"));
			bool allFields = num == 8 && XWSlotPositionPresenter.SpecializedProperties.Count == 5 && flag2 && flag3 && flag4 && flag5 && flag6 && flag7;
			bool noDuplicates = CountGenericSpecializedControls(_editor, presenterRoot) == 0;
			bool layerUi = GodotObject.IsInstanceValid(Find<Control>(_editor, "SlotInsideLayer")) && GodotObject.IsInstanceValid(Find<Control>(_editor, "SlotSurroundLayer")) && GodotObject.IsInstanceValid(Find<Label>(_editor, "SlotHeightLineExplanation"));
			Require(allFields && noDuplicates && layerUi, $"13 字段直编、专用字段去重或两个实际占用层不完整：base={num} missing={value} marker={flag2} interval={flag3} fixed={flag4} follow={flag5} shadow={flag6} release={flag7} duplicate={noDuplicates} layers={layerUi}。");
			Node2D node2D2 = new Node2D
			{
				Name = "SlotPreviewOwner",
				Position = new Vector2(128f, 84f),
				Rotation = 0.31f,
				Scale = new Vector2(-1.25f, 0.82f),
				ProcessMode = ProcessModeEnum.Disabled
			};
			Marker2D node = new Marker2D
			{
				Name = "MarkerA",
				Position = new Vector2(-34f, -48f)
			};
			Marker2D markerB = new Marker2D
			{
				Name = "MarkerB",
				Position = new Vector2(42f, -31f)
			};
			node2D2.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			node2D2.AddChild(markerB, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
			(SlotComponent Runtime, bool Layers, bool Height) tuple = CreateAndRunRealCellHarness();
			SlotComponent item = tuple.Runtime;
			bool item2 = tuple.Layers;
			bool item3 = tuple.Height;
			bool realCellOccupants = item2;
			bool heightLine = item3;
			presenter.BindPreviewRuntime(node2D2, item);
			await WaitFrames(3);
			Label markerStatus = Find<Label>(_editor, "SlotPositionMarkerStatus");
			Label label = Find<Label>(_editor, "SlotInsideLayerStatus");
			Label label2 = Find<Label>(_editor, "SlotSurroundLayerStatus");
			bool markerPin = presenter.MarkerPinCount == 1 && presenter.OverlayInputActive;
			bool markerValidation = markerStatus?.Text.Contains("有效 Marker2D", StringComparison.Ordinal) ?? false;
			layerUi &= label != null && label.Text.Contains("SlotOccupant", StringComparison.Ordinal) && (label2?.Text.Contains("SurroundOccupant", StringComparison.Ordinal) ?? false);
			Require(realCellOccupants && heightLine && markerPin && markerValidation && layerUi, "真实 Cell slot/surround 分层、高度写入或角色画面 marker pin 未工作。");
			LineEdit lineEdit = Find<LineEdit>(_editor, "SlotPositionMarkerPath");
			_history.ClearHistory();
			lineEdit.EmitSignal(Control.SignalName.FocusEntered);
			lineEdit.Text = string.Empty;
			lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, string.Empty);
			lineEdit.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(2);
			bool emptyRejected = !presenter.MarkerValid && presenter.MarkerPinCount == 0 && presenter.MeaninglessSimulationStopped && markerStatus.Text.Contains("假模拟已停止", StringComparison.Ordinal);
			bool emptyUndo = _history.Undo();
			await WaitFrames(2);
			emptyUndo &= definition.posMarkPath.ToString() == "MarkerA" && presenter.MarkerValid;
			markerValidation = markerValidation && emptyRejected && emptyUndo;
			Require(markerValidation, "空路径被标为可用，或无 marker 时仍进行假模拟。");
			_history.ClearHistory();
			ulong rootId = presenter.Root.GetInstanceId();
			int runtimeBuildBefore = _editor.RuntimePreviewBuildCount;
			Vector2 markerHandlePosition = presenter.GetMarkerHandlePosition();
			Vector2 toLocal = overlay.GetGlobalTransformWithCanvas().AffineInverse() * markerB.GetGlobalTransformWithCanvas().Origin;
			DragOverlay(overlay, markerHandlePosition, toLocal);
			await WaitFrames(3);
			bool dragApplied = definition.posMarkPath.ToString() == "MarkerB" && _history.HasUndo();
			bool dragUndone = _history.Undo();
			await WaitFrames(2);
			dragUndone &= definition.posMarkPath.ToString() == "MarkerA";
			bool dragRedone = _history.Redo();
			await WaitFrames(2);
			dragRedone &= definition.posMarkPath.ToString() == "MarkerB";
			bool transformedDrag = dragApplied && dragUndone && dragRedone && presenter.Root.GetInstanceId() == rootId && _editor.RuntimePreviewBuildCount == runtimeBuildBefore;
			Require(transformedDrag, "负 scale/旋转 Canvas 下 marker 吸附或完整 Undo/Redo 失败。");
			SpinBox spinBox = Find<SpinBox>(_editor, "SlotOccupantRefreshIntervalValue");
			_history.ClearHistory();
			spinBox.EmitSignal(Control.SignalName.FocusEntered);
			spinBox.Value = 7.0;
			spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 7.0);
			spinBox.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(2);
			bool scalarApplied = definition.occupantRefreshInterval == 7 && presenter.Root.GetInstanceId() == rootId && _editor.RuntimePreviewBuildCount == runtimeBuildBefore;
			bool scalarUndo = _history.Undo();
			await WaitFrames(2);
			scalarUndo &= definition.occupantRefreshInterval == 3;
			bool scalarRedo = _history.Redo();
			await WaitFrames(2);
			scalarRedo &= definition.occupantRefreshInterval == 7;
			bool scalarNoRebuild = scalarApplied && scalarUndo && scalarRedo && presenter.Root.GetInstanceId() == rootId;
			_history.ClearHistory();
			Find<Button>(_editor, "SlotHeightModes_follow")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			_ = definition.heightFollow;
			bool modeUndo = _history.Undo();
			await WaitFrames(2);
			modeUndo &= !definition.heightFollow;
			bool modeRedo = _history.Redo();
			await WaitFrames(2);
			modeRedo &= definition.heightFollow;
			bool undoRedo = scalarUndo && scalarRedo && modeUndo && modeRedo;
			bool shadowModes = (Find<Button>(_editor, "SlotShadowModes_hide")?.ButtonPressed ?? false) && (Find<Button>(_editor, "SlotReleaseModes_restore")?.ButtonPressed ?? false);
			Label label3 = Find<Label>(_editor, "SlotPhysicsScanStatus");
			bool physicsCadence = label3 != null && label3.Text.Contains("物理帧", StringComparison.Ordinal) && label3.Text.Contains("Cell.GetSlot / GetSurround", StringComparison.Ordinal) && GodotObject.IsInstanceValid(Find<Control>(_editor, "SlotPhysicsCadenceStrip"));
			bool noFakeCooldown = label3 != null && label3.Text.Contains("不是卡牌冷却", StringComparison.Ordinal) && label3.Text.Contains("不会读取或修改 PacketConfig", StringComparison.Ordinal);
			Require(scalarNoRebuild && undoRedo && shadowModes && physicsCadence && noFakeCooldown, "标量/模式原位 UndoRedo、阴影状态或物理扫描节奏说明不完整。");
			bool saveReload = _editor.SaveActiveResource();
			await WaitFrames(6);
			SlotComponentDefinition slotComponentDefinition = LoadDefinition("user://ModEditorSlotPositionPresenterProbe/SlotPositionPresenter.tres");
			saveReload &= GodotObject.IsInstanceValid(slotComponentDefinition) && slotComponentDefinition.posMarkPath.ToString() == "MarkerB" && slotComponentDefinition.occupantRefreshInterval == 7 && slotComponentDefinition.heightFollow && slotComponentDefinition.hideShadow && slotComponentDefinition.restoreGroundHeightOnRelease;
			bool inspectorUntouched = inspector?.CurrentObject == inspectorSentinel;
			_editor = XWEditorInterface.Instance?.GetResourceEditor("character_component_editor") as XWCharacterComponentVisualResourceEditor;
			presenter = _editor?.SlotPositionPresenter;
			_editor?.SetWorkbenchPage(2);
			await WaitFrames(3);
			bool flag8 = (presenter?.IsProcessIdle ?? false) && !presenter.OverlayVisible && !presenter.OverlayInputActive;
			Require(saveReload && inspectorUntouched && flag8, "保存重载、Inspector 隔离或隐藏页静默失败。");
			GD.Print($"[MOD_EDITOR_SLOT_POSITION_PRESENTER_PROBE] f3={f3} route={route} allFields={allFields} builtIns15={builtIns15} padPotDifference={padPotDifference} markerValidation={markerValidation} markerPin={markerPin} transformedDrag={transformedDrag} layerUi={layerUi} realCellOccupants={realCellOccupants} heightLine={heightLine} shadowModes={shadowModes} physicsCadence={physicsCadence} noFakeCooldown={noFakeCooldown} scalarNoRebuild={scalarNoRebuild} undoRedo={undoRedo} saveReload={saveReload} noDuplicates={noDuplicates} inspectorUntouched={inspectorUntouched} hiddenIdle={flag8} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private (SlotComponent Runtime, bool Layers, bool Height) CreateAndRunRealCellHarness()
	{
		SlotPositionProbeCharacter slotPositionProbeCharacter = new SlotPositionProbeCharacter
		{
			Name = "SlotHarnessOwner",
			Position = new Vector2(80f, 130f),
			Rotation = 0.22f,
			gridPos = Vector2I.Zero,
			ProcessMode = ProcessModeEnum.Disabled
		};
		Marker2D marker2D = new Marker2D
		{
			Name = "TransformPoint",
			Scale = new Vector2(-1.3f, 0.75f)
		};
		Marker2D marker2D2 = new Marker2D
		{
			Name = "MarkerA",
			Position = new Vector2(0f, -44f)
		};
		marker2D.AddChild(marker2D2, forceReadableName: false, InternalMode.Disabled);
		slotPositionProbeCharacter.AddChild(marker2D, forceReadableName: false, InternalMode.Disabled);
		slotPositionProbeCharacter.transformPoint = marker2D;
		ComponentManager componentManager = (slotPositionProbeCharacter.componentManager = new ComponentManager
		{
			Name = "SlotHarnessManager"
		});
		componentManager.AttachOwner(slotPositionProbeCharacter);
		AddChild(slotPositionProbeCharacter, forceReadableName: false, InternalMode.Disabled);
		SlotPositionProbeCharacter slotPositionProbeCharacter2 = new SlotPositionProbeCharacter
		{
			Name = "SlotOccupant",
			groundHeight = 11.0,
			ProcessMode = ProcessModeEnum.Disabled
		};
		SlotPositionProbeCharacter slotPositionProbeCharacter3 = new SlotPositionProbeCharacter
		{
			Name = "SurroundOccupant",
			groundHeight = 22.0,
			ProcessMode = ProcessModeEnum.Disabled
		};
		AddChild(slotPositionProbeCharacter2, forceReadableName: false, InternalMode.Disabled);
		AddChild(slotPositionProbeCharacter3, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
		{
			gridPos = Vector2I.Zero
		};
		towerDefenseCellInstance.characterSlotDictionary[slotPositionProbeCharacter] = slotPositionProbeCharacter2;
		towerDefenseCellInstance.characterSurround = slotPositionProbeCharacter3;
		SlotComponentDefinition definition = new SlotComponentDefinition
		{
			ComponentTypeId = "SlotComponent",
			DefinitionId = "probe.slot.physics",
			InstanceId = "probe.slot.physics.instance",
			posMarkPath = new NodePath("TransformPoint/MarkerA"),
			heightFollow = false,
			hideShadow = true,
			occupantRefreshInterval = 1,
			restoreGroundHeightOnRelease = true
		};
		if (!(componentManager.AddRuntimeComponent(definition) is SlotComponent slotComponent))
		{
			return (Runtime: null, Layers: false, Height: false);
		}
		slotComponent.cell = towerDefenseCellInstance;
		slotComponent.PhysicsProcess(1.0 / 60.0, 1uL);
		double num = (slotPositionProbeCharacter.GlobalPosition.Y - marker2D2.GlobalPosition.Y) / slotPositionProbeCharacter.transformPoint.GlobalScale.Y;
		bool item = slotComponent.slotCharacter == slotPositionProbeCharacter2 && slotComponent.surroundCharacter == slotPositionProbeCharacter3;
		bool flag = Mathf.IsEqualApprox((float)slotPositionProbeCharacter2.groundHeight, (float)num) && Mathf.IsEqualApprox((float)slotPositionProbeCharacter3.groundHeight, (float)num);
		slotComponent.SetAlive(alive: false);
		flag &= Mathf.IsEqualApprox((float)slotPositionProbeCharacter2.groundHeight, 11f) && Mathf.IsEqualApprox((float)slotPositionProbeCharacter3.groundHeight, 22f);
		slotComponent.SetAlive(alive: true);
		slotComponent.cell = towerDefenseCellInstance;
		slotComponent.PhysicsProcess(1.0 / 60.0, 2uL);
		return (Runtime: slotComponent, Layers: item, Height: flag);
	}

	private static SlotComponentDefinition LoadDefinition(string path)
	{
		return ResourceLoader.Load<SlotComponentDefinition>(path, "", ResourceLoader.CacheMode.Ignore);
	}

	private static void DragOverlay(XWSlotPositionOverlay overlay, Vector2 fromLocal, Vector2 toLocal)
	{
		Viewport viewport = overlay.GetViewport();
		Vector2 vector = overlay.GetGlobalTransformWithCanvas() * fromLocal;
		Vector2 vector2 = overlay.GetGlobalTransformWithCanvas() * toLocal;
		viewport.PushInput(new InputEventMouseButton
		{
			Position = vector,
			GlobalPosition = vector,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		}, inLocalCoords: true);
		viewport.PushInput(new InputEventMouseMotion
		{
			Position = vector2,
			GlobalPosition = vector2,
			Relative = vector2 - vector,
			ButtonMask = MouseButtonMask.Left
		}, inLocalCoords: true);
		viewport.PushInput(new InputEventMouseButton
		{
			Position = vector2,
			GlobalPosition = vector2,
			ButtonIndex = MouseButton.Left,
			Pressed = false
		}, inLocalCoords: true);
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

	private async Task<bool> WaitForComponentEditor(SlotComponentDefinition definition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			_editor = XWEditorInterface.Instance?.GetResourceEditor("character_component_editor") as XWCharacterComponentVisualResourceEditor;
			if (GodotObject.IsInstanceValid(_editor) && _editor.SelectedDefinition == definition && GodotObject.IsInstanceValid(_editor.SlotPositionPresenter?.Root))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static int CountBaseDirectFields(Node root)
	{
		return BaseDirectFields.Count((string property) => root.FindChildren("*", "", recursive: true, owned: false).OfType<Control>().Any((Control control) => control.TooltipText == property || control.TooltipText.StartsWith(property + " ·", StringComparison.Ordinal)));
	}

	private static string MissingBaseDirectFields(Node root)
	{
		return string.Join(",", BaseDirectFields.Where((string property) => !root.FindChildren("*", "", recursive: true, owned: false).OfType<Control>().Any((Control control) => control.TooltipText == property || control.TooltipText.StartsWith(property + " ·", StringComparison.Ordinal))));
	}

	private static int CountGenericSpecializedControls(Node root, Control presenterRoot)
	{
		int num = 0;
		foreach (string specializedProperty in XWSlotPositionPresenter.SpecializedProperties)
		{
			foreach (Control item in root.FindChildren("*", "", recursive: true, owned: false).OfType<Control>())
			{
				if (!presenterRoot.IsAncestorOf(item) && item.TooltipText.StartsWith(specializedProperty + " ·", StringComparison.Ordinal))
				{
					num++;
				}
			}
		}
		return num;
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
			GD.PrintErr("[MOD_EDITOR_SLOT_POSITION_PRESENTER_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_SLOT_POSITION_PRESENTER_FAILURE] " + failure);
		}
		string path = ProjectSettings.GlobalizePath("user://ModEditorSlotPositionPresenterProbe");
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
