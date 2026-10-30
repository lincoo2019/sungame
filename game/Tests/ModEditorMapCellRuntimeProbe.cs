using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorMapCellRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWMapCellVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 17;
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
			bool flag = await WaitForEditor(900);
			Require(flag, "F3 did not initialize the MapCell editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool flag2 = await EnterEditorSurface(900);
			Require(flag2, "F3 editor did not finish loading its main editing surface.");
			if (!flag2)
			{
				Finish();
				return;
			}
			var (entryCreated, cell, savePath, entryRoot) = CreateThroughEntry();
			Require(entryCreated, "Resources/MapCells creation entry did not produce a typed TowerDefenseCellConfig.");
			if (!entryCreated)
			{
				Finish();
				return;
			}
			cell.ResourceName = "ProbeMapCell";
			cell.pos = new Vector4I(1, 1, 1, 1);
			cell.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
			{
				TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
				TowerDefenseEnum.PLANTGRIDTYPE.AIR
			};
			cell.ElementFlags = 0;
			Error error = ResourceSaver.Save(cell, savePath, ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"Could not create MapCell save probe resource: {error}.");
			cell.TakeOverPath(savePath);
			XWVisualEditorDescriptor descriptor;
			bool descriptorRoute = XWResourceEditorRegistry.TryGetEditor(cell, "res://Mods/Probe/Resources/MapCells/Cell.tres", out descriptor) && descriptor?.Category == "MapCell" && descriptor.DockKey == "map_cell_editor";
			XWEditorInterface.Instance.EditResource(cell);
			await WaitFrames(7);
			Window window = FindAncestorWindow(_editor);
			bool window2 = GodotObject.IsInstanceValid(window);
			if (window2)
			{
				window.MinSize = Vector2I.Zero;
				window.Size = new Vector2I(820, 720);
				await WaitFrames(8);
			}
			bool route = descriptorRoute && XWEditorInterface.Instance.GetResourceEditor("map_cell_editor") == _editor;
			Require(window2, "MapCell editor is not mounted under the F3 ModEditor window.");
			Require(route, "TowerDefenseCellConfig did not route to map_cell_editor.");
			Control layout = FindControl<Control>("MapCellVisualEditorLayout");
			TabContainer pages = _editor.MapCellPages;
			bool pagesReachable = GodotObject.IsInstanceValid(pages) && pages.GetTabCount() == 3;
			if (pagesReachable)
			{
				for (int page = 0; page < pages.GetTabCount(); page++)
				{
					_editor.SetMapCellPage(page);
					await WaitFrames(2);
					pagesReachable &= pages.GetTabControl(page).IsVisibleInTree();
				}
			}
			bool responsive = GodotObject.IsInstanceValid(layout) && layout.CustomMinimumSize.X <= 420f && layout.GetCombinedMinimumSize().X <= 500f && layout.Size.X <= 820f && pagesReachable;
			Require(responsive, $"MapCell pages did not remain reachable in the 820px editor: layout={layout?.Size.X ?? (-1f)}; minimum={layout?.GetCombinedMinimumSize().X ?? (-1f)}; tabs={pages?.GetTabCount() ?? (-1)}.");
			PanelContainer panelContainer = FindControl<PanelContainer>("InspectorPanel");
			VBoxContainer vBoxContainer = FindControl<VBoxContainer>("EmbeddedInspectorHost");
			bool inspectorHidden = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
			Require(inspectorHidden, "MapCell still materializes the raw InspectorPanel.");
			GridContainer gridContainer = FindControl<GridContainer>("BoardGrid");
			SpinBox endX = FindControl<SpinBox>("EndX");
			Button instance = FindControl<Button>("GridType_GROUND");
			Button water = FindControl<Button>("GridType_WATER");
			Button fire = FindControl<Button>($"ElementFlag_{2}");
			Button ridge = FindControl<Button>("RidgeCurve");
			Node instance2 = _editor.FindChild("GroundCurvePicker", recursive: true, owned: false);
			bool visualBoard = GodotObject.IsInstanceValid(gridContainer) && gridContainer.GetChildCount() == 54 && GodotObject.IsInstanceValid(endX) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(water) && GodotObject.IsInstanceValid(fire) && GodotObject.IsInstanceValid(ridge) && GodotObject.IsInstanceValid(instance2);
			Require(visualBoard, "MapCell game board or direct property controls are incomplete.");
			HFlowContainer hFlowContainer = FindControl<HFlowContainer>("GridTypeButtons");
			HFlowContainer hFlowContainer2 = FindControl<HFlowContainer>("ElementButtons");
			bool flag3 = _editor.GridVisualCardCount == 14 && _editor.ElementVisualCardCount == 4 && GodotObject.IsInstanceValid(hFlowContainer) && hFlowContainer.GetChildCount() == 14 && GodotObject.IsInstanceValid(hFlowContainer2) && hFlowContainer2.GetChildCount() == 4;
			bool flag4 = flag3 && hFlowContainer.GetChild(0) is XWGameVisualChoiceCard && hFlowContainer2.GetChild(0) is XWGameVisualChoiceCard;
			bool flag5 = flag4 && GodotObject.IsInstanceValid((hFlowContainer.GetChild(0) as XWGameVisualChoiceCard)?.Icon) && GodotObject.IsInstanceValid((hFlowContainer2.GetChild(0) as XWGameVisualChoiceCard)?.Icon);
			bool flag6 = hFlowContainer.FindChildren("*", "OptionButton", recursive: true, owned: false).Count == 0 && hFlowContainer2.FindChildren("*", "OptionButton", recursive: true, owned: false).Count == 0;
			bool visualCards = flag3 && flag4 && flag5 && flag6;
			Require(visualCards, $"MapCell placement layers/elements are not scene-authored visual icon cards: counts={flag3} scenes={flag4} icons={flag5} noTextDropdowns={flag6}.");
			_editor.SetMapCellPage(1);
			await WaitFrames(2);
			bool metadataUndoRedo = await ProbeResourceName(cell);
			var (continuousMerged, positionUndoRedo) = await ProbeContinuousPosition(cell, endX);
			_editor.SetMapCellPage(0);
			await WaitFrames(2);
			bool boardSelection = await ProbeBoardSelection(cell);
			_editor.SetMapCellPage(1);
			await WaitFrames(2);
			bool gridTypeUndoRedo = await ProbeToggle(water, () => cell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER), "Water grid type");
			bool elementUndoRedo = await ProbeToggle(fire, () => (cell.ElementFlags & 2) != 0, "Fire element flag");
			_editor.SetMapCellPage(2);
			await WaitFrames(2);
			bool curveUndoRedo = await ProbeCurvePreset(cell, ridge);
			bool saveRoundTrip = await ProbeSaveRoundTrip(cell, savePath);
			bool value = await ProbeHiddenProcessing();
			PanelContainer panelContainer2 = FindControl<PanelContainer>("InspectorPanel");
			VBoxContainer vBoxContainer2 = FindControl<VBoxContainer>("EmbeddedInspectorHost");
			bool flag7 = GodotObject.IsInstanceValid(panelContainer2) && !panelContainer2.Visible && GodotObject.IsInstanceValid(vBoxContainer2) && vBoxContainer2.GetChildCount() == 0;
			Require(flag7, "MapCell editing touched the raw embedded Inspector.");
			bool flag8 = _editor.Visible && _editor.IsInsideTree() && FindAncestorWindow(_editor) != null;
			Require(flag8, "MapCell editor was detached after direct edits.");
			GD.Print($"[MOD_EDITOR_MAP_CELL_PROBE] window={window2} entryCreated={entryCreated} route={route} inspectorHidden={inspectorHidden} responsive={responsive} visualBoard={visualBoard} visualCards={visualCards} metadataUndoRedo={metadataUndoRedo} continuousMerged={continuousMerged} positionUndoRedo={positionUndoRedo} boardSelection={boardSelection} gridTypeUndoRedo={gridTypeUndoRedo} elementUndoRedo={elementUndoRedo} curveUndoRedo={curveUndoRedo} saveRoundTrip={saveRoundTrip} hiddenStopped={value} inspectorUntouched={flag7} editorMounted={flag8} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
			if (Directory.Exists(entryRoot))
			{
				Directory.Delete(entryRoot, recursive: true);
			}
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static (bool created, TowerDefenseCellConfig cell, string resourcePath, string rootPath) CreateThroughEntry()
	{
		string text = Guid.NewGuid().ToString("N");
		string text2 = "user://ModEditorMapCellEntryProbe_" + text;
		string text3 = ProjectSettings.GlobalizePath(text2);
		string text4 = Path.Combine(text3, "Resources", "MapCells");
		Directory.CreateDirectory(text4);
		bool flag = false;
		foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(text4, text3))
		{
			if (item.Id == "new-map-cell" && item.TemplateId == "map-cell-resource")
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return (created: false, cell: null, resourcePath: "", rootPath: text3);
		}
		XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-map-cell", text4, "ProbeCreatedMapCell");
		string text5 = text2 + "/Resources/MapCells/ProbeCreatedMapCell.tres";
		TowerDefenseCellConfig towerDefenseCellConfig = (templateCreateResult.Success ? ResourceLoader.Load<TowerDefenseCellConfig>(text5, "", ResourceLoader.CacheMode.Replace) : null);
		return (created: templateCreateResult.Success && GodotObject.IsInstanceValid(towerDefenseCellConfig), cell: towerDefenseCellConfig, resourcePath: text5, rootPath: text3);
	}

	private async Task<bool> ProbeResourceName(TowerDefenseCellConfig cell)
	{
		LineEdit lineEdit = FindControl<LineEdit>("ResourceNameEdit");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			Require(condition: false, "MapCell resource-name direct editor is unavailable.");
			return false;
		}
		_history.ClearHistory();
		string before = cell.ResourceName;
		SimulateTextSession(lineEdit, "ProbeMapCellEdited");
		await WaitFrames(3);
		bool applied = cell.ResourceName == "ProbeMapCellEdited" && _history.HasUndo();
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && cell.ResourceName == before;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && cell.ResourceName == "ProbeMapCellEdited";
		bool flag2 = applied && undone && flag;
		Require(flag2, "MapCell resource name did not round-trip through the main direct editor and shared UndoRedo.");
		return flag2;
	}

	private async Task<(bool continuousMerged, bool positionUndoRedo)> ProbeContinuousPosition(TowerDefenseCellConfig cell, SpinBox endX)
	{
		if (!GodotObject.IsInstanceValid(endX))
		{
			return (continuousMerged: false, positionUndoRedo: false);
		}
		_history.ClearHistory();
		int beforeVersion = _history.GetVersion();
		endX.EmitSignal(Control.SignalName.FocusEntered);
		double[] array = new double[3] { 2.0, 3.0, 4.0 };
		foreach (double num in array)
		{
			endX.SetValueNoSignal(num);
			endX.EmitSignal(Godot.Range.SignalName.ValueChanged, num);
		}
		endX.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool continuousMerged = cell.pos == new Vector4I(1, 1, 4, 1) && _history.GetVersion() - beforeVersion == 1 && _history.HasUndo();
		Require(continuousMerged, "Continuous position input did not merge into one history action.");
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && cell.pos == new Vector4I(1, 1, 1, 1);
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && cell.pos == new Vector4I(1, 1, 4, 1);
		bool flag2 = undone && flag;
		Require(flag2, "Position did not round-trip through shared UndoRedo.");
		return (continuousMerged: continuousMerged, positionUndoRedo: flag2);
	}

	private async Task<bool> ProbeBoardSelection(TowerDefenseCellConfig cell)
	{
		Button button = FindControl<Button>("MapCellGridButton_1_1");
		Button finish = FindControl<Button>("MapCellGridButton_4_3");
		if (!GodotObject.IsInstanceValid(button) || !GodotObject.IsInstanceValid(finish))
		{
			Require(condition: false, "Board selection buttons were not found.");
			return false;
		}
		_history.ClearHistory();
		Vector4I before = cell.pos;
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		bool anchorOnly = cell.pos == before && !_history.HasUndo();
		finish.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		Vector4I expected = new Vector4I(2, 2, 5, 4);
		bool applied = cell.pos == expected && _history.HasUndo();
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && cell.pos == before;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && cell.pos == expected;
		bool flag2 = anchorOnly && applied && undone && flag;
		Require(flag2, "Two-click visual board selection did not apply and round-trip one rectangle action.");
		return flag2;
	}

	private async Task<bool> ProbeToggle(Button button, Func<bool> isApplied, string label)
	{
		if (!GodotObject.IsInstanceValid(button))
		{
			return false;
		}
		_history.ClearHistory();
		bool before = isApplied();
		button.SetPressedNoSignal(!before);
		button.EmitSignal(BaseButton.SignalName.Toggled, !before);
		await WaitFrames(3);
		bool applied = isApplied() != before && _history.HasUndo();
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && isApplied() == before;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && isApplied() != before;
		bool flag2 = applied && undone && flag;
		Require(flag2, label + " did not round-trip through shared UndoRedo.");
		return flag2;
	}

	private async Task<bool> ProbeCurvePreset(TowerDefenseCellConfig cell, Button ridge)
	{
		if (!GodotObject.IsInstanceValid(ridge))
		{
			return false;
		}
		_history.ClearHistory();
		CurveTexture before = cell.groundHeightCurve;
		ridge.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool applied = GodotObject.IsInstanceValid(cell.groundHeightCurve) && GodotObject.IsInstanceValid(cell.groundHeightCurve.Curve) && cell.groundHeightCurve.Curve.PointCount == 3 && _history.HasUndo();
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && cell.groundHeightCurve == before;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && GodotObject.IsInstanceValid(cell.groundHeightCurve) && cell.groundHeightCurve.Curve.PointCount == 3;
		bool flag2 = applied && undone && flag;
		Require(flag2, "Visual ground-height curve preset did not round-trip through shared UndoRedo.");
		return flag2;
	}

	private async Task<bool> ProbeSaveRoundTrip(TowerDefenseCellConfig cell, string savePath)
	{
		Button button = FindControl<Button>("SaveButton");
		if (!GodotObject.IsInstanceValid(button))
		{
			return false;
		}
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(5);
		TowerDefenseCellConfig towerDefenseCellConfig = ResourceLoader.Load<TowerDefenseCellConfig>(savePath, "", ResourceLoader.CacheMode.Replace);
		bool flag = GodotObject.IsInstanceValid(towerDefenseCellConfig) && towerDefenseCellConfig.ResourceName == "ProbeMapCellEdited" && towerDefenseCellConfig.pos == cell.pos && towerDefenseCellConfig.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER) && (towerDefenseCellConfig.ElementFlags & 2) != 0 && GodotObject.IsInstanceValid(towerDefenseCellConfig.groundHeightCurve?.Curve) && towerDefenseCellConfig.groundHeightCurve.Curve.PointCount == 3;
		Require(flag, "Save button did not persist all MapCell direct-edit properties.");
		return flag;
	}

	private async Task<bool> ProbeHiddenProcessing()
	{
		_editor.SetMapCellPage(0);
		await WaitFrames(2);
		bool boardActive = _editor.IsBoardSurfaceActive && !_editor.IsCurveSurfaceActive;
		_editor.SetMapCellPage(2);
		await WaitFrames(2);
		bool curveActive = !_editor.IsBoardSurfaceActive && _editor.IsCurveSurfaceActive;
		_editor.SetMapCellPage(1);
		await WaitFrames(2);
		bool directStopped = !_editor.IsBoardSurfaceActive && !_editor.IsCurveSurfaceActive;
		_editor.Hide();
		await WaitFrames(2);
		bool hiddenStopped = !_editor.IsBoardSurfaceActive && !_editor.IsCurveSurfaceActive;
		_editor.Show();
		XWEditorInterface.Instance.FocusPanel("map_cell_editor");
		_editor.SetMapCellPage(0);
		await WaitFrames(3);
		bool flag = _editor.IsBoardSurfaceActive && !_editor.IsCurveSurfaceActive;
		bool flag2 = boardActive && curveActive && directStopped && hiddenStopped && flag;
		Require(flag2, "MapCell board/curve processing did not stop off-page or while hidden.");
		return flag2;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetResourceEditor("map_cell_editor") is XWMapCellVisualResourceEditor xWMapCellVisualResourceEditor && GodotObject.IsInstanceValid(xWMapCellVisualResourceEditor))
			{
				_editor = xWMapCellVisualResourceEditor;
				_history = instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance.FocusPanel("map_cell_editor");
				await WaitFrames(3);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private T FindControl<T>(string name) where T : Node
	{
		return _editor?.FindChild(name, recursive: true, owned: false) as T;
	}

	private static Window FindAncestorWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static void SimulateTextSession(LineEdit control, string value)
	{
		control.EmitSignal(Control.SignalName.FocusEntered);
		control.Text = value;
		control.EmitSignal(LineEdit.SignalName.TextChanged, value);
		control.EmitSignal(LineEdit.SignalName.TextSubmitted, value);
		control.EmitSignal(Control.SignalName.FocusExited);
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_MAP_CELL_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (_failures.Count > 0)
		{
			foreach (string failure in _failures)
			{
				GD.PrintErr("[MOD_EDITOR_MAP_CELL_PROBE_FAILURE] " + failure);
			}
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
