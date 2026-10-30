using System;
using Godot;

[GlobalClass]
public partial class MenuDialogBase : DialogBoxBase
{
	private CheckBox fullScreenCheckBox;

	private CheckBox mobilePresetCheckBox;

	private HSlider musicSlider;

	private HSlider sfxSlider;

	private TextureButton backButton;

	private Label animeFrameRateLabel;

	private HSlider animeFrameRateSlider;

	private CheckBox gpuOptimizationCheckBox;

	private Label fullScreenLabel;

	private Label mobilePresetLabel;

	private int[] _frameRateOptions = Array.Empty<int>();

	public override void _Ready()
	{
		fullScreenCheckBox = GetNode<CheckBox>("%FullScreenCheckBox");
		mobilePresetCheckBox = GetNode<CheckBox>("%MobilePresetCheckBox");
		musicSlider = GetNode<HSlider>("%MusicHSlider");
		sfxSlider = GetNode<HSlider>("%SfxHSlider");
		backButton = GetNode<TextureButton>("%BackButton");
		animeFrameRateLabel = GetNode<Label>("%AnimeFrameRateLabel");
		animeFrameRateSlider = GetNode<HSlider>("%AnimeFrameRateSlider");
		gpuOptimizationCheckBox = GetNode<CheckBox>("%GpuOptimizationCheckBox");
		fullScreenLabel = GetNode<Label>("%FullScreenLabel");
		mobilePresetLabel = GetNode<Label>("%MobilePresetLabel");
		GetNode<CheckBox>("%FullScreenCheckBox").Pressed += FullScreenCheckBoxPressed;
		GetNode<CheckBox>("%MobilePresetCheckBox").Pressed += MobilePresetCheckBoxPressed;
		gpuOptimizationCheckBox.Pressed += GpuOptimizationCheckBoxPressed;
		GetNode<HSlider>("%MusicHSlider").DragEnded += MusicSliderRelease;
		GetNode<HSlider>("%MusicHSlider").DragStarted += SliderPressed;
		GetNode<HSlider>("%MusicHSlider").ValueChanged += MusicValueChanged;
		GetNode<HSlider>("%SfxHSlider").DragEnded += SfxSliderRelease;
		GetNode<HSlider>("%SfxHSlider").DragStarted += SliderPressed;
		GetNode<HSlider>("%SfxHSlider").ValueChanged += SFXValueChanged;
		GetNode<BaseButton>("%BackButton").Pressed += base.CloseDialog;
		base._Ready();
		Global.Instance.OnFrameRateOptionsChanged += RefreshFrameRateOptions;
		animeFrameRateSlider.DragEnded += AnimeFrameRateSliderDragEnded;
		animeFrameRateSlider.ValueChanged += AnimeFrameRateSliderValueChanged;
		RefreshFrameRateOptions();
		if (Global.Instance.isMobile)
		{
			fullScreenLabel.Visible = false;
		}
		AudioManager.Instance.AudioPlay("GraveButtonPress");
		musicSlider.CallDeferred("set_value", AudioManager.Instance.VolumGet(AudioManagerEnum.TYPE.MUSIC));
		sfxSlider.CallDeferred("set_value", AudioManager.Instance.VolumGet());
		fullScreenCheckBox.ButtonPressed = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
		mobilePresetCheckBox.ButtonPressed = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		gpuOptimizationCheckBox.ButtonPressed = Global.Instance.adobeAnimateRenderBackend == AdobeAnimateRenderBackend.GpuCrowd;
	}

	private void RefreshFrameRateOptions()
	{
		_frameRateOptions = Global.Instance.GetFrameRateOptions();
		if (_frameRateOptions.Length == 0)
		{
			_frameRateOptions = new int[1] { 60 };
		}
		animeFrameRateSlider.MaxValue = Math.Max(0, _frameRateOptions.Length - 1);
		animeFrameRateSlider.Value = FindFrameRateIndex(Global.Instance.animeFrameRate);
		UpdateFrameRateLabel();
	}

	private int FindFrameRateIndex(double value)
	{
		int num = 0;
		int num2 = _frameRateOptions.Length;
		while (num < num2)
		{
			int num3 = (num + num2) / 2;
			if ((double)_frameRateOptions[num3] < value)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return Mathf.Clamp(num, 0, _frameRateOptions.Length - 1);
	}

	private void AnimeFrameRateSliderValueChanged(double value)
	{
		int num = Mathf.Clamp((int)Math.Round(value), 0, _frameRateOptions.Length - 1);
		animeFrameRateLabel.Text = $"动画帧率:{_frameRateOptions[num]}";
	}

	private void AnimeFrameRateSliderDragEnded(bool valueChanged)
	{
		if (valueChanged)
		{
			int num = Mathf.Clamp((int)animeFrameRateSlider.Value, 0, _frameRateOptions.Length - 1);
			Global.Instance.animeFrameRate = _frameRateOptions[num];
			GameSaveManager.Instance.SetConfigValue("AnimeFrameRate", Global.Instance.animeFrameRate);
			UpdateFrameRateLabel();
		}
	}

	private void UpdateFrameRateLabel()
	{
		int num = FrameRatePolicy.NormalizeRefreshRate(Global.Instance.animeFrameRate);
		int effectiveAnimeFrameRate = Global.Instance.effectiveAnimeFrameRate;
		animeFrameRateLabel.Text = ((num == effectiveAnimeFrameRate) ? $"动画帧率:{num}" : $"动画帧率:{num}（当前{effectiveAnimeFrameRate}）");
	}

	private void GpuOptimizationCheckBoxPressed()
	{
		AdobeAnimateRenderBackend adobeAnimateRenderBackend = ((!gpuOptimizationCheckBox.ButtonPressed) ? AdobeAnimateRenderBackend.CpuPose : AdobeAnimateRenderBackend.GpuCrowd);
		Global.Instance.adobeAnimateRenderBackend = adobeAnimateRenderBackend;
		GameSaveManager.Instance.SetConfigValue("AdobeAnimateRenderBackend", (int)adobeAnimateRenderBackend);
		AudioManager.Instance.AudioPlay("ButtonPress");
	}

	public override void _ExitTree()
	{
		if (Global.Instance != null)
		{
			Global.Instance.OnFrameRateOptionsChanged -= RefreshFrameRateOptions;
		}
	}

	public void SliderPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
	}

	public void SfxSliderRelease(bool valueChanged)
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (valueChanged)
		{
			GameSaveManager.Instance.SetConfigValue("SfxVolum", sfxSlider.Value);
		}
	}

	public void MusicSliderRelease(bool valueChanged)
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (valueChanged)
		{
			GameSaveManager.Instance.SetConfigValue("MusicVolum", musicSlider.Value);
		}
	}

	public void MusicValueChanged(double value)
	{
		AudioManager.Instance.VolumSet(AudioManagerEnum.TYPE.MUSIC, value);
	}

	public void SFXValueChanged(double value)
	{
		AudioManager.Instance.VolumSet(AudioManagerEnum.TYPE.SFX, value);
	}

	public void FullScreenCheckBoxPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (fullScreenCheckBox.ButtonPressed)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
		GameSaveManager.Instance.SetConfigValue("FullScreen", fullScreenCheckBox.ButtonPressed);
	}

	public void MobilePresetCheckBoxPressed()
	{
		BattleEventBus.Instance.EmitUiSwitched(mobilePresetCheckBox.ButtonPressed);
		AudioManager.Instance.AudioPlay("ButtonPress");
		GameSaveManager.Instance.SetConfigValue("MobilePreset", mobilePresetCheckBox.ButtonPressed);
	}
}
