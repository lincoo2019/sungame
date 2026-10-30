using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorProductionDropPresenterRuntimeProbe : Node
{
	private const string ResourcePath = "user://ModEditorProductionDropPresenterProbe/ProductionDropPresenter.tres";

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
			ProduceComponentDefinition produceComponentDefinition = LoadDefinition("res://Script/Component/TowerDefense/Character/ProduceComponent/Definitions/HotDogProduceComponentDefinition.tres");
			ProduceComponentDefinition produceComponentDefinition2 = LoadDefinition("res://Script/Component/TowerDefense/Character/ProduceComponent/Definitions/TwinSunShroomProduceComponentDefinition.tres");
			ProduceComponentDefinition produceComponentDefinition3 = LoadDefinition("res://Script/Component/TowerDefense/Character/ProduceComponent/Definitions/MarigoldProduceComponentDefinition.tres");
			ProduceComponentDefinition produceComponentDefinition4 = LoadDefinition("res://Script/Component/TowerDefense/Character/ProduceComponent/Definitions/NutFlowerProduceComponentDefinition.tres");
			bool builtInSemantics = produceComponentDefinition != null && produceComponentDefinition.num == 100 && produceComponentDefinition.sunOnceMax == 25 && produceComponentDefinition.markerPaths.Count == 1 && produceComponentDefinition2 != null && produceComponentDefinition2.markerPaths.Count == 2 && produceComponentDefinition2.num == 25 && produceComponentDefinition3?.produceType == "Coin" && produceComponentDefinition3.coinRandom && produceComponentDefinition4?.produceType == "Packet" && produceComponentDefinition4.packetName.Count == 1 && produceComponentDefinition4.packetName[0] == "PlantNutSeed";
			Require(builtInSemantics, "四个内置生产资源的真实契约未加载或发生漂移。");
			string path = ProjectSettings.GlobalizePath("user://ModEditorProductionDropPresenterProbe");
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
			Directory.CreateDirectory(path);
			ProduceComponentDefinition definition = new ProduceComponentDefinition
			{
				ResourceName = "ProductionDropProbe",
				ComponentTypeId = "ProduceComponent",
				DefinitionId = "probe.production.drop",
				InstanceId = "probe.production.drop.instance",
				produceType = "Sun",
				num = 100,
				sunOnceMax = 25,
				produceInterval = 25f,
				glowLeadTime = 1.5f,
				maxCatchUpProductions = 2,
				markerPaths = new Array<NodePath>
				{
					new NodePath("MarkerA")
				},
				produceGlowTargetPath = new NodePath()
			};
			Require(ResourceSaver.Save(definition, "user://ModEditorProductionDropPresenterProbe/ProductionDropPresenter.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "Production 探针资源保存失败。");
			definition = ResourceLoader.Load<ProduceComponentDefinition>("user://ModEditorProductionDropPresenterProbe/ProductionDropPresenter.tres", "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(definition), "Production 探针资源无法重载。");
			Node inspectorSentinel = new Node
			{
				Name = "ProductionInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			inspector?.EditObject(inspectorSentinel);
			await WaitFrames(2);
			XWVisualEditorDescriptor descriptor;
			bool route = XWResourceEditorRegistry.TryGetEditor(definition, "user://ModEditorProductionDropPresenterProbe/ProductionDropPresenter.tres", out descriptor) && descriptor.Category == "CharacterComponent" && descriptor.DockKey == "character_component_editor";
			Require(route, "Produce 定义未路由到角色组件编辑器。");
			XWEditorInterface.Instance.EditResource(definition, XWResourceEditContext.ForRoot(definition, "user://ModEditorProductionDropPresenterProbe/ProductionDropPresenter.tres", "character_component_editor"));
			XWEditorInterface.Instance.FocusPanel("character_component_editor");
			bool flag = await WaitForComponentEditor(definition, 900);
			Require(flag, "角色组件编辑器未挂载 ProductionDropPresenter。");
			if (!flag)
			{
				Finish();
				return;
			}
			_editor.SetWorkbenchPage(0);
			await WaitFrames(3);
			XWProductionDropPresenter presenter = _editor.ProductionDropPresenter;
			PanelContainer root = Find<PanelContainer>(_editor, "ProductionDropPresenter");
			Node2D node2D = Find<Node2D>(_editor, "PreviewWorld");
			XWProductionDropOverlay overlay = Find<XWProductionDropOverlay>(_editor, "ProductionDropPreviewOverlay");
			bool gamePosition = GodotObject.IsInstanceValid(presenter?.Root) && root == presenter.Root && GodotObject.IsInstanceValid(node2D) && overlay?.GetParent() == node2D && IsDescendantOf(root, Find<Node>(_editor, "ComponentPropertyHost")) && _editor.IsPreviewViewportRendering && presenter.OverlayVisible && presenter.OverlayInputActive;
			Require(gamePosition, "Production 游戏位置面板或 overlay 未正确挂载。");
			bool typeCards = new string[6] { "Sun", "BrainSun", "JalaSun", "Coin", "Packet", "QXSun" }.All((string type) => GodotObject.IsInstanceValid(Find<Button>(_editor, "ProductionType_" + type))) && GodotObject.IsInstanceValid(Find<Control>(_editor, "ProductionHealthRuler")) && GodotObject.IsInstanceValid(Find<Control>(_editor, "ProductionTimingTimeline")) && XWProductionDropPresenter.SpecializedProperties.Count == 17;
			Require(typeCards, "六种产物图卡、时间轴、血量标尺或 17 个专用字段不完整。");
			ProductionDropOverridePreviewOwner owner = new ProductionDropOverridePreviewOwner
			{
				Name = "ProductionPreviewOwner",
				Position = new Vector2(130f, 90f),
				Rotation = 0.17f,
				Scale = new Vector2(1.2f, 0.85f),
				ProcessMode = ProcessModeEnum.Disabled
			};
			Marker2D node = new Marker2D
			{
				Name = "MarkerA",
				Position = new Vector2(-26f, -45f)
			};
			Marker2D markerB = new Marker2D
			{
				Name = "MarkerB",
				Position = new Vector2(35f, -38f)
			};
			Node2D node2 = new Node2D
			{
				Name = "GlowWrong"
			};
			owner.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			owner.AddChild(markerB, forceReadableName: false, InternalMode.Disabled);
			owner.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(owner, forceReadableName: false, InternalMode.Disabled);
			ProduceComponent runtime = new ProduceComponent
			{
				produceType = "Sun",
				num = 100,
				produceInterval = 25f,
				_IZMMode = false
			};
			presenter.BindPreviewRuntime(owner, runtime);
			await WaitFrames(3);
			Label markerStatus = Find<Label>(_editor, "ProductionMarkerStatus_0");
			Label glowStatus = Find<Label>(_editor, "ProductionGlowWarning");
			definition.produceGlowTargetPath = new NodePath("GlowWrong");
			_editor.RefreshProductionDropPresenterFromHistory();
			await WaitFrames(2);
			bool markerGlowPaths = (markerStatus?.Text.Contains("有效", StringComparison.Ordinal) ?? false) && (glowStatus?.Text.Contains("类型错误", StringComparison.Ordinal) ?? false);
			bool markerPins = presenter.MarkerPinCount == 1 && overlay.MarkerPinCount == 1;
			Require(markerGlowPaths && markerPins, "Marker/Glow 路径验证或游戏位置 pin 未工作。");
			Label label = Find<Label>(_editor, "ProductionSunChunkSummary");
			bool hotDogChunks = label != null && label.Text.Contains("每点拆成 4 枚", StringComparison.Ordinal) && label.Text.Contains("共 4 枚", StringComparison.Ordinal);
			Require(hotDogChunks, "HotDog 100/25 未预览为每点 4 个分块。");
			SpinBox amount = Find<SpinBox>(_editor, "ProductionAmountValue");
			ulong rootId = presenter.Root.GetInstanceId();
			ulong amountId = amount?.GetInstanceId() ?? 0;
			int fullBefore = _editor.FullSurfaceRefreshCount;
			int runtimeBuildBefore = _editor.RuntimePreviewBuildCount;
			int previewBefore = presenter.PreviewRevision;
			Stopwatch watch = Stopwatch.StartNew();
			if (GodotObject.IsInstanceValid(amount))
			{
				amount.EmitSignal(Control.SignalName.FocusEntered);
				double[] array = new double[5] { 105.0, 110.0, 115.0, 120.0, 125.0 };
				for (int num = 0; num < array.Length; num++)
				{
					double num2 = (amount.Value = array[num]);
					amount.EmitSignal(Godot.Range.SignalName.ValueChanged, num2);
				}
			}
			watch.Stop();
			await WaitFrames(2);
			int num4;
			if (presenter.Root.GetInstanceId() == rootId)
			{
				SpinBox spinBox = Find<SpinBox>(_editor, "ProductionAmountValue");
				if (spinBox != null && spinBox.GetInstanceId() == amountId && _editor.FullSurfaceRefreshCount == fullBefore && _editor.RuntimePreviewBuildCount == runtimeBuildBefore)
				{
					num4 = ((presenter.PreviewRevision > previewBefore) ? 1 : 0);
					goto IL_0dd1;
				}
			}
			num4 = 0;
			goto IL_0dd1;
			IL_0dd1:
			bool scalarNoRebuild = (byte)num4 != 0;
			bool previewNonBlocking = scalarNoRebuild && watch.ElapsedMilliseconds < 1000;
			amount?.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(2);
			_history.ClearHistory();
			amount = Find<SpinBox>(_editor, "ProductionAmountValue");
			amount?.EmitSignal(Control.SignalName.FocusEntered);
			if (GodotObject.IsInstanceValid(amount))
			{
				amount.Value = 140.0;
				amount.EmitSignal(Godot.Range.SignalName.ValueChanged, 140.0);
				amount.EmitSignal(Control.SignalName.FocusExited);
			}
			await WaitFrames(2);
			bool applied = definition.num == 140 && _history.HasUndo();
			bool undone = _history.Undo();
			await WaitFrames(2);
			undone &= definition.num == 125 && presenter.Root.GetInstanceId() == rootId && _editor.RuntimePreviewBuildCount == runtimeBuildBefore;
			bool redone = _history.Redo();
			await WaitFrames(2);
			bool num5 = redone;
			int num6;
			if (definition.num == 140)
			{
				SpinBox spinBox2 = Find<SpinBox>(_editor, "ProductionAmountValue");
				num6 = ((spinBox2 != null && spinBox2.Value == 140.0) ? 1 : 0);
			}
			else
			{
				num6 = 0;
			}
			redone = (byte)((num5 ? 1u : 0u) & (uint)num6) != 0;
			bool undoRedo = applied && undone && redone;
			Require(scalarNoRebuild && previewNonBlocking && undoRedo, "普通标量未原位刷新/UndoRedo，或重建了 runtime。");
			_history.ClearHistory();
			Find<Button>(_editor, "ProductionType_Coin")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			Control coinPanel = Find<Control>(_editor, "ProductionCoinPanel");
			Label totalSummary = Find<Label>(_editor, "ProductionTotalSummary");
			Label numWarning = Find<Label>(_editor, "ProductionNumWarning");
			bool coinProbability = definition.produceType == "Coin" && (coinPanel?.Visible ?? false) && (totalSummary?.Text.Contains("每点生成 1 枚随机金币", StringComparison.Ordinal) ?? false) && totalSummary.Text.Contains("概率池", StringComparison.Ordinal) && (numWarning?.Text.Contains("忽略 num", StringComparison.Ordinal) ?? false);
			bool typeUndone = _history.Undo();
			await WaitFrames(2);
			typeUndone &= definition.produceType == "Sun" && (Find<Control>(_editor, "ProductionSunPanel")?.Visible ?? false) && presenter.Root.GetInstanceId() == rootId;
			bool typeRedone = _history.Redo();
			await WaitFrames(2);
			typeRedone &= definition.produceType == "Coin" && coinPanel.Visible;
			_history.ClearHistory();
			Find<Button>(_editor, "ProductionMode_emit-only")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			bool onlyEmit = definition.onlyEmit && (Find<Control>(_editor, "ProductionEmitOnlyNotice")?.Visible ?? false) && !coinPanel.Visible && totalSummary.Text.Contains("事件总量", StringComparison.Ordinal);
			bool emitUndone = _history.Undo();
			await WaitFrames(2);
			emitUndone &= !definition.onlyEmit && coinPanel.Visible;
			bool typeModeUndo = typeUndone && typeRedone && emitUndone && presenter.Root.GetInstanceId() == rootId && _editor.RuntimePreviewBuildCount == runtimeBuildBefore;
			bool conditionalFields = onlyEmit && coinPanel.Visible;
			Require(coinProbability && typeModeUndo && conditionalFields, "Coin/onlyEmit 条件字段、摘要或原位 Undo 未同步。");
			_history.ClearHistory();
			Find<Button>(_editor, "ProductionType_Sun")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			amount = Find<SpinBox>(_editor, "ProductionAmountValue");
			amount?.EmitSignal(Control.SignalName.FocusEntered);
			if (GodotObject.IsInstanceValid(amount))
			{
				amount.Value = 25.0;
				amount.EmitSignal(Godot.Range.SignalName.ValueChanged, 25.0);
				amount.EmitSignal(Control.SignalName.FocusExited);
			}
			Find<Button>(_editor, "ProductionAddMarkerButton")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			LineEdit lineEdit = Find<LineEdit>(_editor, "ProductionMarkerPath_1");
			lineEdit?.EmitSignal(Control.SignalName.FocusEntered);
			if (GodotObject.IsInstanceValid(lineEdit))
			{
				lineEdit.Text = "MarkerB";
				lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, "MarkerB");
				lineEdit.EmitSignal(Control.SignalName.FocusExited);
			}
			await WaitFrames(3);
			bool twinTotal = definition.markerPaths.Count == 2 && presenter.MarkerPinCount == 2 && totalSummary.Text.Contains("2 个有效生产位置", StringComparison.Ordinal) && totalSummary.Text.Contains("50", StringComparison.Ordinal);
			Require(twinTotal, "Twin 双 marker 每轮总量未显示 25×2=50。");
			overlay = Find<XWProductionDropOverlay>(_editor, "ProductionDropPreviewOverlay");
			overlay.QueueRedraw();
			await WaitFrames(2);
			_history.ClearHistory();
			Vector2 markerHandlePosition = presenter.GetMarkerHandlePosition(0);
			Vector2 toLocal = overlay.ToLocal(markerB.GlobalPosition);
			DragOverlay(overlay, markerHandlePosition, toLocal);
			await WaitFrames(3);
			bool dragApplied = definition.markerPaths[0].ToString() == "MarkerB" && _history.HasUndo();
			bool dragUndone = _history.Undo();
			await WaitFrames(2);
			dragUndone &= definition.markerPaths[0].ToString() == "MarkerA";
			bool dragRedone = _history.Redo();
			await WaitFrames(2);
			dragRedone &= definition.markerPaths[0].ToString() == "MarkerB";
			bool directDrag = dragApplied && dragUndone && dragRedone && presenter.Root.GetInstanceId() == rootId && _editor.RuntimePreviewBuildCount == runtimeBuildBefore;
			Require(directDrag, "变换 Canvas 下 PushInput marker 拖拽未完成 Undo/Redo，或重建了 runtime。");
			_history.ClearHistory();
			Find<Button>(_editor, "ProductionType_Packet")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			Find<Button>(_editor, "ProductionAddPacketButton")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			LineEdit lineEdit2 = Find<LineEdit>(_editor, "ProductionPacketName_0");
			lineEdit2?.EmitSignal(Control.SignalName.FocusEntered);
			if (GodotObject.IsInstanceValid(lineEdit2))
			{
				lineEdit2.Text = "PlantNutSeed";
				lineEdit2.EmitSignal(LineEdit.SignalName.TextChanged, "PlantNutSeed");
				lineEdit2.EmitSignal(Control.SignalName.FocusExited);
			}
			await WaitFrames(2);
			bool packetList = definition.packetName.Count == 1 && definition.packetName[0] == "PlantNutSeed" && presenter.PacketRowCount == 1 && totalSummary.Text.Contains("每点最多 1 张卡牌", StringComparison.Ordinal) && totalSummary.Text.Contains("最多 2 张", StringComparison.Ordinal) && numWarning.Text.Contains("忽略 num", StringComparison.Ordinal);
			Require(packetList, "NutFlower Packet 列表或 num 无效摘要错误。");
			definition.produceType = "HostOnlyMystery";
			_editor.RefreshProductionDropPresenterFromHistory();
			await WaitFrames(2);
			Label label2 = Find<Label>(_editor, "ProductionUnknownTypeWarning");
			bool flag2 = new string[6] { "Sun", "BrainSun", "JalaSun", "Coin", "Packet", "QXSun" }.All(delegate(string type)
			{
				Button button = Find<Button>(_editor, "ProductionType_" + type);
				return button != null && !button.ButtonPressed;
			});
			bool unknownType = label2 != null && label2.Text.Contains("HostOnlyMystery", StringComparison.Ordinal) && label2.Text.Contains("不会生成实体", StringComparison.Ordinal) && flag2;
			Require(unknownType, "未知 produceType 被静默归一或仍选中了六类型图卡。");
			definition.produceType = "Sun";
			_editor.RefreshProductionDropPresenterFromHistory();
			presenter.BindPreviewRuntime(owner, runtime);
			await WaitFrames(2);
			Label label3 = Find<Label>(_editor, "ProductionRuntimeDifference");
			bool runtimeOverride = label3 != null && label3.Text.Contains("角色脚本最终覆盖/预期运行值 produceType：定义 Sun → 角色 QXSun", StringComparison.Ordinal) && label3.Text.Contains("角色 sunNum 75", StringComparison.Ordinal) && label3.Text.Contains("interval", StringComparison.Ordinal) && label3.Text.Contains("角色 18", StringComparison.Ordinal);
			Require(runtimeOverride, "SunFlowerQX 风格的 runtime produceType/num/IZM override 未明确显示。");
			bool noDuplicates = CountGenericSpecializedControls(_editor, root) == 0;
			Require(noDuplicates, "Production 专用字段仍被通用表单重复渲染。");
			bool saveReload = _editor.SaveActiveResource();
			await WaitFrames(6);
			bool oldPresenterReleased = presenter.Root == null;
			ProduceComponentDefinition produceComponentDefinition5 = ResourceLoader.Load<ProduceComponentDefinition>("user://ModEditorProductionDropPresenterProbe/ProductionDropPresenter.tres", "", ResourceLoader.CacheMode.Ignore);
			saveReload &= GodotObject.IsInstanceValid(produceComponentDefinition5) && produceComponentDefinition5.produceType == "Sun" && produceComponentDefinition5.markerPaths.Count == 2 && produceComponentDefinition5.markerPaths[0].ToString() == "MarkerB" && produceComponentDefinition5.markerPaths[1].ToString() == "MarkerB" && produceComponentDefinition5.packetName.Count == 1 && produceComponentDefinition5.packetName[0] == "PlantNutSeed";
			Require(saveReload && oldPresenterReleased, "Production 保存/忽略缓存重载失败，或旧 presenter 未释放。");
			PanelContainer panelContainer = _editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
			bool inspectorUntouched = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null && inspector?.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "Production presenter 暴露或替换了 Inspector。");
			presenter = _editor.ProductionDropPresenter;
			XWEditorInterface.Instance.FocusPanel("bp_editor");
			await WaitFrames(4);
			bool flag3 = GodotObject.IsInstanceValid(presenter?.Root) && !_editor.IsVisibleInTree() && _editor.ProcessMode == ProcessModeEnum.Disabled && presenter.IsProcessIdle;
			Require(flag3, "隐藏后的 Production presenter 仍在处理输入或帧。");
			GD.Print($"[MOD_EDITOR_PRODUCTION_DROP_PRESENTER_PROBE] f3={f3} route={route} builtInSemantics={builtInSemantics} gamePosition={gamePosition} typeCards={typeCards} onlyEmit={onlyEmit} conditionalFields={conditionalFields} markerGlowPaths={markerGlowPaths} markerPins={markerPins} directDrag={directDrag} hotDogChunks={hotDogChunks} twinTotal={twinTotal} coinProbability={coinProbability} packetList={packetList} unknownType={unknownType} runtimeOverride={runtimeOverride} scalarNoRebuild={scalarNoRebuild} undoRedo={undoRedo} typeModeUndo={typeModeUndo} saveReload={saveReload} oldPresenterReleased={oldPresenterReleased} noDuplicates={noDuplicates} inspectorUntouched={inspectorUntouched} hiddenIdle={flag3} previewNonBlocking={previewNonBlocking} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static ProduceComponentDefinition LoadDefinition(string path)
	{
		return ResourceLoader.Load<ProduceComponentDefinition>(path, "", ResourceLoader.CacheMode.Ignore);
	}

	private static void DragOverlay(XWProductionDropOverlay overlay, Vector2 fromLocal, Vector2 toLocal)
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
			ButtonMask = (MouseButtonMask)0L,
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

	private async Task<bool> WaitForComponentEditor(ProduceComponentDefinition definition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			_editor = XWEditorInterface.Instance?.GetResourceEditor("character_component_editor") as XWCharacterComponentVisualResourceEditor;
			if (GodotObject.IsInstanceValid(_editor) && _editor.SelectedDefinition == definition && GodotObject.IsInstanceValid(_editor.ProductionDropPresenter?.Root))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static int CountGenericSpecializedControls(Node root, Control presenterRoot)
	{
		int num = 0;
		foreach (string specializedProperty in XWProductionDropPresenter.SpecializedProperties)
		{
			foreach (Node item in root.FindChildren("*", "", recursive: true, owned: false))
			{
				if (item is Control control && !presenterRoot.IsAncestorOf(control) && (control.TooltipText == specializedProperty || control.TooltipText.StartsWith(specializedProperty + " ·", StringComparison.Ordinal) || item.Name == (StringName)("EnumSource_" + specializedProperty)))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static bool IsDescendantOf(Node child, Node expectedAncestor)
	{
		if (GodotObject.IsInstanceValid(child) && GodotObject.IsInstanceValid(expectedAncestor))
		{
			return expectedAncestor.IsAncestorOf(child);
		}
		return false;
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
			GD.PrintErr("[MOD_EDITOR_PRODUCTION_DROP_PRESENTER_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_PRODUCTION_DROP_PRESENTER_FAILURE] " + failure);
		}
		string path = ProjectSettings.GlobalizePath("user://ModEditorProductionDropPresenterProbe");
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
