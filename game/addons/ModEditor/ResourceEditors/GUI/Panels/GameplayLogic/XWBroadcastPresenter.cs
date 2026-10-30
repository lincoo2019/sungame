using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Tools;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWBroadcastPresenter : IXWGameplayLogicPresenter
{
	public const int MaximumBroadcastCharacters = 4000;

	private const string BroadcastScenePath = "res://Core/BroadCastManager/BroadCastManager.tscn";

	private XWGameplayLogicPresentationContext _context;

	private BroadCastConfig _config;

	private BroadCastManager _previewManager;

	private TextEdit _textEditor;

	private Label _resolvedLabel;

	private Label _characterCountLabel;

	private Label _durationLabel;

	private Timer _previewTimer;

	private Button _readyButton;

	private Button _battleButton;

	private Button _settleButton;

	private HSlider _scrubber;

	private Label _timeLabel;

	private Action _readyPressed;

	private Action _battlePressed;

	private Action _settlePressed;

	private Godot.Range.ValueChangedEventHandler _scrubberChanged;

	private bool _syncingTransport;

	private XWGameplayLogicPreviewSafety PreviewSafety => _context?.PreviewSafety;

	public double PreviewTime { get; private set; }

	private double PreviewDuration
	{
		get
		{
			BroadCastConfig config = _config;
			if (config == null || !(config.broadCastTime > 0.0))
			{
				return 5.0;
			}
			return _config.broadCastTime;
		}
	}

	public bool CanPresent(Resource resource)
	{
		return resource is BroadCastConfig;
	}

	public void Mount(XWGameplayLogicPresentationContext context)
	{
		_context = context;
		_config = context?.Resource as BroadCastConfig;
	}

	public void Refresh()
	{
		if (GodotObject.IsInstanceValid(_config))
		{
			DisconnectTransport();
			ClearChildren(_context.StageRoot);
			ClearChildren(_context.OverlayRoot);
			ClearChildren(_context.HudRoot);
			ClearChildren(_context.ShelfRoot);
			ClearChildren(_context.TimelineRoot);
			MountBroadcastBar();
			MountResourceShelf();
			MountDurationTimeline();
			SetupTransport();
			PreviewTime = 0.0;
			UpdateLocalizedPreview();
			SetPreviewTime(0.0);
		}
	}

	public void Unmount()
	{
		DisconnectTransport();
		_previewManager = null;
		_textEditor = null;
		_resolvedLabel = null;
		_characterCountLabel = null;
		_durationLabel = null;
		_config = null;
		_context = null;
	}

	private void MountBroadcastBar()
	{
		_previewManager = ResourceLoader.Load<PackedScene>("res://Core/BroadCastManager/BroadCastManager.tscn", "", ResourceLoader.CacheMode.Reuse)?.Instantiate<BroadCastManager>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_previewManager))
		{
			_context.ShelfRoot.AddChild(new Label
			{
				Text = "游戏广播条场景加载失败",
				Modulate = new Color("e66b62")
			}, forceReadableName: false, Node.InternalMode.Disabled);
			return;
		}
		_previewManager.editorPreviewMode = true;
		if (PreviewSafety != null)
		{
			PreviewSafety.PrepareCharacter(_previewManager);
		}
		_previewManager.SetProcess(enable: false);
		_previewManager.SetPhysicsProcess(enable: false);
		_context.StageRoot.AddChild(_previewManager, forceReadableName: false, Node.InternalMode.Disabled);
		if (PreviewSafety != null)
		{
			PreviewSafety.TrackPreviewRoot(_previewManager);
		}
		_previewManager.broad.Visible = true;
		_previewManager.broad.MouseFilter = Control.MouseFilterEnum.Pass;
		_previewManager.broadCastLabel.Visible = false;
		_textEditor = new TextEdit
		{
			PlaceholderText = "在这里直接编辑游戏广播文字或 Localization Key",
			WrapMode = TextEdit.LineWrappingMode.Boundary,
			ScrollFitContentHeight = true,
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_textEditor.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_textEditor.OffsetLeft = 44f;
		_textEditor.OffsetTop = 8f;
		_textEditor.OffsetRight = -44f;
		_textEditor.OffsetBottom = -38f;
		_textEditor.AddThemeColorOverride("font_color", new Color("ead8a1"));
		_textEditor.AddThemeColorOverride("caret_color", new Color("fff5cf"));
		_textEditor.AddThemeFontSizeOverride("font_size", 24);
		_textEditor.AddThemeConstantOverride("outline_size", 4);
		_textEditor.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		_textEditor.AddThemeStyleboxOverride("focus", new StyleBoxFlat
		{
			BgColor = new Color("1b160c33"),
			BorderColor = new Color("ead8a1aa"),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1
		});
		_previewManager.broad.AddChild(_textEditor, forceReadableName: false, Node.InternalMode.Disabled);
		_context.PropertyBinding.BindText(_textEditor, _config, "broadCastString", UpdateLocalizedPreview);
		_resolvedLabel = new Label
		{
			AnchorLeft = 0.04f,
			AnchorTop = 1f,
			AnchorRight = 0.96f,
			AnchorBottom = 1f,
			OffsetTop = -34f,
			OffsetBottom = -6f,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color("b9d39e")
		};
		_previewManager.broad.AddChild(_resolvedLabel, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void MountResourceShelf()
	{
		HBoxContainer hBoxContainer = new HBoxContainer();
		Label label = new Label
		{
			Text = "游戏广播条",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", 18);
		hBoxContainer.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = " Broadcast ",
			Modulate = new Color("9ccf7a")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(new Label
		{
			Text = "文字就在游戏广播条原位编辑；-1 秒表示常驻。",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Modulate = new Color("9aa894")
		}, forceReadableName: false, Node.InternalMode.Disabled);
		_characterCountLabel = new Label();
		_context.ShelfRoot.AddChild(_characterCountLabel, forceReadableName: false, Node.InternalMode.Disabled);
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		hBoxContainer2.AddChild(new Label
		{
			Text = "广播持续时间",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		}, forceReadableName: false, Node.InternalMode.Disabled);
		SpinBox durationSpin = new SpinBox
		{
			MinValue = -1.0,
			MaxValue = 120.0,
			Step = 0.1,
			CustomMinimumSize = new Vector2(108f, 0f),
			FocusMode = Control.FocusModeEnum.All
		};
		_context.PropertyBinding.BindNumber(durationSpin, _config, "broadCastTime", UpdateDurationPreview);
		hBoxContainer2.AddChild(durationSpin, forceReadableName: false, Node.InternalMode.Disabled);
		_context.ShelfRoot.AddChild(hBoxContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		Button button = new Button
		{
			Text = "设为常驻（手动结束）"
		};
		button.Pressed += delegate
		{
			_context.PropertyBinding.SetValue(_config, "broadCastTime", -1.0, "设置广播常驻");
			durationSpin.Value = -1.0;
			UpdateDurationPreview();
		};
		_context.ShelfRoot.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
		Button button2 = new Button
		{
			Text = "打开当前 Mod 多语言表"
		};
		button2.Pressed += OpenLocalizationResource;
		_context.ShelfRoot.AddChild(button2, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void MountDurationTimeline()
	{
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_durationLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center
		};
		vBoxContainer.AddChild(_durationLabel, forceReadableName: false, Node.InternalMode.Disabled);
		HSlider hSlider = new HSlider
		{
			MinValue = -1.0,
			MaxValue = 120.0,
			Step = 0.1,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_context.PropertyBinding.BindNumber(hSlider, _config, "broadCastTime", UpdateDurationPreview);
		vBoxContainer.AddChild(hSlider, forceReadableName: false, Node.InternalMode.Disabled);
		_context.TimelineRoot.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private void UpdateLocalizedPreview()
	{
		if (GodotObject.IsInstanceValid(_config))
		{
			string text = _config.broadCastString ?? "";
			string text2 = ResolveLocalizedText(text);
			if (text2.Length > 4000)
			{
				text2 = text2.Substring(0, 4000) + "…";
			}
			if (GodotObject.IsInstanceValid(_resolvedLabel))
			{
				_resolvedLabel.Text = (string.Equals(text, text2, StringComparison.Ordinal) ? "原文显示" : ("当前语言预览：" + text2));
			}
			if (GodotObject.IsInstanceValid(_characterCountLabel))
			{
				_characterCountLabel.Text = $"字符数：{text.Length}";
				_characterCountLabel.Modulate = ((text.Length > 4000) ? new Color("e9a95f") : Colors.White);
			}
		}
	}

	private string ResolveLocalizedText(string source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return "";
		}
		string currentModProjectPath = GetCurrentModProjectPath();
		if (!string.IsNullOrWhiteSpace(currentModProjectPath) && XWLocalizationTable.LoadFromProject(currentModProjectPath).Entries.TryGetValue(source, out var value))
		{
			string locale = TranslationServer.GetLocale();
			if (value.Values.TryGetValue(locale, out var value2) && !string.IsNullOrWhiteSpace(value2))
			{
				return value2;
			}
			string key = (locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh_CN" : "en_US");
			if (value.Values.TryGetValue(key, out var value3) && !string.IsNullOrWhiteSpace(value3))
			{
				return value3;
			}
		}
		string text = TranslationServer.Translate(source);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return source;
	}

	private void OpenLocalizationResource()
	{
		string currentModProjectPath = GetCurrentModProjectPath();
		if (string.IsNullOrWhiteSpace(currentModProjectPath))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开一个 Mod 工程，再编辑多语言表");
			return;
		}
		string path = Path.Combine(currentModProjectPath, "Localization");
		string[] array = (Directory.Exists(path) ? Directory.GetFiles(path, "*.csv", SearchOption.TopDirectoryOnly) : Array.Empty<string>());
		if (array.Length == 0)
		{
			XWEditorInterface.Instance?.ShowToast("当前 Mod 尚未创建 Localization CSV");
			return;
		}
		Array.Sort(array, (IComparer<string>?)StringComparer.OrdinalIgnoreCase);
		new XWFileSystemExtensionLocalizationCsvMethod().Execute(new XWFileSystemTreeItemData(array[0], default(Variant), pIsFolder: false));
	}

	private static string GetCurrentModProjectPath()
	{
		if (!(XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel))
		{
			return "";
		}
		return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
	}

	private void SetupTransport()
	{
		Node node = _context.TimelineRoot?.GetParent();
		_readyButton = node?.GetNodeOrNull<Button>("Transport/ReadyButton");
		_battleButton = node?.GetNodeOrNull<Button>("Transport/BattleButton");
		_settleButton = node?.GetNodeOrNull<Button>("Transport/SettleButton");
		_scrubber = node?.GetNodeOrNull<HSlider>("Transport/Scrubber");
		_timeLabel = node?.GetNodeOrNull<Label>("Header/TimeLabel");
		if (GodotObject.IsInstanceValid(_readyButton))
		{
			_readyButton.Text = "⏮ 重播";
			_readyPressed = delegate
			{
				SetPreviewTime(0.0);
			};
			_readyButton.Pressed += _readyPressed;
		}
		if (GodotObject.IsInstanceValid(_battleButton))
		{
			_battleButton.Text = "▶ 播放";
			_battlePressed = TogglePlayback;
			_battleButton.Pressed += _battlePressed;
		}
		if (GodotObject.IsInstanceValid(_settleButton))
		{
			_settleButton.Text = "■ 结束";
			_settlePressed = delegate
			{
				StopPlayback();
				SetPreviewTime(PreviewDuration);
			};
			_settleButton.Pressed += _settlePressed;
		}
		if (GodotObject.IsInstanceValid(_scrubber))
		{
			_scrubber.Step = 0.05;
			_scrubberChanged = delegate(double value)
			{
				if (!_syncingTransport)
				{
					SetPreviewTime(value);
				}
			};
			_scrubber.ValueChanged += _scrubberChanged;
		}
		_previewTimer = new Timer
		{
			WaitTime = 0.05,
			OneShot = false
		};
		_previewTimer.Timeout += OnPreviewTick;
		_context.TimelineRoot.AddChild(_previewTimer, forceReadableName: false, Node.InternalMode.Disabled);
		if (PreviewSafety != null)
		{
			PreviewSafety.TrackTimer(_previewTimer);
		}
	}

	private void TogglePlayback()
	{
		if (!GodotObject.IsInstanceValid(_previewTimer))
		{
			return;
		}
		if (_previewTimer.IsStopped())
		{
			if (PreviewTime >= PreviewDuration)
			{
				SetPreviewTime(0.0);
			}
			_previewTimer.Start();
			_battleButton.Text = "⏸ 暂停";
		}
		else
		{
			StopPlayback();
		}
	}

	private void OnPreviewTick()
	{
		AdvancePreview(_previewTimer?.WaitTime ?? 0.05);
	}

	public void AdvancePreview(double delta)
	{
		double num = PreviewTime + Math.Max(0.0, delta);
		if (num >= PreviewDuration)
		{
			num = PreviewDuration;
			StopPlayback();
		}
		SetPreviewTime(num);
	}

	private void SetPreviewTime(double value)
	{
		double previewDuration = PreviewDuration;
		PreviewTime = Math.Clamp(double.IsFinite(value) ? value : 0.0, 0.0, previewDuration);
		_syncingTransport = true;
		if (GodotObject.IsInstanceValid(_scrubber))
		{
			_scrubber.MaxValue = previewDuration;
			_scrubber.Value = PreviewTime;
		}
		_syncingTransport = false;
		if (GodotObject.IsInstanceValid(_timeLabel))
		{
			_timeLabel.Text = $"{PreviewTime:00.0} / {previewDuration:00.0}s";
		}
		if (GodotObject.IsInstanceValid(_previewManager?.broad))
		{
			float a = ((previewDuration <= 0.0) ? 1f : (1f - Mathf.Clamp((float)(PreviewTime / previewDuration - 0.8199999928474426) / 0.18f, 0f, 0.65f)));
			_previewManager.broad.Modulate = new Color(1f, 1f, 1f, a);
		}
	}

	private void UpdateDurationPreview()
	{
		if (GodotObject.IsInstanceValid(_durationLabel))
		{
			_durationLabel.Text = ((_config.broadCastTime < 0.0) ? "广播常驻 · 手动结束" : $"广播持续 {_config.broadCastTime:0.0} 秒");
		}
		SetPreviewTime(Math.Min(PreviewTime, PreviewDuration));
	}

	private void StopPlayback()
	{
		_previewTimer?.Stop();
		if (GodotObject.IsInstanceValid(_battleButton))
		{
			_battleButton.Text = "▶ 播放";
		}
	}

	private void DisconnectTransport()
	{
		if (GodotObject.IsInstanceValid(_previewTimer))
		{
			_previewTimer.Stop();
			_previewTimer.Timeout -= OnPreviewTick;
		}
		if (GodotObject.IsInstanceValid(_readyButton) && _readyPressed != null)
		{
			_readyButton.Pressed -= _readyPressed;
		}
		if (GodotObject.IsInstanceValid(_battleButton) && _battlePressed != null)
		{
			_battleButton.Pressed -= _battlePressed;
		}
		if (GodotObject.IsInstanceValid(_settleButton) && _settlePressed != null)
		{
			_settleButton.Pressed -= _settlePressed;
		}
		if (GodotObject.IsInstanceValid(_scrubber) && _scrubberChanged != null)
		{
			_scrubber.ValueChanged -= _scrubberChanged;
		}
		_previewTimer = null;
		_readyButton = null;
		_battleButton = null;
		_settleButton = null;
		_scrubber = null;
		_timeLabel = null;
		_readyPressed = null;
		_battlePressed = null;
		_settlePressed = null;
		_scrubberChanged = null;
	}

	private static void ClearChildren(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}
		foreach (Node child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.QueueFree();
		}
	}
}
