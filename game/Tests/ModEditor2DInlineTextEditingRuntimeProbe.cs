using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DInlineTextEditingRuntimeProbe : Node
{
	private const string SceneAPath = "user://mod_editor_2d_inline_text_probe_a.tscn";

	private const string SceneBPath = "user://mod_editor_2d_inline_text_probe_b.tscn";

	private const string ButtonScenePath = "res://Prefab/GUI/Button/NinePatchButtonBase/NinePatchButtonBase.tscn";

	private readonly List<string> _failures = new List<string>();

	private Viewport _inputViewport;

	private XW2DSceneEditor _editor;

	private XW2DViewport _canvas;

	private XWUndoRedoManager _history;

	private XWInspector _inspector;

	private Node _inspectorSentinel;

	private bool _f3;

	private bool _window;

	private bool _sceneLoaded;

	private bool _doubleClick;

	private bool _chineseSingle;

	private bool _overlayPlacement;

	private bool _escapeRollback;

	private bool _singleCommit;

	private bool _undoRedo;

	private bool _richTextBbcode;

	private bool _multiline;

	private bool _customExport;

	private bool _stringName;

	private bool _placeholder;

	private bool _saveReload;

	private bool _tabIsolation;

	private bool _hiddenCleanup;

	private bool _canvasLayer;

	private bool _lockedHiddenSafe;

	private bool _lowPerfNoShadow;

	private bool _inspectorUntouched;

	private bool _noCanvasLayerCacheRebuild;

	public override async void _Ready()
	{
		try
		{
			Require(SaveFixtureScene("user://mod_editor_2d_inline_text_probe_a.tscn", "甲"), "无法保存画布文字探针场景甲。");
			Require(SaveFixtureScene("user://mod_editor_2d_inline_text_probe_b.tscn", "乙"), "无法保存画布文字探针场景乙。");
			if (_failures.Count > 0)
			{
				PrintResult();
				Finish();
				return;
			}
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
				PrintResult();
				Finish();
				return;
			}
			await WaitFrames(2);
			SendGlobalKey(Key.F3);
			_f3 = await WaitForModEditorReady(900);
			Require(_f3, "F3 ModEditor 未完成真实加载队列。");
			_editor = await WaitForSceneEditor(90);
			_window = GodotObject.IsInstanceValid(FindAncestorWindow(_editor));
			Require(_window, "2D 编辑器没有挂载到真实 F3 ModEditor 窗口。");
			bool flag = await EnterEditorSurface(90);
			Require(flag, "未进入真实 2D 编辑工作区。");
			if (!_f3 || !_window || !flag || !GodotObject.IsInstanceValid(_editor))
			{
				PrintResult();
				Finish();
				return;
			}
			_editor.LoadPackedSceneFromPath("user://mod_editor_2d_inline_text_probe_a.tscn");
			await WaitFrames(10);
			ActivateEditorDock();
			TabContainer nodeOrNull = _editor.GetNodeOrNull<TabContainer>("%WorkspaceTabs");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.CurrentTab = 0;
			}
			await WaitFrames(4);
			_canvas = _editor.FindChild("Viewport2D", recursive: true, owned: false) as XW2DViewport;
			_history = XWEditorInterface.Instance?.GetUndoRedoManager();
			_inputViewport = FindAncestorWindow(_editor) ?? _canvas?.GetViewport();
			_inputViewport?.NotifyMouseEntered();
			_sceneLoaded = GodotObject.IsInstanceValid(_canvas) && _canvas.IsVisibleInTree() && GodotObject.IsInstanceValid(_editor.CurrentSceneInstance) && GodotObject.IsInstanceValid(_history);
			Require(_sceneLoaded, "真实 2D 画布、场景实例或共享历史未就绪。");
			if (!_sceneLoaded)
			{
				PrintResult();
				Finish();
				return;
			}
			_inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			_inspectorSentinel = new Node
			{
				Name = "InlineTextInspectorSentinel"
			};
			AddChild(_inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(_inspectorSentinel);
			await WaitFrames(2);
			ActivateEditorDock();
			Node currentSceneInstance = _editor.CurrentSceneInstance;
			Label singleA = Find<Label>(currentSceneInstance, "SingleLabel");
			RichTextLabel richA = Find<RichTextLabel>(currentSceneInstance, "RichTextLabel");
			NinePatchButtonBase customA = Find<NinePatchButtonBase>(currentSceneInstance, "CustomExportButton");
			ModEditor2DInlineStringNameProbeControl stringNameA = Find<ModEditor2DInlineStringNameProbeControl>(currentSceneInstance, "StringNameLabel");
			LineEdit placeholderA = Find<LineEdit>(currentSceneInstance, "PlaceholderLineEdit");
			Label layerLabelA = Find<Label>(currentSceneInstance, "CanvasLayerLabel");
			Label lockedA = Find<Label>(currentSceneInstance, "LockedLabel");
			Label lockedChildA = Find<Label>(currentSceneInstance, "LockedChildLabel");
			Label hiddenFallbackA = Find<Label>(currentSceneInstance, "HiddenFallbackLabel");
			Require(AllValid(singleA, richA, customA, stringNameA, placeholderA, layerLabelA, lockedA, lockedChildA, hiddenFallbackA), "画布文字探针场景缺少一种或多种文字节点。");
			string singleBefore = singleA.Text;
			ResetHistory(_editor.GetCurrentSceneHistoryId());
			int cancelBefore = _editor.InlineTextCancelCount;
			_doubleClick = await UserDoubleClick(singleA) && _editor.InlineTextEditorVisible && _editor.InlineTextTarget == singleA && _editor.InlineTextProperty.ToString() == "text";
			Require(_doubleClick, "用户态双击没有进入单行画布文字覆盖层。");
			Rect2 canvasItemViewportBounds = _canvas.GetCanvasItemViewportBounds(singleA);
			Rect2 inlineTextInputRect = _editor.InlineTextInputRect;
			_overlayPlacement = IsOriginalPositionOverlay(canvasItemViewportBounds, inlineTextInputRect) && _editor.InlineTextOverlayRect.HasArea();
			Require(_overlayPlacement, "单行输入框没有覆盖在原游戏文字位置。");
			int rebuildBeforeTyping = ReadCanvasLayerCacheRebuildCount(_canvas);
			bool perCharacterCacheStable = true;
			int previewBefore = _editor.InlineTextPreviewCount;
			await TypeText("画布中文预览", delegate
			{
				int num = ReadCanvasLayerCacheRebuildCount(_canvas);
				if (rebuildBeforeTyping < 0 || num != rebuildBeforeTyping)
				{
					perCharacterCacheStable = false;
				}
			});
			await WaitFrames(2);
			_chineseSingle = singleA.Text == "画布中文预览" && _editor.InlineTextDraft == "画布中文预览" && _editor.InlineTextPreviewCount > previewBefore;
			_noCanvasLayerCacheRebuild = perCharacterCacheStable && ReadCanvasLayerCacheRebuildCount(_canvas) == rebuildBeforeTyping;
			Require(_chineseSingle, "中文逐字输入没有即时预览到真实 Label。");
			Require(_noCanvasLayerCacheRebuild, "逐字输入触发了 CanvasLayer 全树缓存重建。");
			int versionBeforeEscape = _history.GetVersion();
			SendKey(Key.Escape);
			await WaitFrames(4);
			_escapeRollback = !_editor.InlineTextEditorVisible && singleA.Text == singleBefore && _history.GetVersion() == versionBeforeEscape && !_history.HasUndo() && _editor.InlineTextCancelCount == cancelBefore + 1;
			Require(_escapeRollback, "Esc 没有回退预览文字，或错误写入了历史。");
			ResetHistory(_editor.GetCurrentSceneHistoryId());
			int commitBefore = _editor.InlineTextCommitCount;
			int versionBeforeCommit = _history.GetVersion();
			bool openedSingle = await UserDoubleClick(singleA);
			await TypeText("游戏标题已提交");
			SendKey(Key.Enter);
			await WaitFrames(5);
			string singleCommitted = "游戏标题已提交";
			_singleCommit = openedSingle && !_editor.InlineTextEditorVisible && singleA.Text == singleCommitted && _editor.InlineTextCommitCount == commitBefore + 1 && _history.GetVersion() == versionBeforeCommit + 1 && _history.HasUndo();
			bool undo = _singleCommit && _history.Undo();
			await WaitFrames(3);
			bool undoValue = undo && singleA.Text == singleBefore && !_history.HasUndo();
			bool redo = undoValue && _history.Redo();
			await WaitFrames(3);
			bool flag2 = redo && singleA.Text == singleCommitted;
			_undoRedo = undoValue && flag2;
			Require(_singleCommit, "单行 Enter 提交没有严格产生一次场景历史。");
			Require(_undoRedo, "单行文字撤销/重做没有完整往返。");
			ResetHistory(_editor.GetCurrentSceneHistoryId());
			string richCommitted = "[b]粗体标题[/b]\n第二行中文";
			bool richOpened = await UserDoubleClick(richA);
			await TypeText("[b]粗体标题[/b]");
			SendKey(Key.Enter);
			await WaitFrames(1);
			await TypeText("第二行中文", null, selectAllExpected: false);
			SendKey(Key.Enter, ctrl: true);
			await WaitFrames(5);
			_multiline = richOpened && !_editor.InlineTextEditorVisible && richA.Text == richCommitted && richCommitted.Contains('\n') && _history.HasUndo();
			_richTextBbcode = _multiline && richA.BbcodeEnabled && richA.Text.Contains("[b]", StringComparison.Ordinal) && richA.Text.Contains("[/b]", StringComparison.Ordinal);
			Require(_multiline, "RichTextLabel 普通 Enter 换行或 Ctrl+Enter 提交失败。");
			Require(_richTextBbcode, "RichTextLabel BBCode 在画布编辑时被转义或丢失。");
			ResetHistory(_editor.GetCurrentSceneHistoryId());
			string customCommitted = "自定义按钮\n第二行";
			bool customResolved = await UserDoubleClick(customA) && _editor.InlineTextTarget == customA && _editor.InlineTextProperty.ToString() == "text" && _editor.InlineTextHint == PropertyHint.MultilineText;
			await TypeText("自定义按钮");
			SendKey(Key.Enter);
			await WaitFrames(1);
			await TypeText("第二行", null, selectAllExpected: false);
			SendKey(Key.Enter, ctrl: true);
			await WaitFrames(5);
			_customExport = customResolved && customA.text == customCommitted && GodotObject.IsInstanceValid(customA.labelText) && customA.labelText.Text == customCommitted && _history.HasUndo();
			Require(_customExport, "自定义 Export MultilineText 属性没有映射到游戏按钮本体。");
			ResetHistory(_editor.GetCurrentSceneHistoryId());
			bool stringNameResolved = await UserDoubleClick(stringNameA) && _editor.InlineTextTarget == stringNameA && _editor.InlineTextProperty.ToString() == "Caption" && _editor.InlineTextValueType == Variant.Type.StringName;
			await TypeText("字符串名称已改");
			SendKey(Key.Enter);
			await WaitFrames(4);
			_stringName = stringNameResolved && stringNameA.Caption == new StringName("字符串名称已改") && _history.HasUndo();
			Require(_stringName, "StringName 文字属性未保持类型提交。");
			ResetHistory(_editor.GetCurrentSceneHistoryId());
			bool placeholderResolved = await UserDoubleClick(placeholderA) && _editor.InlineTextTarget == placeholderA && _editor.InlineTextProperty.ToString() == "placeholder_text";
			await TypeText("不可输入框的新占位文字");
			SendKey(Key.Enter);
			await WaitFrames(4);
			_placeholder = placeholderResolved && placeholderA.Text == "" && placeholderA.PlaceholderText == "不可输入框的新占位文字" && !placeholderA.Editable && _history.HasUndo();
			Require(_placeholder, "空且不可编辑的 LineEdit 没有编辑 placeholder_text。");
			string layerBefore = layerLabelA.Text;
			bool layerOpened = await UserDoubleClick(layerLabelA);
			Rect2 layerTargetRect = _canvas.GetCanvasItemViewportBounds(layerLabelA);
			Rect2 layerInputRect = _editor.InlineTextInputRect;
			await TypeText("图层映射预览");
			await WaitFrames(2);
			bool layerPreview = layerLabelA.Text == "图层映射预览";
			SendKey(Key.Escape);
			await WaitFrames(3);
			_canvasLayer = layerOpened && layerPreview && IsOriginalPositionOverlay(layerTargetRect, layerInputRect) && layerLabelA.Text == layerBefore;
			Require(_canvasLayer, "CanvasLayer 变换后的命中、覆盖位置或 Esc 回退错误。");
			bool lockedRejected = !(await UserDoubleClick(lockedA)) && !_editor.InlineTextEditorVisible;
			bool lockedParentRejected = !(await UserDoubleClick(lockedChildA)) && !_editor.InlineTextEditorVisible;
			bool hiddenOpened = await UserDoubleClick(hiddenFallbackA);
			Node hiddenTarget = _editor.InlineTextTarget;
			SendKey(Key.Escape);
			await WaitFrames(3);
			_lockedHiddenSafe = lockedRejected && lockedParentRejected && hiddenOpened && hiddenTarget == hiddenFallbackA;
			Require(_lockedHiddenSafe, "锁定节点被误编辑，或隐藏覆盖节点拦截了可见节点。");
			bool previousLowPerformance = XWUiMotion.LowPerformanceMode;
			try
			{
				XWUiMotion.SetLowPerformanceMode(lowPerformanceMode: true);
				bool flag3 = await UserDoubleClick(singleA);
				PanelContainer panelContainer = _editor.FindChild("InlineTextEditorOverlay", recursive: true, owned: false) as PanelContainer;
				StyleBoxFlat styleBoxFlat = panelContainer?.GetThemeStylebox("panel") as StyleBoxFlat;
				_lowPerfNoShadow = flag3 && GodotObject.IsInstanceValid(panelContainer) && styleBoxFlat != null && styleBoxFlat.ShadowSize == 0;
				SendKey(Key.Escape);
				await WaitFrames(3);
			}
			finally
			{
				XWUiMotion.SetLowPerformanceMode(previousLowPerformance);
			}
			Require(_lowPerfNoShadow, "低性能模式仍给画布文字覆盖层绘制阴影。");
			bool savedA = _editor.SaveCurrentScene();
			await WaitFrames(5);
			Node node = InstantiateUncached("user://mod_editor_2d_inline_text_probe_a.tscn");
			_saveReload = savedA && Find<Label>(node, "SingleLabel")?.Text == singleCommitted && Find<RichTextLabel>(node, "RichTextLabel")?.Text == richCommitted && Find<NinePatchButtonBase>(node, "CustomExportButton")?.text == customCommitted && Find<LineEdit>(node, "PlaceholderLineEdit")?.PlaceholderText == "不可输入框的新占位文字" && Find<ModEditor2DInlineStringNameProbeControl>(node, "StringNameLabel")?.Caption == new StringName("字符串名称已改");
			node?.Free();
			Require(_saveReload, "画布文字没有通过保存与 CacheMode.Ignore 重载。");
			_editor.LoadPackedSceneFromPath("user://mod_editor_2d_inline_text_probe_b.tscn");
			await WaitFrames(9);
			TabBar sceneTabs = _editor.FindChild("SceneTabBar", recursive: true, owned: false) as TabBar;
			Node currentSceneInstance2 = _editor.CurrentSceneInstance;
			Label singleB = Find<Label>(currentSceneInstance2, "SingleLabel");
			int historyA = _editor.GetSceneHistoryIdAt(0L);
			int historyB = _editor.GetSceneHistoryIdAt(1L);
			ResetHistory(historyB);
			bool openedB = await UserDoubleClick(singleB);
			await TypeText("乙标签独立文字");
			SendKey(Key.Enter);
			await WaitFrames(4);
			bool bCommitted = openedB && singleB.Text == "乙标签独立文字";
			if (GodotObject.IsInstanceValid(sceneTabs))
			{
				sceneTabs.CurrentTab = 0;
			}
			await WaitFrames(6);
			currentSceneInstance = _editor.CurrentSceneInstance;
			singleA = Find<Label>(currentSceneInstance, "SingleLabel");
			ResetHistory(historyA);
			bool openedA = await UserDoubleClick(singleA);
			await TypeText("甲标签独立文字");
			SendKey(Key.Enter);
			await WaitFrames(4);
			bool isolatedUndoCalled = openedA && singleA.Text == "甲标签独立文字" && _history.Undo();
			await WaitFrames(3);
			bool isolatedRedo = isolatedUndoCalled && singleA.Text == singleCommitted && singleB.Text == "乙标签独立文字" && _history.Redo();
			await WaitFrames(3);
			_tabIsolation = historyA >= 0 && historyB >= 0 && historyA != historyB && bCommitted && isolatedRedo && singleA.Text == "甲标签独立文字" && singleB.Text == "乙标签独立文字";
			Require(_tabIsolation, "双场景标签的文字值或撤销历史发生串扰。");
			int hiddenCommitBefore = _editor.InlineTextCommitCount;
			int hiddenVersionBefore = _history.GetVersion();
			bool hiddenOpenedForCleanup = await UserDoubleClick(singleA);
			await TypeText("隐藏时自动收尾");
			Control inlineInput = GetActiveInlineInput();
			_editor.Hide();
			await WaitFrames(8);
			_hiddenCleanup = hiddenOpenedForCleanup && !_editor.IsVisibleInTree() && !_editor.InlineTextEditorVisible && singleA.Text == "隐藏时自动收尾" && _editor.InlineTextCommitCount == hiddenCommitBefore + 1 && _history.GetVersion() == hiddenVersionBefore + 1 && !_canvas.IsProcessing() && (!GodotObject.IsInstanceValid(inlineInput) || !inlineInput.HasFocus());
			Require(_hiddenCleanup, "隐藏 2D 编辑器没有提交一次待输入或停止处理。");
			_inspectorUntouched = GodotObject.IsInstanceValid(_inspector) && _inspector.CurrentObject == _inspectorSentinel && _inspector.CurrentObject != singleA && _inspector.CurrentObject != singleB;
			Require(_inspectorUntouched, "画布文字编辑占用了原始 Inspector。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
			GD.PrintErr($"[MOD_EDITOR_2D_INLINE_TEXT_EDITING_PROBE_FAILURE] {ex}");
		}
		PrintResult();
		Finish();
	}

	private static bool SaveFixtureScene(string path, string suffix)
	{
		Node2D node2D = new Node2D
		{
			Name = "InlineTextProbe" + suffix
		};
		Label child = new Label
		{
			Name = "SingleLabel",
			Position = new Vector2(-420f, -230f),
			Size = new Vector2(330f, 54f),
			Text = "原始中文标题" + suffix,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddOwned(node2D, child);
		RichTextLabel child2 = new RichTextLabel
		{
			Name = "RichTextLabel",
			Position = new Vector2(-420f, -140f),
			Size = new Vector2(350f, 120f),
			Text = "[b]原始富文本" + suffix + "[/b]\n原始第二行",
			BbcodeEnabled = true,
			FitContent = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddOwned(node2D, child2);
		NinePatchButtonBase ninePatchButtonBase = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/Button/NinePatchButtonBase/NinePatchButtonBase.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<NinePatchButtonBase>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(ninePatchButtonBase))
		{
			node2D.Free();
			return false;
		}
		ninePatchButtonBase.Name = "CustomExportButton";
		ninePatchButtonBase.Position = new Vector2(50f, -150f);
		ninePatchButtonBase.Size = new Vector2(270f, 86f);
		ninePatchButtonBase.text = "自定义原文" + suffix;
		AddOwned(node2D, ninePatchButtonBase);
		ModEditor2DInlineStringNameProbeControl modEditor2DInlineStringNameProbeControl = new ModEditor2DInlineStringNameProbeControl
		{
			Name = "StringNameLabel",
			Position = new Vector2(50f, -30f),
			Size = new Vector2(290f, 48f),
			Caption = new StringName("StringName 原文" + suffix),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		modEditor2DInlineStringNameProbeControl.SetMeta("_xw_inline_text_property", "Caption");
		AddOwned(node2D, modEditor2DInlineStringNameProbeControl);
		LineEdit child3 = new LineEdit
		{
			Name = "PlaceholderLineEdit",
			Position = new Vector2(50f, 52f),
			Size = new Vector2(310f, 50f),
			Text = "",
			PlaceholderText = "请输入玩家名称" + suffix,
			Editable = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddOwned(node2D, child3);
		Label label = new Label
		{
			Name = "LockedLabel",
			Position = new Vector2(-420f, 55f),
			Size = new Vector2(320f, 50f),
			Text = "锁定文字" + suffix,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.SetMeta("_edit_lock_", true);
		AddOwned(node2D, label);
		Control control = new Control
		{
			Name = "LockedParent",
			Position = new Vector2(-420f, -18f),
			Size = new Vector2(320f, 50f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		control.SetMeta("_edit_lock_", true);
		AddOwned(node2D, control);
		Label label2 = new Label
		{
			Name = "LockedChildLabel",
			Size = control.Size,
			Text = "父节点锁定文字" + suffix,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		control.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		label2.Owner = node2D;
		Label label3 = new Label
		{
			Name = "HiddenFallbackLabel",
			Position = new Vector2(-420f, 135f),
			Size = new Vector2(320f, 50f),
			Text = "隐藏节点下方可见文字" + suffix,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddOwned(node2D, label3);
		Label child4 = new Label
		{
			Name = "HiddenTopLabel",
			Position = label3.Position,
			Size = label3.Size,
			Text = "不应命中的隐藏文字" + suffix,
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddOwned(node2D, child4);
		CanvasLayer canvasLayer = new CanvasLayer
		{
			Name = "MappedCanvasLayer",
			Transform = new Transform2D(0.08f, new Vector2(0.92f, 1.08f), 0f, new Vector2(36f, 18f))
		};
		AddOwned(node2D, canvasLayer);
		Label label4 = new Label
		{
			Name = "CanvasLayerLabel",
			Position = new Vector2(120f, 230f),
			Size = new Vector2(300f, 54f),
			Text = "CanvasLayer 文字" + suffix,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		canvasLayer.AddChild(label4, forceReadableName: false, InternalMode.Disabled);
		label4.Owner = node2D;
		PackedScene packedScene = new PackedScene
		{
			ResourceName = "InlineTextProbe" + suffix
		};
		Error error = packedScene.Pack(node2D);
		if (error == Error.Ok)
		{
			error = ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None);
		}
		node2D.Free();
		return error == Error.Ok;
	}

	private static void AddOwned(Node root, Node child)
	{
		root.AddChild(child, forceReadableName: false, InternalMode.Disabled);
		child.Owner = root;
	}

	private async Task<bool> UserDoubleClick(CanvasItem target)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(_canvas) || !GodotObject.IsInstanceValid(_inputViewport))
		{
			return false;
		}
		_canvas.SetToolMode(XW2DViewport.ToolMode.Select);
		Rect2 canvasItemWorldBounds = _canvas.GetCanvasItemWorldBounds(target);
		if (canvasItemWorldBounds.HasArea())
		{
			_canvas.CenterAt(canvasItemWorldBounds.GetCenter());
		}
		await WaitFrames(3);
		Rect2 canvasItemViewportBounds = _canvas.GetCanvasItemViewportBounds(target);
		if (!canvasItemViewportBounds.HasArea())
		{
			return false;
		}
		Vector2 center = canvasItemViewportBounds.GetCenter();
		Vector2 windowPoint = _canvas.GetGlobalTransformWithCanvas() * center;
		PushMouseMotion(windowPoint);
		PushMouseButton(windowPoint, pressed: true, doubleClick: false);
		PushMouseButton(windowPoint, pressed: false, doubleClick: false);
		await WaitFrames(2);
		PushMouseButton(windowPoint, pressed: true, doubleClick: true);
		await WaitFrames(1);
		PushMouseButton(windowPoint, pressed: false, doubleClick: true);
		await WaitFrames(4);
		return _editor.InlineTextEditorVisible;
	}

	private void PushMouseMotion(Vector2 point)
	{
		_inputViewport.PushInput(new InputEventMouseMotion
		{
			Position = point,
			GlobalPosition = point
		}, inLocalCoords: true);
	}

	private void PushMouseButton(Vector2 point, bool pressed, bool doubleClick)
	{
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = point,
			GlobalPosition = point,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)(pressed ? 1 : 0),
			Pressed = pressed,
			DoubleClick = doubleClick
		}, inLocalCoords: true);
	}

	private async Task TypeText(string text, Action afterCharacter = null, bool selectAllExpected = true)
	{
		Control activeInlineInput = GetActiveInlineInput();
		Require(GodotObject.IsInstanceValid(activeInlineInput) && activeInlineInput.HasFocus(), "画布文字输入框没有获得真实键盘焦点。");
		if (!GodotObject.IsInstanceValid(activeInlineInput))
		{
			return;
		}
		if (selectAllExpected)
		{
			if (activeInlineInput is LineEdit lineEdit)
			{
				lineEdit.SelectAll();
			}
			else if (activeInlineInput is TextEdit textEdit)
			{
				textEdit.SelectAll();
			}
		}
		foreach (char character in text)
		{
			SendUnicode(character);
			await WaitFrames(1);
			afterCharacter?.Invoke();
		}
	}

	private Control GetActiveInlineInput()
	{
		LineEdit lineEdit = _editor?.FindChild("InlineTextLineEdit", recursive: true, owned: false) as LineEdit;
		if (GodotObject.IsInstanceValid(lineEdit) && lineEdit.Visible)
		{
			return lineEdit;
		}
		TextEdit textEdit = _editor?.FindChild("InlineTextMultilineEdit", recursive: true, owned: false) as TextEdit;
		if (!GodotObject.IsInstanceValid(textEdit) || !textEdit.Visible)
		{
			return null;
		}
		return textEdit;
	}

	private void SendUnicode(char character)
	{
		_inputViewport.PushInput(new InputEventKey
		{
			Keycode = Key.None,
			PhysicalKeycode = Key.None,
			Unicode = character,
			Pressed = true
		}, inLocalCoords: true);
		_inputViewport.PushInput(new InputEventKey
		{
			Keycode = Key.None,
			PhysicalKeycode = Key.None,
			Unicode = character,
			Pressed = false
		}, inLocalCoords: true);
	}

	private void SendKey(Key key, bool ctrl = false)
	{
		_inputViewport.PushInput(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = true,
			CtrlPressed = ctrl
		}, inLocalCoords: true);
		_inputViewport.PushInput(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = false,
			CtrlPressed = ctrl
		}, inLocalCoords: true);
	}

	private static void SendGlobalKey(Key key)
	{
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = true
		});
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = false
		});
	}

	private void ResetHistory(int historyId)
	{
		_history.ClearHistory(historyId);
		_history.SetCurrentHistoryType(historyId);
	}

	private static bool IsOriginalPositionOverlay(Rect2 target, Rect2 input)
	{
		if (!target.HasArea() || !input.HasArea())
		{
			return false;
		}
		Rect2 rect = target.Intersection(input);
		float a = target.Size.X * target.Size.Y;
		float b = input.Size.X * input.Size.Y;
		if (Mathf.Max(0f, rect.Size.X) * Mathf.Max(0f, rect.Size.Y) >= Mathf.Min(a, b) * 0.42f)
		{
			return input.GetCenter().DistanceTo(target.GetCenter()) <= Mathf.Max(36f, target.Size.Length() * 0.35f);
		}
		return false;
	}

	private static int ReadCanvasLayerCacheRebuildCount(XW2DViewport viewport)
	{
		object obj = typeof(XW2DViewport).GetProperty("CanvasLayerCacheRebuildCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(viewport);
		if (obj is int)
		{
			return (int)obj;
		}
		return -1;
	}

	private static Node InstantiateUncached(string path)
	{
		return ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
	}

	private static T Find<T>(Node root, string name) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		return root.FindChild(name, recursive: true, owned: false) as T;
	}

	private static bool AllValid(params GodotObject[] objects)
	{
		for (int i = 0; i < objects.Length; i++)
		{
			if (!GodotObject.IsInstanceValid(objects[i]))
			{
				return false;
			}
		}
		return true;
	}

	private void ActivateEditorDock()
	{
		XWEditorInterface.Instance?.FocusPanel("2d_editor");
	}

	private async Task<bool> WaitForModEditorReady(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Control instance = obj?.GetNodeOrNull<Control>("%LoadingOverlay");
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsInsideTree())
			{
				return xW2DSceneEditor;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Control instance = obj?.GetNodeOrNull<Control>("%LoadingOverlay");
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				ActivateEditorDock();
				await WaitFrames(4);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
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
			GD.PrintErr("[MOD_EDITOR_2D_INLINE_TEXT_EDITING_PROBE_FAILURE] " + message);
		}
	}

	private void PrintResult()
	{
		bool value = _f3 && _window && _sceneLoaded && _doubleClick && _chineseSingle && _overlayPlacement && _escapeRollback && _singleCommit && _undoRedo && _richTextBbcode && _multiline && _customExport && _stringName && _placeholder && _saveReload && _tabIsolation && _hiddenCleanup && _canvasLayer && _lockedHiddenSafe && _lowPerfNoShadow && _inspectorUntouched && _noCanvasLayerCacheRebuild && _failures.Count == 0;
		GD.Print($"[MOD_EDITOR_2D_INLINE_TEXT_EDITING_PROBE] f3={_f3} window={_window} sceneLoaded={_sceneLoaded} doubleClick={_doubleClick} chineseSingle={_chineseSingle} overlayPlacement={_overlayPlacement} escapeRollback={_escapeRollback} singleCommit={_singleCommit} undoRedo={_undoRedo} richTextBbcode={_richTextBbcode} multiline={_multiline} customExport={_customExport} stringName={_stringName} placeholder={_placeholder} saveReload={_saveReload} tabIsolation={_tabIsolation} hiddenCleanup={_hiddenCleanup} canvasLayer={_canvasLayer} lockedHiddenSafe={_lockedHiddenSafe} lowPerfNoShadow={_lowPerfNoShadow} inspectorUntouched={_inspectorUntouched} noCanvasLayerCacheRebuild={_noCanvasLayerCacheRebuild} success={value} failures={_failures.Count}");
	}

	private void Finish()
	{
		if (GodotObject.IsInstanceValid(_inputViewport))
		{
			_inputViewport.NotifyMouseExited();
		}
		_inputViewport = null;
		DeleteFixture("user://mod_editor_2d_inline_text_probe_a.tscn");
		DeleteFixture("user://mod_editor_2d_inline_text_probe_b.tscn");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_INLINE_TEXT_EDITING_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static void DeleteFixture(string path)
	{
		if (FileAccess.FileExists(path))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
		}
	}
}
