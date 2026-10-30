using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWNpcTalkPreviewWindow : Window
{
	private const string CrazyDaveScenePath = "res://Prefab/Npc/CrazyDave/NpcCrazyDave.tscn";

	private const string WeiWeiMiScenePath = "res://Prefab/Npc/WeiWeiMi/WeiWeiMi.tscn";

	private NpcTalkConfig _config;

	private int _index;

	private bool _autoPlaying;

	private NpcTalkControl _runtimeControl;

	private NpcBase _npc;

	private string _npcKey = "";

	private Label _titleLabel;

	private Label _statusLabel;

	private Label _emptyLabel;

	private Button _previousButton;

	private Button _nextButton;

	private Button _autoButton;

	private Timer _autoTimer;

	private AudioStreamPlayer _directAudioPlayer;

	private XWAspectScaledPreviewHost _previewHost;

	public XWAspectScaledPreviewHost ResponsivePreviewHost => _previewHost;

	public override void _Ready()
	{
		base.CloseRequested += ClosePreview;
		BindInterface();
		RefreshFromConfig();
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey { Pressed: not false, Echo: false, Keycode: var keycode } inputEventKey)
		{
			if ((keycode == Key.Space || keycode == Key.Enter || keycode == Key.Right) ? true : false)
			{
				ShowNext();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.Left)
			{
				ShowPrevious();
				GetViewport().SetInputAsHandled();
			}
			else if (inputEventKey.Keycode == Key.Escape)
			{
				ClosePreview();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	public void Preview(NpcTalkConfig config, int startIndex = 0)
	{
		_config = config;
		_index = Mathf.Max(0, startIndex);
		if (IsNodeReady())
		{
			RefreshFromConfig();
			PopupCenteredClamped(new Vector2I(1000, 680), 0.92f);
			GrabFocus();
		}
	}

	public void RefreshConfig(NpcTalkConfig config)
	{
		_config = config;
		if (IsNodeReady())
		{
			RefreshFromConfig();
		}
	}

	private void BindInterface()
	{
		_previewHost = GetNode<XWAspectScaledPreviewHost>("%GameCanvasCenter");
		_titleLabel = GetNode<Label>("Layout/Header/Title");
		_statusLabel = GetNode<Label>("Layout/Status");
		_previousButton = GetNode<Button>("Layout/Controls/PreviousButton");
		_nextButton = GetNode<Button>("Layout/Controls/NextButton");
		_autoButton = GetNode<Button>("Layout/Controls/AutoButton");
		Button node = GetNode<Button>("Layout/Controls/ReplayButton");
		SubViewportContainer node2 = GetNode<SubViewportContainer>("Layout/GameCanvasCenter/ViewportContainer");
		_runtimeControl = GetNode<NpcTalkControl>("%RuntimeNpcTalkControl");
		_emptyLabel = GetNode<Label>("Layout/GameCanvasCenter/ViewportContainer/Viewport/EmptyLabel");
		_autoTimer = GetNode<Timer>("AutoTimer");
		_directAudioPlayer = GetNode<AudioStreamPlayer>("DirectAudioPlayer");
		node2.GuiInput += delegate(InputEvent inputEvent)
		{
			if (inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				ShowNext();
			}
		};
		_previousButton.Pressed += ShowPrevious;
		_nextButton.Pressed += ShowNext;
		_autoButton.Pressed += ToggleAutoPlay;
		node.Pressed += ShowCurrent;
		_autoTimer.Timeout += AdvanceAutoPlay;
	}

	private void RefreshFromConfig()
	{
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (valueOrDefault == 0)
		{
			_index = 0;
			_emptyLabel.Visible = true;
			_statusLabel.Text = "对话列表为空";
			_previousButton.Disabled = true;
			_nextButton.Disabled = true;
			_autoButton.Disabled = true;
			ClearNpc();
		}
		else
		{
			_index = Mathf.Clamp(_index, 0, valueOrDefault - 1);
			_emptyLabel.Visible = false;
			_previousButton.Disabled = _index <= 0;
			_nextButton.Disabled = _index >= valueOrDefault - 1;
			_autoButton.Disabled = false;
			_titleLabel.Text = "NPC 对话运行预览  ·  " + EmptyToPlaceholder(_config.saveKey);
			ShowCurrent();
		}
	}

	private void ShowCurrent()
	{
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (valueOrDefault != 0 && _index >= 0 && _index < valueOrDefault)
		{
			NpcTalkBaseConfig npcTalkBaseConfig = _config.talkList[_index];
			if (!GodotObject.IsInstanceValid(npcTalkBaseConfig))
			{
				_statusLabel.Text = $"{_index + 1} / {valueOrDefault}    空对话条目";
				ScheduleAutoAdvance();
				return;
			}
			EnsureNpc(npcTalkBaseConfig.npc);
			ApplyTalkToNpc(npcTalkBaseConfig);
			_statusLabel.Text = $"{_index + 1} / {valueOrDefault}    NPC: {EmptyToPlaceholder(npcTalkBaseConfig.npc)}    动画: {EmptyToPlaceholder(npcTalkBaseConfig.anime)}    音频: {EmptyToPlaceholder(npcTalkBaseConfig.audio)}";
			_previousButton.Disabled = _index <= 0;
			_nextButton.Disabled = _index >= valueOrDefault - 1;
			ScheduleAutoAdvance();
		}
	}

	private void EnsureNpc(string npcKey)
	{
		npcKey = (string.IsNullOrWhiteSpace(npcKey) ? "CrazyDave" : npcKey);
		if (GodotObject.IsInstanceValid(_npc) && _npcKey == npcKey)
		{
			return;
		}
		ClearNpc();
		string path = ((!(npcKey == "WeiWeiMi")) ? "res://Prefab/Npc/CrazyDave/NpcCrazyDave.tscn" : "res://Prefab/Npc/WeiWeiMi/WeiWeiMi.tscn");
		_npc = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse)?.Instantiate<NpcBase>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(_npc))
		{
			_npcKey = npcKey;
			_npc.Name = "PreviewNpc";
			_npc.Position = Vector2.Zero;
			_npc.Scale = Vector2.One;
			ColorRect nodeOrNull = _npc.GetNodeOrNull<ColorRect>("CanvasLayer/ColorRect");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.MouseFilter = Control.MouseFilterEnum.Ignore;
			}
			_runtimeControl.AddNpc(_npc);
		}
	}

	private void ApplyTalkToNpc(NpcTalkBaseConfig talk)
	{
		if (GodotObject.IsInstanceValid(_npc))
		{
			Label nodeOrNull = _npc.GetNodeOrNull<Label>("%TalkLabel");
			TextureRect nodeOrNull2 = _npc.GetNodeOrNull<TextureRect>("%TalkBubble");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.Text = talk.text ?? "";
			}
			if (GodotObject.IsInstanceValid(nodeOrNull2))
			{
				nodeOrNull2.Visible = true;
				nodeOrNull2.Scale = Vector2.One;
			}
			if (GodotObject.IsInstanceValid(_npc.sprite) && !string.IsNullOrWhiteSpace(talk.anime))
			{
				_npc.sprite.SetAnimation(talk.anime);
			}
			if (talk is NpcTalkHandConfig hand)
			{
				_npc.Hand(hand);
			}
			PlayTalkAudio(talk.audio);
		}
	}

	private void PlayTalkAudio(string audioKey)
	{
		_directAudioPlayer.Stop();
		if (!string.IsNullOrWhiteSpace(audioKey))
		{
			if (GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(audioKey, AudioManagerEnum.TYPE.SFX, 0.0, once: true, pauseAlive: true);
			}
			else if (ResourceLoader.Exists(audioKey))
			{
				_directAudioPlayer.Stream = ResourceLoader.Load<AudioStream>(audioKey, null, ResourceLoader.CacheMode.Reuse);
				_directAudioPlayer.Play();
			}
		}
	}

	private void ShowPrevious()
	{
		if (_index > 0)
		{
			_index--;
			ShowCurrent();
		}
	}

	private void ShowNext()
	{
		int valueOrDefault = (_config?.talkList?.Count).GetValueOrDefault();
		if (_index + 1 >= valueOrDefault)
		{
			StopAutoPlay();
			return;
		}
		_index++;
		ShowCurrent();
	}

	private void ToggleAutoPlay()
	{
		if (_autoPlaying)
		{
			StopAutoPlay();
			return;
		}
		_autoPlaying = true;
		_index = 0;
		_autoButton.Text = "暂停播放";
		ShowCurrent();
	}

	private void ScheduleAutoAdvance()
	{
		if (_autoPlaying)
		{
			double num = 3.2;
			if (GodotObject.IsInstanceValid(_directAudioPlayer.Stream))
			{
				num = Mathf.Max(num, _directAudioPlayer.Stream.GetLength() + 0.35);
			}
			_autoTimer.Start(num);
		}
	}

	private void AdvanceAutoPlay()
	{
		if (_autoPlaying)
		{
			ShowNext();
		}
	}

	private void StopAutoPlay()
	{
		_autoPlaying = false;
		_autoTimer.Stop();
		_autoButton.Text = "播放整段";
	}

	private void ClearNpc()
	{
		if (GodotObject.IsInstanceValid(_npc))
		{
			_npc.GetParent()?.RemoveChild(_npc);
			_npc.QueueFree();
		}
		_npc = null;
		_npcKey = "";
	}

	private void ClosePreview()
	{
		StopAutoPlay();
		_directAudioPlayer?.Stop();
		Hide();
	}

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未设置";
	}
}
