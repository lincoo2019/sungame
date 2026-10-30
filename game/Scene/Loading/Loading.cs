using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class Loading : Control
{
	private enum ModInstallConfirmation
	{
		Confirmed,
		Cancelled,
		Unavailable
	}

	private const string SodRollCapTexturePath = "res://Asset/Texture/GUI/General/Loading/SodRollCap.png";

	private const string LoadingFontPath = "res://Asset/Font/fzkt.ttf";

	private const string BootstrapAtlasProfilePath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres";

	private const string LoadingAnimationClip = "Idle";

	private const int LoadingAnimationCount = 5;

	private const float LoadingPresentationCompleteProgress = 0.95f;

	private const float MaxPresentationDeltaSeconds = 1f / 30f;

	private Label _loadLabel;

	private MainButton _offlineButton;

	private TextureProgressBar _progressBar;

	private Button _startButton;

	private MainButton _clearCacheButton;

	private DialogBoxChoose _clearCacheDialog;

	private Sprite2D _sodRollCap;

	private AdobeAnimateSpriteBase _loadBarSprout;

	private AdobeAnimateSpriteBase _loadBarSprout2;

	private AdobeAnimateSpriteBase _loadBarSprout3;

	private AdobeAnimateSpriteBase _loadBarSprout4;

	private AdobeAnimateSpriteBase _loadBarZombieHead;

	private float _percentage;

	private bool _isStart;

	private float _currentValue;

	private bool _loadCompleted;

	private bool _loadingPresentationCompleted;

	private static bool _startupModsLoaded = false;

	private static readonly SemaphoreSlim StartupModsGate = new SemaphoreSlim(1, 1);

	private bool _startupModFlowStarted;

	private bool _startupModsReady;

	private int _completedLoadingAnimationCount;

	private bool? _startupImportSuccess;

	private string _startupImportMessage = "";

	private XWModStartupReport _startupReport;

	public ulong LoadingFirstFramePresentedUsec { get; private set; }

	public ulong BootstrapFirstFramePresentedUsec { get; private set; }

	public ulong GameplayResourceLoadRequestedUsec { get; private set; }

	public override void _Ready()
	{
		StartupLoadDiagnostics.Mark("loading.ready.begin");
		_loadLabel = GetNode<Label>("%LoadLabel");
		_offlineButton = GetNode<MainButton>("%OfflineButton");
		_progressBar = GetNode<TextureProgressBar>("%ProgressBar");
		_startButton = GetNode<Button>("%StartButton");
		_clearCacheButton = GetNode<MainButton>("%ClearCacheButton");
		_sodRollCap = GetNode<Sprite2D>("%SodRollCap");
		EnsureSodRollCapTexture();
		_startButton.Text = Tr("LOADING_IN_PROGRESS");
		_clearCacheButton.text = Tr("CLEAR_LOADING_CACHE");
		_clearCacheButton.Visible = false;
		_clearCacheButton.Disabled = true;
		_clearCacheButton.Pressed += ShowClearLoadingCacheDialog;
		_loadBarSprout = GetNode<AdobeAnimateSpriteBase>("%LoadBarSprout");
		_loadBarSprout2 = GetNode<AdobeAnimateSpriteBase>("%LoadBarSprout2");
		_loadBarSprout3 = GetNode<AdobeAnimateSpriteBase>("%LoadBarSprout3");
		_loadBarSprout4 = GetNode<AdobeAnimateSpriteBase>("%LoadBarSprout4");
		_loadBarZombieHead = GetNode<AdobeAnimateSpriteBase>("%LoadBarZombieHead");
		BindBootstrapAtlasProfile();
		ResourceManager.Instance.OnLoadPercentage += SetPercentage;
		ResourceManager.Instance.OnLoadOver += LoadOver;
		ResourceManager.Instance.OnLoadFailed += LoadFailed;
		GetNode<BaseButton>("HBoxContainer/OnlineButton").Pressed += OnlineButtonPressed;
		GetNode<BaseButton>("%OfflineButton").Pressed += OfflineButtonPressed;
		StartResourceLoadAfterFirstDraw();
		StartupLoadDiagnostics.Mark("loading.ready.end");
		StartupLoadDiagnostics.StopObservingTree();
	}

	private void BindBootstrapAtlasProfile()
	{
		AdobeAnimateAtlasProfile adobeAnimateAtlasProfile = ResourceLoader.Load<AdobeAnimateAtlasProfile>("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres", null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(adobeAnimateAtlasProfile))
		{
			throw new InvalidOperationException("Bootstrap Adobe Animate atlas profile is unavailable: res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres");
		}
		_loadBarSprout.atlasProfileOverride = adobeAnimateAtlasProfile;
		_loadBarSprout2.atlasProfileOverride = adobeAnimateAtlasProfile;
		_loadBarSprout3.atlasProfileOverride = adobeAnimateAtlasProfile;
		_loadBarSprout4.atlasProfileOverride = adobeAnimateAtlasProfile;
		_loadBarZombieHead.atlasProfileOverride = adobeAnimateAtlasProfile;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		DisconnectLoadingAnimationCompletion(_loadBarSprout);
		DisconnectLoadingAnimationCompletion(_loadBarSprout2);
		DisconnectLoadingAnimationCompletion(_loadBarSprout3);
		DisconnectLoadingAnimationCompletion(_loadBarSprout4);
		DisconnectLoadingAnimationCompletion(_loadBarZombieHead);
		if (ResourceManager.Instance != null)
		{
			ResourceManager.Instance.OnLoadPercentage -= SetPercentage;
			ResourceManager.Instance.OnLoadOver -= LoadOver;
			ResourceManager.Instance.OnLoadFailed -= LoadFailed;
		}
	}

	public override void _Process(double delta)
	{
		if (_currentValue < _percentage)
		{
			float num = Mathf.Min((float)delta, 1f / 30f);
			_currentValue = Mathf.Lerp(_currentValue, _percentage, Mathf.Min((_currentValue * 2f + 0.5f) * num, 1f));
			_progressBar.Value = _currentValue;
			UpdateLoadingAnimationPlayback(_currentValue);
			if (CanAnimateSodRollCap())
			{
				_sodRollCap.Position = new Vector2(_currentValue * 315f, -26f + _currentValue * 30f);
				_sodRollCap.Rotation = (float)Math.PI * 4f * _currentValue;
				_sodRollCap.Scale = Vector2.One * (1f - _currentValue * 0.75f);
			}
		}
		if (_currentValue >= 0.95f && GodotObject.IsInstanceValid(_sodRollCap))
		{
			_sodRollCap.Visible = false;
		}
		TryFinishLoadingPresentation();
	}

	private void UpdateLoadingAnimationPlayback(float progress, bool playAudio = true)
	{
		ResumeLoadingAnimation(_loadBarSprout, progress, 0.15f, playAudio);
		ResumeLoadingAnimation(_loadBarSprout2, progress, 0.3f, playAudio);
		ResumeLoadingAnimation(_loadBarSprout3, progress, 0.4f, playAudio);
		ResumeLoadingAnimation(_loadBarSprout4, progress, 0.6f, playAudio);
		if (GodotObject.IsInstanceValid(_loadBarZombieHead) && _loadBarZombieHead.pause && !(progress < 0.8f))
		{
			if (playAudio)
			{
				AudioManager.Instance.AudioPlay("LoadingBarFlower");
				AudioManager.Instance.AudioPlay("LoadingBarZombie");
			}
			_loadBarZombieHead.SetAnimation("Idle", loop: false);
			_loadBarZombieHead.pause = false;
		}
	}

	private static void ResumeLoadingAnimation(AdobeAnimateSpriteBase animation, float progress, float threshold, bool playAudio)
	{
		if (GodotObject.IsInstanceValid(animation) && animation.pause && !(progress < threshold))
		{
			if (playAudio)
			{
				AudioManager.Instance.AudioPlay("LoadingBarFlower");
			}
			animation.SetAnimation("Idle", loop: false);
			animation.pause = false;
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!_isStart && !_loadLabel.Visible && Input.IsActionJustPressed("Press") && !ShouldIgnorePressStart(inputEvent))
		{
			AudioManager.Instance.AudioPlay("ButtonPress");
			OfflineButtonPressed();
		}
	}

	private async void StartResourceLoadAfterFirstDraw()
	{
		UpdateLoadingStepText("LOAD_STARTUP");
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		LoadingFirstFramePresentedUsec = Time.GetTicksUsec();
		StartupLoadDiagnostics.Mark("loading.first_frame");
		if (!IsInsideTree())
		{
			return;
		}
		StartupLoadDiagnostics.Mark("loading.font.begin");
		ApplyDeferredLoadingFont();
		StartupLoadDiagnostics.Mark("loading.font.end");
		ResourceManager resourceManager = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(resourceManager))
		{
			GD.PushError("Loading cannot continue because ResourceManager is unavailable.");
			return;
		}
		StartupLoadDiagnostics.Mark("loading.bootstrap.begin");
		ActivateLoadingAnimations();
		StartupLoadDiagnostics.Mark("loading.bootstrap.end");
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (IsInsideTree())
		{
			BootstrapFirstFramePresentedUsec = Time.GetTicksUsec();
			StartupLoadDiagnostics.Mark("loading.bootstrap_frame");
			GameplayResourceLoadRequestedUsec = Time.GetTicksUsec();
			resourceManager.BeginGameplayAtlasPreload();
			StartupLoadDiagnostics.Mark("loading.savedata.begin");
			GameSaveManager.Instance?.EnsureLoaded();
			StartupLoadDiagnostics.Mark("loading.savedata.end");
			resourceManager.BeginLoad();
		}
	}

	private void ApplyDeferredLoadingFont()
	{
		if (GodotObject.IsInstanceValid(_startButton))
		{
			FontFile fontFile = GD.Load<FontFile>("res://Asset/Font/fzkt.ttf");
			if (GodotObject.IsInstanceValid(fontFile))
			{
				_startButton.AddThemeFontOverride("font", fontFile);
			}
		}
	}

	private void ActivateLoadingAnimations()
	{
		ActivateLoadingAnimation(_loadBarSprout);
		ActivateLoadingAnimation(_loadBarSprout2);
		ActivateLoadingAnimation(_loadBarSprout3);
		ActivateLoadingAnimation(_loadBarSprout4);
		ActivateLoadingAnimation(_loadBarZombieHead);
	}

	private void ActivateLoadingAnimation(AdobeAnimateSpriteBase animation)
	{
		if (GodotObject.IsInstanceValid(animation))
		{
			animation.Visible = true;
			animation.pause = true;
			animation.OnAnimeCompleted -= OnLoadingAnimationCompleted;
			animation.OnAnimeCompleted += OnLoadingAnimationCompleted;
			animation.SetAnimation("Idle", loop: false);
		}
	}

	private void DisconnectLoadingAnimationCompletion(AdobeAnimateSpriteBase animation)
	{
		if (GodotObject.IsInstanceValid(animation))
		{
			animation.OnAnimeCompleted -= OnLoadingAnimationCompleted;
		}
	}

	private void OnLoadingAnimationCompleted(string clipName)
	{
		if (!(clipName != "Idle") && _completedLoadingAnimationCount < 5)
		{
			_completedLoadingAnimationCount++;
			TryFinishLoadingPresentation();
		}
	}

	private bool ShouldIgnorePressStart(InputEvent inputEvent)
	{
		if (!IsClearCacheDialogVisible() && !IsPointerOverClearCacheButton(inputEvent))
		{
			return IsClearCacheButtonFocused();
		}
		return true;
	}

	private bool IsClearCacheDialogVisible()
	{
		if (GodotObject.IsInstanceValid(_clearCacheDialog))
		{
			return _clearCacheDialog.Visible;
		}
		return false;
	}

	private bool IsClearCacheButtonFocused()
	{
		if (GodotObject.IsInstanceValid(_clearCacheButton) && _clearCacheButton.Visible)
		{
			return _clearCacheButton.HasFocus();
		}
		return false;
	}

	private bool IsPointerOverClearCacheButton(InputEvent inputEvent)
	{
		if (!GodotObject.IsInstanceValid(_clearCacheButton) || !_clearCacheButton.Visible || _clearCacheButton.Disabled)
		{
			return false;
		}
		if (!(inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton))
		{
			return false;
		}
		return _clearCacheButton.GetGlobalRect().HasPoint(inputEventMouseButton.Position);
	}

	public void OnlineButtonPressed()
	{
		ResourceManager.Instance.RequireFullGameplayResourcesReady("OnlineButtonPressed");
		_isStart = true;
		GameSaveManager.Instance.EnsureLoaded();
		SceneManager.Instance.ChangeScene("MainMenu", stopAllAudio: false);
	}

	public void OfflineButtonPressed()
	{
		ResourceManager.Instance.RequireFullGameplayResourcesReady("OfflineButtonPressed");
		_isStart = true;
		GameSaveManager.Instance.EnsureLoaded();
		SceneManager.Instance.ChangeScene("MainMenu", stopAllAudio: false);
	}

	public void SetPercentage(double percentage, string stepName, string resourceName, int resourceIndex, int resourceTotal)
	{
		_percentage = (float)percentage;
		UpdateLoadingStepText(stepName, resourceName, resourceIndex, resourceTotal);
	}

	public void LoadOver()
	{
		_loadCompleted = true;
		_percentage = 1f;
		ThumbnailBinaryResourceCache.ClearOutdatedVersionCaches();
		ThumbnailBinaryResourceCache.ClearLegacyCharacterThumbnailCategories();
		LoadStartupModsOnce();
		TryFinishLoadingPresentation();
		AudioManager.Instance.AudioPlay("MainMenu", AudioManagerEnum.TYPE.MUSIC);
	}

	private async void LoadStartupModsOnce()
	{
		if (_startupModFlowStarted)
		{
			return;
		}
		_startupModFlowStarted = true;
		await StartupModsGate.WaitAsync();
		try
		{
			if (_startupModsLoaded || !IsInsideTree())
			{
				return;
			}
			XWModPackageInstaller.CleanupInstallArtifacts();
			AndroidModInstallIntent.PrepareResult prepareResult = AndroidModInstallIntent.PrepareStartupIntent();
			if (prepareResult.RequiresConfirmation)
			{
				using AndroidModInstallIntent.PreparedStartupRequest request = prepareResult.Request;
				switch (await ConfirmStartupModInstall(request))
				{
				case ModInstallConfirmation.Confirmed:
				{
					AndroidModInstallIntent.StartupResult startupResult = AndroidModInstallIntent.InstallPreparedRequest(request);
					ReportStartupModImportResult(startupResult.Success, startupResult.Message);
					return;
				}
				case ModInstallConfirmation.Cancelled:
					GD.Print("[Loading] Android Mod import cancelled: " + request.Manifest?.Id);
					return;
				}
				if (IsInsideTree())
				{
					ReportStartupModImportResult(success: false, "无法显示 Mod 安装确认窗口，已取消本次安装。");
				}
			}
			else if (prepareResult.Handled)
			{
				ReportStartupModImportResult(prepareResult.Success, prepareResult.Message);
			}
		}
		catch (Exception ex)
		{
			GD.PushError("[Loading] Mod startup load failed: " + ex.GetBaseException().Message);
		}
		finally
		{
			if (!_startupModsLoaded && IsInsideTree())
			{
				LoadEnabledStartupMods();
				_startupModsLoaded = true;
			}
			if (_startupModsLoaded && IsInsideTree())
			{
				_startupModsReady = true;
				TryFinishLoadingPresentation();
			}
			StartupModsGate.Release();
		}
	}

	private void LoadEnabledStartupMods()
	{
		try
		{
			_startupReport = ModLoader.LoadAllDetailed();
			int legacyCount = _startupReport.LegacyCount;
			if (legacyCount < 0)
			{
				GD.PushWarning("[Loading] Mod startup load completed with a blocked rollback; see Mod diagnostics");
				return;
			}
			GD.Print($"[Loading] loaded {legacyCount} enabled Mod package(s)");
		}
		catch (Exception ex)
		{
			GD.PushError("[Loading] Mod startup load failed: " + ex.GetBaseException().Message);
			_startupReport = new XWModStartupReport
			{
				LegacyCount = -1,
				BlockingReason = ex.GetBaseException().Message
			};
		}
		finally
		{
			if (_startupImportSuccess.HasValue)
			{
				goto IL_00c5;
			}
			XWModStartupReport startupReport = _startupReport;
			if (startupReport != null && startupReport.HasFailures)
			{
				goto IL_00c5;
			}
			goto end_IL_00a4;
			IL_00c5:
			CallDeferred("ShowStartupModImportResult", _startupImportSuccess ?? true, _startupImportMessage);
			end_IL_00a4:;
		}
	}

	private async Task<ModInstallConfirmation> ConfirmStartupModInstall(AndroidModInstallIntent.PreparedStartupRequest request)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!IsInsideTree() || !GodotObject.IsInstanceValid(DialogManager.Instance))
		{
			return ModInstallConfirmation.Unavailable;
		}
		DialogBoxChoose dialog = DialogManager.Instance.DialogCreate("DialogBoxChoose") as DialogBoxChoose;
		if (!GodotObject.IsInstanceValid(dialog))
		{
			return ModInstallConfirmation.Unavailable;
		}
		TaskCompletionSource<ModInstallConfirmation> completion = new TaskCompletionSource<ModInstallConfirmation>();
		dialog.text = BuildStartupModInstallConfirmation(request);
		dialog.OnChooseTrue += Confirmed;
		dialog.OnChooseFalse += Cancelled;
		dialog.OnClose += DialogClosed;
		dialog.TreeExited += DialogExited;
		base.TreeExited += LoadingExited;
		try
		{
			return await completion.Task;
		}
		finally
		{
			base.TreeExited -= LoadingExited;
			if (GodotObject.IsInstanceValid(dialog))
			{
				dialog.OnChooseTrue -= Confirmed;
				dialog.OnChooseFalse -= Cancelled;
				dialog.OnClose -= DialogClosed;
				dialog.TreeExited -= DialogExited;
			}
		}
		void Cancelled()
		{
			completion.TrySetResult(ModInstallConfirmation.Cancelled);
		}
		void Confirmed()
		{
			completion.TrySetResult(ModInstallConfirmation.Confirmed);
		}
		void DialogClosed()
		{
			completion.TrySetResult(ModInstallConfirmation.Cancelled);
		}
		void DialogExited()
		{
			completion.TrySetResult(ModInstallConfirmation.Unavailable);
		}
		void LoadingExited()
		{
			completion.TrySetResult(ModInstallConfirmation.Unavailable);
		}
	}

	private static string BuildStartupModInstallConfirmation(AndroidModInstallIntent.PreparedStartupRequest request)
	{
		XWModManifest manifest = request.Manifest;
		string value = (string.IsNullOrWhiteSpace(manifest?.Author) ? "未声明" : manifest.Author);
		return "[center][font_size=24]Mod 安装确认[/font_size][/center]\n[font_size=17]名称：" + SafeDialogField(manifest?.Name, 80) + "\nID：" + SafeDialogField(manifest?.Id, 128) + "\nID SHA256：" + ManifestIdSha256(manifest?.Id) + "\n版本：" + SafeDialogField(manifest?.Version, 64) + "\n作者：" + SafeDialogField(value, 80) + "\n来源：" + SafeDialogField(request.SourceAuthority, 128) + "\nSHA256：" + SafeDialogField(request.Sha256, 64) + "[/font_size]\n[center]确认后将安装并启用此 Mod。[/center]";
	}

	private static string SafeDialogField(string value, int maxLength)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = value ?? "";
		foreach (char c in text)
		{
			UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
			if (char.IsControl(c) || unicodeCategory == UnicodeCategory.Format)
			{
				stringBuilder.Append(' ');
			}
			else
			{
				stringBuilder.Append(char.IsWhiteSpace(c) ? ' ' : c);
			}
		}
		string text2 = string.Join(" ", stringBuilder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
		if (text2.Length > maxLength)
		{
			int num = maxLength;
			if (num > 0 && char.IsHighSurrogate(text2[num - 1]) && char.IsLowSurrogate(text2[num]))
			{
				num--;
			}
			text2 = text2.Substring(0, num) + "…";
		}
		return EscapeBbcode(text2);
	}

	private static string ManifestIdSha256(string value)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? "")));
	}

	private void ReportStartupModImportResult(bool success, string message)
	{
		if (success)
		{
			GD.Print("[Loading] Android Mod import completed: " + message);
		}
		else
		{
			GD.PushWarning("[Loading] Android Mod import rejected: " + message);
		}
		_startupImportSuccess = success;
		_startupImportMessage = message;
	}

	private void ShowStartupModImportResult(bool success, string message)
	{
		if (DialogManager.Instance == null || !(DialogManager.Instance.DialogCreate("DialogBoxTips") is DialogBoxTips dialogBoxTips))
		{
			return;
		}
		string value = ((!_startupImportSuccess.HasValue) ? "Mod 加载结果" : (success ? "Mod 安装完成" : "Mod 安装失败"));
		string value2 = ((!success) ? (message + "\n") : "") + (_startupReport?.PlayerSummary() ?? "加载结果暂不可用，请查看 Mod 管理。");
		dialogBoxTips.text = $"[center][font_size=24]{value}[/font_size]\n{SafeDialogField(value2, 1600)}[/center]";
		XWModStartupReport startupReport = _startupReport;
		if (startupReport == null || !startupReport.HasFailures)
		{
			return;
		}
		dialogBoxTips.GetNode<BaseButton>("%ConfirmButton").Set("text", "查看 Mod 管理");
		dialogBoxTips.OnClose += delegate
		{
			if (DialogManager.Instance.DialogCreate("MainMenuOption") is DialogMainMenuOption dialogMainMenuOption)
			{
				dialogMainMenuOption.AllowModNavigation = false;
				Callable.From(dialogMainMenuOption.OpenManagement).CallDeferred();
			}
		};
	}

	private static string EscapeBbcode(string value)
	{
		return (value ?? "").Replace("[", "［", StringComparison.Ordinal).Replace("]", "］", StringComparison.Ordinal);
	}

	private void TryFinishLoadingPresentation()
	{
		if (_loadCompleted && _startupModsReady && !_loadingPresentationCompleted && !(_currentValue < 0.95f) && _completedLoadingAnimationCount >= 5)
		{
			_loadingPresentationCompleted = true;
			_currentValue = 1f;
			_progressBar.Value = 1.0;
			if (GodotObject.IsInstanceValid(_sodRollCap))
			{
				_sodRollCap.Visible = false;
			}
			_startButton.Disabled = false;
			_startButton.Text = Tr("CLICK_TO_START");
			_clearCacheButton.Visible = true;
			_clearCacheButton.Disabled = false;
			_loadLabel.Visible = false;
		}
	}

	private void LoadFailed(string error)
	{
		_loadCompleted = false;
		_loadingPresentationCompleted = false;
		_startButton.Disabled = true;
		_startButton.Text = Tr("LOADING_FAILED");
		_loadLabel.Visible = true;
		_loadLabel.Text = Tr("LOADING_FAILED") + ": " + error;
		_clearCacheButton.Visible = true;
		_clearCacheButton.Disabled = false;
	}

	private void UpdateLoadingStepText(string stepName, string resourceName = "", int resourceIndex = 0, int resourceTotal = 0)
	{
		if (!string.IsNullOrEmpty(stepName))
		{
			_loadLabel.Text = BuildLoadingText(stepName, resourceName, resourceIndex, resourceTotal);
		}
	}

	private string BuildLoadingText(string stepName, string resourceName, int resourceIndex, int resourceTotal)
	{
		string text = TranslateLoadingStep(stepName);
		if (!string.IsNullOrEmpty(resourceName))
		{
			text = text + ": " + TranslateLoadingStep(resourceName);
		}
		if (resourceTotal > 0)
		{
			text += $" {resourceIndex}/{resourceTotal}";
		}
		return text;
	}

	private string TranslateLoadingStep(string stepName)
	{
		return TranslationServer.Translate(stepName);
	}

	private void EnsureSodRollCapTexture()
	{
		if (GodotObject.IsInstanceValid(_sodRollCap) && !GodotObject.IsInstanceValid(_sodRollCap.Texture))
		{
			_sodRollCap.Texture = GD.Load<Texture2D>("res://Asset/Texture/GUI/General/Loading/SodRollCap.png");
			if (!GodotObject.IsInstanceValid(_sodRollCap.Texture))
			{
				_sodRollCap.Visible = false;
			}
		}
	}

	private bool CanAnimateSodRollCap()
	{
		if (GodotObject.IsInstanceValid(_sodRollCap))
		{
			return GodotObject.IsInstanceValid(_sodRollCap.Texture);
		}
		return false;
	}

	private void ShowClearLoadingCacheDialog()
	{
		if (!IsClearCacheDialogVisible() && GodotObject.IsInstanceValid(DialogManager.Instance))
		{
			_clearCacheDialog = (DialogBoxChoose)DialogManager.Instance.DialogCreate("DialogBoxChoose");
			_clearCacheDialog.text = BuildClearCacheDialogText();
			_clearCacheDialog.OnChooseTrue += ClearLoadingCacheConfirmed;
		}
	}

	private string BuildClearCacheDialogText()
	{
		return $"[center][font_size=24]{Tr("CLEAR_LOADING_CACHE")}[/font_size][/center]\n[center][font_size=16]{Tr("CLEAR_LOADING_CACHE_CONFIRM")}[/font_size][/center]";
	}

	private void ClearLoadingCacheConfirmed()
	{
		int value = CharacterBinaryResourceCache.ClearAllCaches();
		int value2 = ClearLoadedMapResourceCaches();
		GD.Print($"Loading cache cleared: characterBinaryEntries={value}, mapConfigs={value2}");
		if (GodotObject.IsInstanceValid(_clearCacheButton))
		{
			_clearCacheButton.text = Tr("CLEAR_LOADING_CACHE_DONE");
		}
	}

	private int ClearLoadedMapResourceCaches()
	{
		if (ResourceManager.Instance == null)
		{
			return 0;
		}
		int num = 0;
		foreach (Resource value in ResourceManager.Instance.MAPS.Values)
		{
			if (value is TowerDefenseMapConfig towerDefenseMapConfig)
			{
				towerDefenseMapConfig.ClearLoadedMapResources(clearThumbnail: true);
				num++;
			}
		}
		return num;
	}
}
