using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAudioVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string AudioStreamPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAudioStreamPreview.tscn";

	private const string BgmAudioBindingsPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWBgmAudioBindingsPreview.tscn";

	private static PackedScene _audioStreamPreviewScene;

	private static PackedScene _bgmAudioBindingsPreviewScene;

	private AudioStreamPlayer _audioPreviewPlayer;

	private AudioStreamPlayer _bgmMainPreviewPlayer;

	private AudioStreamPlayer _bgmDrumsPreviewPlayer;

	private HSlider _bgmZombieCountSlider;

	private Label _bgmRuntimeStatus;

	private bool _bgmBattlePreviewActive;

	private XWVisualPropertyBinding _bgmPropertyBinding;

	private XWGameplayResourcePickerWindow _bgmAudioSelector;

	private string _bgmAudioSelectorField = "";

	private bool _bgmRefreshQueued;

	public override void _Ready()
	{
		base._Ready();
		EnableVisibilityGatedProcessing();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		_bgmPropertyBinding?.Dispose();
		_bgmPropertyBinding = null;
		StopAudioPreview();
		StopBgmRuntimePreview();
		Resource currentResource = base.CurrentResource;
		if (GodotObject.IsInstanceValid(currentResource))
		{
			if (TryReadAudioStream(currentResource, out var stream))
			{
				RenderAudioPreview(stream);
				RenderAudioTimeline(stream);
			}
			if (currentResource is TowerDefenseBackgroundMusicConfig bgm)
			{
				RenderBgmAudioSelector(bgm);
				RenderBgmBindingGraph(bgm);
			}
			else
			{
				RenderBgmBindingGraph(currentResource);
			}
			RenderAudioReferences(currentResource, stream);
		}
	}

	public override void _ExitTree()
	{
		_bgmPropertyBinding?.Dispose();
		_bgmPropertyBinding = null;
		StopAudioPreview();
		StopBgmRuntimePreview();
		if (GodotObject.IsInstanceValid(_bgmAudioSelector))
		{
			_bgmAudioSelector.Dismiss();
		}
		if (GodotObject.IsInstanceValid(_audioPreviewPlayer))
		{
			_audioPreviewPlayer.QueueFree();
		}
		base._ExitTree();
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (resource?.GetType() != typeof(TowerDefenseBackgroundMusicConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_bgmBattlePreviewActive && base.CurrentResource is TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig && GodotObject.IsInstanceValid(_bgmDrumsPreviewPlayer))
		{
			float num = Math.Max(0, towerDefenseBackgroundMusicConfig.drumsZombieThreshold);
			float to = (((float)(_bgmZombieCountSlider?.Value ?? 0.0) >= num) ? 1f : 0f);
			float num2 = Mathf.DbToLinear(_bgmDrumsPreviewPlayer.VolumeDb);
			float num3 = Mathf.Max(0.01f, towerDefenseBackgroundMusicConfig.drumsFadeSpeed);
			float weight = 1f - Mathf.Exp((0f - num3) * (float)Math.Max(0.0, delta));
			float num4 = Mathf.Lerp(num2, to, weight);
			_bgmDrumsPreviewPlayer.VolumeDb = ((num4 <= 0.0001f) ? (-80f) : Mathf.LinearToDb(num4));
			UpdateBgmRuntimeStatus(towerDefenseBackgroundMusicConfig, num4);
		}
	}

	private void RenderAudioPreview(AudioStream stream)
	{
		EnsureAudioPreviewPlayer();
		AddPreviewRow("音频类型: " + stream.GetType().Name);
		AddPreviewRow("播放长度: " + FormatSeconds(stream.GetLength()));
		AddPreviewRow("循环: " + ReadBooleanProperty(stream, "loop", "Loop"));
		AddPreviewRow("音量: " + ReadNamedValue(stream, "volume", "VolumeDb", "volumeDb"));
		AddPreviewRow("淡入淡出: " + ReadNamedValue(stream, "fade", "fadeIn", "fadeOut"));
		if (base.CanvasGrid == null)
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_audioStreamPreviewScene == null)
		{
			_audioStreamPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWAudioStreamPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _audioStreamPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			vBoxContainer.GetNode<Button>("Controls/PlayButton").Pressed += PlayAudioPreview;
			vBoxContainer.GetNode<Button>("Controls/StopButton").Pressed += StopAudioPreview;
			vBoxContainer.GetNode<Label>("Controls/DurationLabel").Text = "时长 " + FormatSeconds(stream.GetLength());
			HBoxContainer node = vBoxContainer.GetNode<HBoxContainer>("Waveform");
			int num = 40;
			double num2 = Math.Max(stream.GetLength(), 1.0);
			for (int i = 0; i < num; i++)
			{
				float num3 = (float)i / (float)Math.Max(1, num - 1);
				float y = 12f + 52f * (0.35f + 0.65f * Mathf.Abs(Mathf.Sin(num3 * ((float)Math.PI * 2f) * 2.75f)));
				ColorRect node2 = new ColorRect
				{
					Color = new Color(0.32f, 0.62f, 0.95f, 0.85f),
					CustomMinimumSize = new Vector2(5f, y),
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					SizeFlagsVertical = SizeFlags.ShrinkEnd,
					TooltipText = (FormatSeconds(num2 * (double)num3) ?? "")
				};
				node.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void RenderAudioTimeline(AudioStream stream)
	{
		double num = Math.Max(0.0, stream.GetLength());
		AddTimelineRow("播放时间线: 0.00s -> " + FormatSeconds(num));
		AddTimelineRow("循环段: " + ReadBooleanProperty(stream, "loop", "Loop"));
		AddTimelineRow("淡入淡出: " + ReadNamedValue(stream, "fade", "fadeIn", "fadeOut"));
		AddTimelineRow("BGM 切换点: 由 BGM 配置的 entry / flag1 / drums / win 决定");
		AddWaveformMarker("起点", 0.0, num);
		AddWaveformMarker("中点", num * 0.5, num);
		AddWaveformMarker("终点", num, num);
	}

	private void RenderBgmBindingGraph(TowerDefenseBackgroundMusicConfig bgm)
	{
		AddGraphRow("BGM 段落绑定");
		AddGraphRow("entry -> " + FormatTextValue(bgm.entry));
		AddGraphRow("flag1 -> " + FormatTextValue(bgm.flag1));
		AddGraphRow("drums -> " + FormatTextValue(bgm.drums));
		AddGraphRow("win -> " + FormatTextValue(bgm.win));
		AddGraphRow("translate -> " + FormatTextValue(bgm.translate));
		AddPreviewRow("BGM 主段: " + FormatTextValue(bgm.entry));
		AddPreviewRow("BGM 鼓点: " + FormatTextValue(bgm.drums));
		AddReferenceRow("音频引用 entry -> " + FormatTextValue(bgm.entry));
		AddReferenceRow("音频引用 flag1 -> " + FormatTextValue(bgm.flag1));
		AddReferenceRow("音频引用 drums -> " + FormatTextValue(bgm.drums));
		AddReferenceRow("音频引用 win -> " + FormatTextValue(bgm.win));
	}

	private void RenderBgmAudioSelector(TowerDefenseBackgroundMusicConfig bgm)
	{
		if (base.CanvasGrid != null && GodotObject.IsInstanceValid(bgm))
		{
			base.CanvasGrid.Columns = 1;
			if (_bgmAudioBindingsPreviewScene == null)
			{
				_bgmAudioBindingsPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWBgmAudioBindingsPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _bgmAudioBindingsPreviewScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				_bgmPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnBgmPropertyEdited);
				_bgmPropertyBinding.BindText(vBoxContainer.GetNode<LineEdit>("%ResourceNameEdit"), bgm, "resource_name", RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
				_bgmPropertyBinding.BindToggle(vBoxContainer.GetNode<CheckButton>("%LocalToSceneCheck"), bgm, "resource_local_to_scene", RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
				_bgmPropertyBinding.BindText(vBoxContainer.GetNode<LineEdit>("%TranslateEdit"), bgm, "translate", RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
				_bgmPropertyBinding.BindNumber(vBoxContainer.GetNode<SpinBox>("%DrumsZombieThreshold"), bgm, "drumsZombieThreshold", RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
				_bgmPropertyBinding.BindNumber(vBoxContainer.GetNode<SpinBox>("%DrumsFadeSpeed"), bgm, "drumsFadeSpeed", RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
				_bgmPropertyBinding.BindNumber(vBoxContainer.GetNode<SpinBox>("%DrumsCheckInterval"), bgm, "drumsCheckInterval", RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
				BindBgmAudioField(vBoxContainer, "EntryRow", "entry", bgm);
				BindBgmAudioField(vBoxContainer, "Flag1Row", "flag1", bgm);
				BindBgmAudioField(vBoxContainer, "DrumsRow", "drums", bgm);
				BindBgmAudioField(vBoxContainer, "WinRow", "win", bgm);
				ConfigureBgmRuntimePreview(vBoxContainer.GetNode<PanelContainer>("BgmRuntimeMixPreview"), bgm);
			}
		}
	}

	private void BindBgmAudioField(Node root, string rowPath, string fieldName, TowerDefenseBackgroundMusicConfig bgm)
	{
		LineEdit edit = root.GetNode<LineEdit>(rowPath + "/Value");
		_bgmPropertyBinding.BindText(edit, bgm, fieldName, RefreshBgmDirectPreview, this, "RefreshBgmFromHistory");
		root.GetNode<Button>(rowPath + "/PickButton").Pressed += delegate
		{
			ShowBgmAudioSelector(fieldName);
		};
		root.GetNode<Button>(rowPath + "/PreviewButton").Pressed += delegate
		{
			PreviewBgmAudioKey(edit.Text);
		};
	}

	private void ConfigureBgmRuntimePreview(PanelContainer panel, TowerDefenseBackgroundMusicConfig bgm)
	{
		EnsureBgmPreviewPlayers();
		if (GodotObject.IsInstanceValid(panel))
		{
			Button node = panel.GetNode<Button>("Layout/Controls/EntryButton");
			Button node2 = panel.GetNode<Button>("Layout/Controls/BattleButton");
			Button node3 = panel.GetNode<Button>("Layout/Controls/WinButton");
			Button node4 = panel.GetNode<Button>("Layout/Controls/StopButton");
			node.Pressed += delegate
			{
				PlayBgmMainSegment(bgm.entry, "entry");
			};
			node2.Pressed += delegate
			{
				StartBgmBattleMix(bgm);
			};
			node3.Pressed += delegate
			{
				PlayBgmMainSegment(bgm.win, "win");
			};
			node4.Pressed += StopBgmRuntimePreview;
			_bgmZombieCountSlider = panel.GetNode<HSlider>("Layout/ZombieRow/CountSlider");
			_bgmZombieCountSlider.MaxValue = Math.Max(30, bgm.drumsZombieThreshold * 2);
			_bgmZombieCountSlider.Value = 0.0;
			Label zombieValue = panel.GetNode<Label>("Layout/ZombieRow/CountValue");
			_bgmZombieCountSlider.ValueChanged += delegate(double value)
			{
				zombieValue.Text = ((int)value).ToString();
			};
			_bgmRuntimeStatus = panel.GetNode<Label>("Layout/Status");
			_bgmRuntimeStatus.Text = $"鼓点触发阈值 {bgm.drumsZombieThreshold}，渐入速度 {bgm.drumsFadeSpeed:0.##}";
		}
	}

	private void PreviewBgmAudioKey(string audioKey)
	{
		if (!TryResolveAudioStream(audioKey, out var stream))
		{
			XWEditorInterface.Instance?.ShowToast("无法解析音频 key: " + FormatTextValue(audioKey));
			return;
		}
		EnsureBgmPreviewPlayers();
		StopBgmRuntimePreview();
		_bgmMainPreviewPlayer.Stream = stream;
		_bgmMainPreviewPlayer.Play();
		if (GodotObject.IsInstanceValid(_bgmRuntimeStatus))
		{
			_bgmRuntimeStatus.Text = "正在试听: " + audioKey + "    " + FormatSeconds(stream.GetLength());
		}
	}

	private void PlayBgmMainSegment(string audioKey, string segmentName)
	{
		if (!TryResolveAudioStream(audioKey, out var stream))
		{
			XWEditorInterface.Instance?.ShowToast("BGM " + segmentName + " 未绑定可用音频: " + FormatTextValue(audioKey));
			return;
		}
		EnsureBgmPreviewPlayers();
		StopBgmRuntimePreview();
		_bgmMainPreviewPlayer.Stream = stream;
		_bgmMainPreviewPlayer.Play();
		_bgmRuntimeStatus.Text = "正在播放 " + segmentName + ": " + audioKey;
	}

	private void StartBgmBattleMix(TowerDefenseBackgroundMusicConfig bgm)
	{
		if (!TryResolveAudioStream(bgm.flag1, out var stream))
		{
			XWEditorInterface.Instance?.ShowToast("战斗主轨 flag1 不可用: " + FormatTextValue(bgm.flag1));
			return;
		}
		EnsureBgmPreviewPlayers();
		StopBgmRuntimePreview();
		_bgmMainPreviewPlayer.Stream = stream;
		_bgmMainPreviewPlayer.Play();
		if (TryResolveAudioStream(bgm.drums, out var stream2))
		{
			_bgmDrumsPreviewPlayer.Stream = stream2;
			_bgmDrumsPreviewPlayer.VolumeDb = -80f;
			_bgmDrumsPreviewPlayer.Play();
		}
		_bgmBattlePreviewActive = true;
		RequestVisibilityGatedProcessing(requested: true);
		UpdateBgmRuntimeStatus(bgm, 0f);
	}

	private void StopBgmRuntimePreview()
	{
		_bgmBattlePreviewActive = false;
		RequestVisibilityGatedProcessing(requested: false);
		if (GodotObject.IsInstanceValid(_bgmMainPreviewPlayer))
		{
			_bgmMainPreviewPlayer.Stop();
			_bgmMainPreviewPlayer.Stream = null;
		}
		if (GodotObject.IsInstanceValid(_bgmDrumsPreviewPlayer))
		{
			_bgmDrumsPreviewPlayer.Stop();
			_bgmDrumsPreviewPlayer.Stream = null;
		}
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		if (!visible)
		{
			StopAudioPreview();
			StopBgmRuntimePreview();
		}
	}

	private void EnsureBgmPreviewPlayers()
	{
		if (!GodotObject.IsInstanceValid(_bgmMainPreviewPlayer))
		{
			_bgmMainPreviewPlayer = new AudioStreamPlayer
			{
				Bus = "Music"
			};
			AddChild(_bgmMainPreviewPlayer, forceReadableName: false, InternalMode.Disabled);
		}
		if (!GodotObject.IsInstanceValid(_bgmDrumsPreviewPlayer))
		{
			_bgmDrumsPreviewPlayer = new AudioStreamPlayer
			{
				Bus = "Music"
			};
			AddChild(_bgmDrumsPreviewPlayer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private bool TryResolveAudioStream(string key, out AudioStream stream)
	{
		stream = null;
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(ResourceManager.Instance) && ResourceManager.Instance.AUDIOS.TryGetValue(key, out var value) && value is AudioStream audioStream)
		{
			stream = audioStream;
			return true;
		}
		EnsureBgmAudioSelectorWindow();
		if (!GodotObject.IsInstanceValid(_bgmAudioSelector))
		{
			return false;
		}
		stream = _bgmAudioSelector.LoadAudioStream(key);
		return GodotObject.IsInstanceValid(stream);
	}

	private void UpdateBgmRuntimeStatus(TowerDefenseBackgroundMusicConfig bgm, float drumsVolume)
	{
		if (GodotObject.IsInstanceValid(_bgmRuntimeStatus))
		{
			int value = (int)(_bgmZombieCountSlider?.Value ?? 0.0);
			_bgmRuntimeStatus.Text = $"战斗混音: {bgm.flag1} + {bgm.drums}    僵尸 {value}/{bgm.drumsZombieThreshold}    鼓点 {drumsVolume * 100f:0}%";
		}
	}

	private void ShowBgmAudioSelector(string fieldName)
	{
		EnsureBgmAudioSelectorWindow();
		if (GodotObject.IsInstanceValid(_bgmAudioSelector))
		{
			_bgmAudioSelectorField = fieldName ?? "";
			_bgmAudioSelector.Open(XWGameplayResourceKind.Audio, GetBgmAudioFieldValue(_bgmAudioSelectorField), SelectBgmAudioChoice, (XWGameplayResourceChoice choice) => choice.Kind == XWGameplayResourceKind.Audio, lockKind: true, "BGM 音频图鉴 · " + _bgmAudioSelectorField);
		}
	}

	private void EnsureBgmAudioSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_bgmAudioSelector))
		{
			_bgmAudioSelector = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_bgmAudioSelector))
			{
				AddChild(_bgmAudioSelector, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void SelectBgmAudioChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null) && choice.Kind == XWGameplayResourceKind.Audio)
		{
			string persistedAudioKey = XWGameplayResourcePickerWindow.GetPersistedAudioKey(choice);
			if (!string.IsNullOrWhiteSpace(persistedAudioKey))
			{
				SetBgmAudioField(_bgmAudioSelectorField, persistedAudioKey, refresh: true);
			}
		}
	}

	private string GetBgmAudioFieldValue(string fieldName)
	{
		if (!(base.CurrentResource is TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig))
		{
			return "";
		}
		return fieldName switch
		{
			"entry" => towerDefenseBackgroundMusicConfig.entry, 
			"flag1" => towerDefenseBackgroundMusicConfig.flag1, 
			"drums" => towerDefenseBackgroundMusicConfig.drums, 
			"win" => towerDefenseBackgroundMusicConfig.win, 
			_ => "", 
		};
	}

	private void SetBgmAudioField(string fieldName, string value, bool refresh)
	{
		if (!(base.CurrentResource is TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig) || string.IsNullOrWhiteSpace(fieldName))
		{
			return;
		}
		string text = value ?? "";
		bool flag;
		switch (fieldName)
		{
		case "entry":
		case "flag1":
		case "drums":
		case "win":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			if (_bgmPropertyBinding != null)
			{
				_bgmPropertyBinding.SetValue(towerDefenseBackgroundMusicConfig, fieldName, text, "更换 BGM " + fieldName, this, "RefreshBgmFromHistory");
			}
			else
			{
				towerDefenseBackgroundMusicConfig.Set(fieldName, text);
				NotifyCurrentResourceEdited();
			}
			if (refresh)
			{
				RefreshBgmFromHistory();
			}
			else
			{
				RefreshBgmDirectPreview();
			}
		}
	}

	private void OnBgmPropertyEdited(bool committed)
	{
		NotifyCurrentResourceEdited();
		RefreshBgmDirectPreview();
	}

	private void RefreshBgmDirectPreview()
	{
		if (base.CurrentResource is TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig)
		{
			if (GodotObject.IsInstanceValid(_bgmZombieCountSlider))
			{
				_bgmZombieCountSlider.MaxValue = Math.Max(30, towerDefenseBackgroundMusicConfig.drumsZombieThreshold * 2);
			}
			UpdateBgmRuntimeStatus(towerDefenseBackgroundMusicConfig, GodotObject.IsInstanceValid(_bgmDrumsPreviewPlayer) ? Mathf.DbToLinear(_bgmDrumsPreviewPlayer.VolumeDb) : 0f);
		}
	}

	public void RefreshBgmFromHistory()
	{
		if (!_bgmRefreshQueued && base.CurrentResource is TowerDefenseBackgroundMusicConfig)
		{
			_bgmRefreshQueued = true;
			CallDeferred("ReloadBgmEditorAfterHistory");
		}
	}

	public void ReloadBgmEditorAfterHistory()
	{
		_bgmRefreshQueued = false;
		if (base.CurrentResource is TowerDefenseBackgroundMusicConfig)
		{
			LoadResource(base.CurrentResource, base.CurrentResourcePath, base.CurrentDescriptor, base.CurrentEditContext);
		}
	}

	private void RenderBgmBindingGraph(Resource resource)
	{
		List<(string, string)> list = ExtractAudioLikeProperties(resource);
		if (list.Count == 0)
		{
			return;
		}
		AddGraphRow("音频/BGM 属性绑定");
		foreach (var item in list)
		{
			AddGraphRow(item.Item1 + " -> " + item.Item2);
			if (IsReferenceLike(item.Item1))
			{
				AddReferenceRow("音频引用 " + item.Item1 + " -> " + item.Item2);
			}
		}
	}

	private void RenderAudioReferences(Resource resource, AudioStream stream)
	{
		if (!string.IsNullOrWhiteSpace(base.CurrentResourcePath))
		{
			AddReferenceRow("资源文件 -> " + base.CurrentResourcePath);
		}
		if (GodotObject.IsInstanceValid(stream))
		{
			string resourcePath = stream.ResourcePath;
			AddReferenceRow(string.IsNullOrWhiteSpace(resourcePath) ? "音频流 -> 内嵌资源" : ("音频流 -> " + resourcePath));
		}
		foreach (var item in ExtractAudioLikeProperties(resource))
		{
			if (IsReferenceLike(item.Name))
			{
				AddReferenceRow("属性引用 " + item.Name + " -> " + item.Value);
			}
		}
	}

	private void PlayAudioPreview()
	{
		if (TryReadAudioStream(base.CurrentResource, out var stream))
		{
			EnsureAudioPreviewPlayer();
			_audioPreviewPlayer.Stop();
			_audioPreviewPlayer.Stream = stream;
			_audioPreviewPlayer.Play();
		}
	}

	private void StopAudioPreview()
	{
		if (GodotObject.IsInstanceValid(_audioPreviewPlayer))
		{
			_audioPreviewPlayer.Stop();
			_audioPreviewPlayer.Stream = null;
		}
	}

	private void EnsureAudioPreviewPlayer()
	{
		if (!GodotObject.IsInstanceValid(_audioPreviewPlayer))
		{
			_audioPreviewPlayer = new AudioStreamPlayer();
			AddChild(_audioPreviewPlayer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private bool TryReadAudioStream(Resource resource, out AudioStream stream)
	{
		stream = null;
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		if (resource is AudioStream audioStream)
		{
			stream = audioStream;
			return true;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = ReadPropertyName(property);
			if (!string.IsNullOrWhiteSpace(text) && IsAudioLike(text))
			{
				Variant variant;
				try
				{
					variant = resource.Get(text);
				}
				catch
				{
					continue;
				}
				if (variant.VariantType == Variant.Type.Object && variant.AsGodotObject() is AudioStream audioStream2)
				{
					stream = audioStream2;
					return true;
				}
			}
		}
		return false;
	}

	private List<(string Name, string Value)> ExtractAudioLikeProperties(Resource resource)
	{
		List<(string, string)> list = new List<(string, string)>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = ReadPropertyName(property);
			if (!string.IsNullOrWhiteSpace(text) && IsAudioLike(text))
			{
				Variant value;
				try
				{
					value = resource.Get(text);
				}
				catch
				{
					continue;
				}
				list.Add((text, FormatVariant(value)));
			}
		}
		return list;
	}

	private void AddWaveformMarker(string label, double second, double totalLength)
	{
		double value = ((totalLength <= 0.0) ? 0.0 : Math.Clamp(second / totalLength * 100.0, 0.0, 100.0));
		AddTimelineRow($"{label}: {FormatSeconds(second)} ({value:0.#}%)");
	}

	private void AddTimelineRow(string text)
	{
		AddItemIfMissing(base.TimelineList, text);
	}

	private void AddPreviewRow(string text)
	{
		AddItemIfMissing(base.PreviewList, text);
	}

	private void AddGraphRow(string text)
	{
		AddItemIfMissing(base.GraphList, text);
	}

	private void AddReferenceRow(string text)
	{
		AddItemIfMissing(base.ReferenceList, text);
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (list == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			if (list.GetItemText(i) == text)
			{
				return;
			}
		}
		list.AddItem(text);
	}

	private static string ReadNamedValue(Resource resource, params string[] names)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未设置";
		}
		foreach (string text in names)
		{
			try
			{
				Variant value = resource.Get(text);
				if (value.VariantType != Variant.Type.Nil)
				{
					return FormatVariant(value);
				}
			}
			catch
			{
			}
		}
		return "未设置";
	}

	private static string ReadBooleanProperty(Resource resource, params string[] names)
	{
		string text = ReadNamedValue(resource, names);
		if (text == "True")
		{
			return "启用";
		}
		if (text == "False")
		{
			return "关闭";
		}
		return text;
	}

	private static string ReadPropertyName(Dictionary property)
	{
		if (property == null || !property.ContainsKey("name"))
		{
			return "";
		}
		return property["name"].AsString();
	}

	private static bool IsAudioLike(string name)
	{
		string text = name.ToLowerInvariant();
		if (!text.Contains("audio") && !text.Contains("sound") && !text.Contains("sfx") && !text.Contains("bgm") && !text.Contains("music") && !text.Contains("stream") && !text.Contains("volume") && !text.Contains("loop") && !text.Contains("fade"))
		{
			switch (text)
			{
			default:
				return text == "win";
			case "entry":
			case "flag1":
			case "drums":
				break;
			}
		}
		return true;
	}

	private static bool IsReferenceLike(string name)
	{
		string text = name.ToLowerInvariant();
		if (!text.Contains("audio") && !text.Contains("sound") && !text.Contains("sfx") && !text.Contains("bgm") && !text.Contains("music") && !text.Contains("stream"))
		{
			switch (text)
			{
			default:
				return text == "win";
			case "entry":
			case "flag1":
			case "drums":
				break;
			}
		}
		return true;
	}

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "True" : "False";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			case 4:
				goto IL_00b8;
			}
		}
		if (variantType != Variant.Type.StringName)
		{
			Variant.Type num = variantType - 24;
			if ((ulong)num <= 4uL)
			{
				switch ((int)num)
				{
				case 0:
					return FormatObject(value.AsGodotObject());
				case 4:
					return $"数组({value.AsGodotArray().Count})";
				case 3:
					return $"字典({value.AsGodotDictionary().Count})";
				}
			}
			return value.ToString();
		}
		goto IL_00b8;
		IL_00b8:
		return FormatTextValue(value.AsString());
	}

	private static string FormatObject(GodotObject obj)
	{
		if (!GodotObject.IsInstanceValid(obj))
		{
			return "空";
		}
		if (obj is Resource resource)
		{
			if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
			{
				return resource.ResourcePath;
			}
			if (!string.IsNullOrWhiteSpace(resource.ResourceName))
			{
				return resource.ResourceName;
			}
		}
		return obj.GetType().Name;
	}

	private static string FormatTextValue(string text)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "未设置";
	}

	private static string FormatSeconds(double seconds)
	{
		if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0.0)
		{
			return "未知";
		}
		return $"{seconds:0.00}s";
	}
}
