using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DSpriteAtlasRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private string _scenePath = "";

	public override async void _Ready()
	{
		_ = 14;
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "无法实例化 ModEditorManager。");
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
			XW2DSceneEditor editor = await WaitForSceneEditor(900);
			bool window = FindAncestorWindow(editor) != null;
			Require(window, "2D 编辑器没有挂载到 F3 ModEditor 窗口。");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			_scenePath = $"res://Tests/.mod_editor_2d_sprite_atlas_{Guid.NewGuid():N}.tscn";
			Require(SaveSourceScene(_scenePath), "无法保存图集探针源场景。");
			editor.LoadPackedSceneFromPath(_scenePath);
			await WaitFrames(10);
			Sprite2D sprite = editor.CurrentSceneInstance?.FindChild("AtlasSprite", recursive: false, owned: false) as Sprite2D;
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			GodotObject inspectorBefore = inspector?.CurrentObject;
			Require(GodotObject.IsInstanceValid(sprite), "编辑场景缺少 AtlasSprite。");
			Require(GodotObject.IsInstanceValid(dock), "真实场景树缺失。");
			Require(GodotObject.IsInstanceValid(history), "场景撤销管理器缺失。");
			if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(dock) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			dock.SelectNode(sprite, emitSignal: false);
			XWEditorInterface.Instance.SetSelectedNode(sprite);
			await WaitFrames(4);
			bool opened = editor.OpenSpriteAtlasForSelection();
			await WaitFrames(5);
			XW2DSpriteAtlasPanel panel = editor.SpriteAtlasPanel;
			XW2DSpriteAtlasCanvas canvas = panel?.AtlasCanvas;
			bool panelBound = opened && GodotObject.IsInstanceValid(panel) && panel.CurrentSprite == sprite && GodotObject.IsInstanceValid(canvas);
			Require(panelBound, "图集工作台没有绑定真实 Sprite2D。");
			if (!panelBound)
			{
				Finish();
				return;
			}
			history.ClearHistory();
			dock.PrepareForSceneHistoryBranchChange();
			bool gridApplied = panel.SetGridWithHistory(4, 2) && sprite.Hframes == 4 && sprite.Vframes == 2 && history.GetCurrentActionName() == "设置精灵图集网格";
			bool gridUndoCalled = history.Undo();
			await WaitFrames(2);
			bool gridUndo = gridUndoCalled && sprite.Hframes == 1 && sprite.Vframes == 1;
			bool gridRedoCalled = history.Redo();
			await WaitFrames(3);
			bool flag = gridRedoCalled && sprite.Hframes == 4 && sprite.Vframes == 2;
			bool gridUndoRedo = gridApplied && gridUndo && flag;
			Require(gridUndoRedo, "图集行列没有完整执行撤销和重做。");
			await WaitFrames(3);
			Rect2 atlasRect = canvas.DrawnAtlasRect;
			Vector2 click = atlasRect.Position + new Vector2(atlasRect.Size.X * 1.5f / 4f, atlasRect.Size.Y * 1.5f / 2f);
			canvas.EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton
			{
				Position = click,
				ButtonIndex = MouseButton.Left,
				ButtonMask = MouseButtonMask.Left,
				Pressed = true
			});
			await WaitFrames(3);
			bool canvasClick = sprite.Frame == 5 && history.GetCurrentActionName() == "选择精灵图集帧";
			Require(canvasClick, $"点击可视图集格子没有选中第 5 帧（实际 {sprite.Frame}，画布 {canvas.Size}，图集矩形 {atlasRect}，点击 {click}）。");
			bool frameUndoCalled = history.Undo();
			await WaitFrames(2);
			bool frameUndo = frameUndoCalled && sprite.Frame == 0;
			bool frameRedoCalled = history.Redo();
			await WaitFrames(2);
			bool flag2 = frameRedoCalled && sprite.Frame == 5;
			bool frameUndoRedo = frameUndo && flag2;
			Require(frameUndoRedo, "可视选帧没有完整执行撤销和重做。");
			Vector2 size = sprite.Texture.GetSize();
			Rect2 region = new Rect2(Vector2.Zero, new Vector2(Mathf.Max(1f, size.X * 0.5f), Mathf.Max(1f, size.Y)));
			bool regionApplied = panel.SetRegionWithHistory(enabled: true, region) && sprite.RegionEnabled && sprite.RegionRect.IsEqualApprox(region) && history.GetCurrentActionName() == "设置精灵图集区域";
			bool regionUndoCalled = history.Undo();
			await WaitFrames(2);
			bool regionUndo = regionUndoCalled && !sprite.RegionEnabled;
			bool regionRedoCalled = history.Redo();
			await WaitFrames(3);
			bool flag3 = regionRedoCalled && sprite.RegionEnabled && sprite.RegionRect.IsEqualApprox(region);
			bool regionUndoRedo = regionApplied && regionUndo && flag3;
			Require(regionUndoRedo, "局部图集区域没有完整执行撤销和重做。");
			double maximumFrameGapMs = 0.0;
			long previousTicks = Stopwatch.GetTimestamp();
			for (int index = 0; index < 32; index++)
			{
				panel.SetFrameWithHistory(index % 8);
				await WaitFrames(1);
				long timestamp = Stopwatch.GetTimestamp();
				double val = (double)(timestamp - previousTicks) * 1000.0 / (double)Stopwatch.Frequency;
				maximumFrameGapMs = Math.Max(maximumFrameGapMs, val);
				previousTicks = timestamp;
			}
			bool mainThreadResponsive = maximumFrameGapMs < 250.0;
			bool canvasIdle = !canvas.IsProcessing();
			canvas.Bind(sprite.Texture, 256, 256, 0, regionEnabled: true, region);
			bool densityGuard = canvas.IsGuideDensityLimited && !canvas.IsProcessing();
			canvas.Bind(sprite.Texture, sprite.Hframes, sprite.Vframes, sprite.Frame, sprite.RegionEnabled, sprite.RegionRect);
			Require(mainThreadResponsive, $"图集连续点选阻塞了主线程，最大帧间隔 {maximumFrameGapMs:0.0} ms。");
			Require(canvasIdle, "图集画布在空闲时仍持续逐帧处理。");
			Require(densityGuard, "超大图集网格没有启用辅助线密度保护。");
			bool inspectorUntouched = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorBefore && inspector.CurrentObject != sprite;
			Require(inspectorUntouched, "图集工作台把 Sprite2D 导向了原始 Inspector。");
			bool saved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node obj = ResourceLoader.Load<PackedScene>(_scenePath, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
			Sprite2D sprite2D = obj?.FindChild("AtlasSprite", recursive: false, owned: false) as Sprite2D;
			bool flag4 = GodotObject.IsInstanceValid(sprite2D) && sprite2D.Hframes == 4 && sprite2D.Vframes == 2 && sprite2D.Frame == sprite.Frame && sprite2D.RegionEnabled && sprite2D.RegionRect.IsEqualApprox(region);
			obj?.Free();
			Require(saved, "图集编辑结果没有保存。");
			Require(flag4, "图集行列、帧与区域没有通过忽略缓存的重载。");
			GD.Print($"[MOD_EDITOR_2D_SPRITE_ATLAS_PROBE] window={window} panelBound={panelBound} canvasClick={canvasClick} gridUndoRedo={gridUndoRedo} frameUndoRedo={frameUndoRedo} regionUndoRedo={regionUndoRedo} saved={saved} reloaded={flag4} inspectorUntouched={inspectorUntouched} mainThreadResponsive={mainThreadResponsive} maxFrameGapMs={maximumFrameGapMs:0.0} canvasIdle={canvasIdle} densityGuard={densityGuard} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
			history.ClearHistory(1);
			editor.SpriteAtlasPanel.BindSprite(null);
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static bool SaveSourceScene(string path)
	{
		Node2D node2D = new Node2D
		{
			Name = "AtlasProbeRoot"
		};
		Texture2D texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/AtlasTexture.svg", null, ResourceLoader.CacheMode.Reuse);
		Sprite2D sprite2D = new Sprite2D
		{
			Name = "AtlasSprite",
			Texture = texture,
			Hframes = 1,
			Vframes = 1,
			Frame = 0,
			RegionEnabled = false
		};
		node2D.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		sprite2D.Owner = node2D;
		PackedScene packedScene = new PackedScene
		{
			ResourceName = "AtlasProbeScene"
		};
		Error num = packedScene.Pack(node2D);
		node2D.Free();
		if (num == Error.Ok)
		{
			return ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None) == Error.Ok;
		}
		return false;
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsInsideTree())
			{
				return xW2DSceneEditor;
			}
			await WaitFrames(1);
		}
		Require(condition: false, $"F3 在 {maximumFrames} 帧内没有初始化 2D 编辑器。");
		return null;
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

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
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
			GD.PrintErr("[MOD_EDITOR_2D_SPRITE_ATLAS_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (!string.IsNullOrWhiteSpace(_scenePath) && FileAccess.FileExists(_scenePath))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(_scenePath));
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_SPRITE_ATLAS_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
