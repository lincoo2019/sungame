using System;
using System.Text;
using Godot;
using Microsoft.Extensions.Logging;
using ZLogger;

[GlobalClass]
public partial class Global : Node
{
	public delegate void AnimeFrameRateChangeEventHandler();

	private static readonly ILogger _logger = Log.CreateLogger<Global>();

	private readonly StringBuilder _pathBuilder = new StringBuilder(256);

	[ExportGroup("Base", "")]
	private DeviceFrameRateService _frameRateService;

	private double _animeFrameRate = 30.0;

	private AdobeAnimateRenderBackend _adobeAnimateRenderBackend;

	private double _timeScale = 1.0;

	public static Global Instance { get; private set; }

	public string version { get; set; } = "0.29.0.0";

	public string[] header { get; set; } = Array.Empty<string>();

	public bool isMobile { get; set; }

	[Export(PropertyHint.None, "")]
	public bool debug { get; set; }

	[Export(PropertyHint.None, "")]
	public double animeFrameRate
	{
		get
		{
			return _animeFrameRate;
		}
		set
		{
			_animeFrameRate = FrameRatePolicy.NormalizeRefreshRate(value);
			if (_frameRateService != null)
			{
				_frameRateService.SetPreferredFrameRate(_animeFrameRate);
			}
			else
			{
				ApplyEffectiveFrameRate((int)_animeFrameRate);
			}
		}
	}

	public int effectiveAnimeFrameRate { get; private set; } = 30;

	public double trueAnimeFrameRate => (double)effectiveAnimeFrameRate / timeScale;

	public AdobeAnimateRenderBackend adobeAnimateRenderBackend
	{
		get
		{
			return _adobeAnimateRenderBackend;
		}
		set
		{
			AdobeAnimateRenderBackend adobeAnimateRenderBackend = AdobeAnimateRenderBackendPolicy.Normalize((int)value);
			if (_adobeAnimateRenderBackend != adobeAnimateRenderBackend)
			{
				_adobeAnimateRenderBackend = adobeAnimateRenderBackend;
				this.OnAdobeAnimateRenderBackendChanged?.Invoke(adobeAnimateRenderBackend);
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double timeScale
	{
		get
		{
			return _timeScale;
		}
		set
		{
			_timeScale = value;
			Engine.TimeScale = _timeScale;
			this.OnAnimeFrameRateChange?.Invoke();
		}
	}

	public string enterLevelMode { get; set; } = "LevelChoose";

	public bool enterLevelIsBattle { get; set; }

	public bool enterLevelIsBattleFinish { get; set; }

	public string enterLevelId { get; set; } = "-1";

	public string enterTryLevelGroup { get; set; } = "";

	public string enterQuizMap { get; set; } = "Frontlawn";

	public string currentLevelChoose { get; set; } = "Adventure";

	public int currentChapterId { get; set; } = -1;

	public int currentLevelId { get; set; } = -1;

	public bool currentAwardMode { get; set; }

	public bool mainMenuShowMoreModes { get; set; }

	public int currentAwardType { get; set; }

	public string currentAwardValue { get; set; } = "";

	public string currentDiyLevelUid { get; set; } = "";

	public bool isMultiplayerMode { get; set; }

	public bool isMultiplayerHost { get; set; }

	public string newVersion { get; set; } = "";

	public bool hasNewVersion { get; set; }

	public string uri { get; set; } = "";

	public bool newVersionSkip { get; set; }

	public int maxFps { get; set; } = 60;

	public bool isEditor { get; set; }

	public static bool IsMultiplayerMode => Instance?.isMultiplayerMode ?? false;

	public static bool IsMultiplayerHost => Instance?.isMultiplayerHost ?? false;

	public static string Version => Instance?.version ?? "";

	public static string[] Header => Instance?.header ?? Array.Empty<string>();

	public static bool IsMobile => Instance?.isMobile ?? false;

	public static bool IsEditor => Instance?.isEditor ?? false;

	public static double TimeScale
	{
		get
		{
			return Instance?.timeScale ?? 1.0;
		}
		set
		{
			if (Instance != null)
			{
				Instance.timeScale = value;
			}
		}
	}

	public event AnimeFrameRateChangeEventHandler OnAnimeFrameRateChange;

	public event Action OnFrameRateOptionsChanged;

	public event Action<AdobeAnimateRenderBackend> OnAdobeAnimateRenderBackendChanged;

	public int[] GetFrameRateOptions()
	{
		return _frameRateService?.GetFrameRateOptions() ?? FrameRatePolicy.BuildOptions(Math.Max(1, maxFps));
	}

	private void ApplyEffectiveFrameRate(int effectiveFrameRate)
	{
		effectiveAnimeFrameRate = Math.Max(1, effectiveFrameRate);
		Engine.MaxFps = effectiveAnimeFrameRate;
		Engine.PhysicsTicksPerSecond = Mathf.Min(60, effectiveFrameRate);
		this.OnAnimeFrameRateChange?.Invoke();
	}

	public string GetHTTPRequestErrorMessage(long result)
	{
		if ((ulong)result <= 8uL)
		{
			switch ((int)result)
			{
			case 0:
				return "";
			case 1:
				return "无法连接到服务器";
			case 2:
				return "无法解析服务器地址";
			case 3:
				return "连接错误";
			case 4:
				return "TLS握手失败，请检查网络连接";
			case 5:
				return "服务器无响应";
			case 6:
				return "请求失败";
			case 7:
				return "重定向次数过多";
			case 8:
				return "服务器返回的数据超过安全上限";
			}
		}
		return $"未知网络错误({result})";
	}

	public override void _Ready()
	{
		StartupLoadDiagnostics.Mark("global.ready.begin");
		Instance = this;
		StartupLoadDiagnostics.Mark("global.log.begin");
		Log.Init(debug);
		StartupLoadDiagnostics.Mark("global.log.end");
		header = new string[1] { "X-PVZHE-Client-Version:" + version };
		GetWindow().CloseRequested += delegate
		{
			if (!Engine.IsEditorHint())
			{
				if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && !isMultiplayerMode)
				{
					GameSaveManager.Instance.SaveLevelProgress(TowerDefenseManager.Instance.currentControl.levelConfig.name, TowerDefenseManager.Instance.currentControl.ModLevelIdentity);
				}
				GameSaveManager.Instance.Save();
			}
		};
		string name = OS.GetName();
		isMobile = name == "Android" || name == "iOS";
		StartupLoadDiagnostics.Mark("global.locale.begin");
		TranslationServer.SetLocale("zh");
		StartupLoadDiagnostics.Mark("global.locale.end");
		StartupLoadDiagnostics.Mark("global.frame_rate.begin");
		_frameRateService = new DeviceFrameRateService();
		_frameRateService.Initialize(_animeFrameRate);
		_frameRateService.EffectiveFrameRateChanged += ApplyEffectiveFrameRate;
		_frameRateService.FrameRateOptionsChanged += delegate
		{
			maxFps = _frameRateService.DeviceFrameRateLimit;
			this.OnFrameRateOptionsChanged?.Invoke();
		};
		AddChild(_frameRateService, forceReadableName: false, InternalMode.Disabled);
		StartupLoadDiagnostics.Mark("global.frame_rate.end");
		if (isMobile)
		{
			OS.RequestPermissions();
			if (name == "Android")
			{
				CallDeferred(MethodName.ApplyAndroidImmersiveMode);
			}
		}
		StartupLoadDiagnostics.Mark("global.ready.end");
	}

	private void ApplyAndroidImmersiveMode()
	{
		Window window = GetWindow();
		if (window != null)
		{
			window.Mode = Window.ModeEnum.ExclusiveFullscreen;
			window.Borderless = true;
			int windowId = DisplayServer.WindowGetCurrentScreen();
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen, windowId);
			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, enabled: true, windowId);
		}
	}

	public void FreshAnimeData()
	{
		new GodotThread().Start(Callable.From(delegate
		{
			FreshAnimeDataOpenDir("res://");
		}), GodotThread.Priority.Normal);
	}

	public void FreshAnimeDataOpenDir(string path)
	{
		DirAccess dirAccess = DirAccess.Open(path);
		ZLoggerInformationInterpolatedStringHandler message;
		bool enabled;
		if (dirAccess != null)
		{
			dirAccess.ListDirBegin();
			string next = dirAccess.GetNext();
			while (next != "")
			{
				_pathBuilder.Clear();
				_pathBuilder.Append(path).Append("//").Append(next);
				string text = _pathBuilder.ToString();
				if (dirAccess.CurrentIsDir())
				{
					ILogger logger = _logger;
					ILogger logger2 = logger;
					message = new ZLoggerInformationInterpolatedStringHandler(5, 1, logger, out enabled);
					if (enabled)
					{
						message.AppendLiteral("发现文件夹");
						message.AppendFormatted(text, 0, null, "fullPath");
					}
					logger2.ZLogInformation(ref message);
					FreshAnimeDataOpenDir(text);
				}
				else
				{
					ILogger logger = _logger;
					ILogger logger3 = logger;
					message = new ZLoggerInformationInterpolatedStringHandler(4, 1, logger, out enabled);
					if (enabled)
					{
						message.AppendLiteral("发现文件");
						message.AppendFormatted(text, 0, null, "fullPath");
					}
					logger3.ZLogInformation(ref message);
					if (next.GetExtension() == "tres" && GD.Load(text) is AdobeAnimateData adobeAnimateData)
					{
						adobeAnimateData.Init();
						ResourceSaver.Save(adobeAnimateData, text, ResourceSaver.SaverFlags.Compress);
					}
				}
				next = dirAccess.GetNext();
			}
		}
		else
		{
			ILogger logger = _logger;
			ILogger logger4 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(10, 0, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("尝试访问路径时出错。");
			}
			logger4.ZLogInformation(ref message);
		}
	}

	public void FreshLevelData()
	{
		new GodotThread().Start(Callable.From(delegate
		{
			FreshLevelDataOpenDir("res://");
		}), GodotThread.Priority.Normal);
	}

	public void FreshLevelDataOpenDir(string path)
	{
		DirAccess dirAccess = DirAccess.Open(path);
		ZLoggerInformationInterpolatedStringHandler message;
		bool enabled;
		if (dirAccess != null)
		{
			dirAccess.ListDirBegin();
			string next = dirAccess.GetNext();
			while (next != "")
			{
				_pathBuilder.Clear();
				_pathBuilder.Append(path).Append("//").Append(next);
				string text = _pathBuilder.ToString();
				if (dirAccess.CurrentIsDir())
				{
					ILogger logger = _logger;
					ILogger logger2 = logger;
					message = new ZLoggerInformationInterpolatedStringHandler(5, 1, logger, out enabled);
					if (enabled)
					{
						message.AppendLiteral("发现文件夹");
						message.AppendFormatted(text, 0, null, "fullPath");
					}
					logger2.ZLogInformation(ref message);
					FreshLevelDataOpenDir(text);
				}
				else
				{
					ILogger logger = _logger;
					ILogger logger3 = logger;
					message = new ZLoggerInformationInterpolatedStringHandler(4, 1, logger, out enabled);
					if (enabled)
					{
						message.AppendLiteral("发现文件");
						message.AppendFormatted(text, 0, null, "fullPath");
					}
					logger3.ZLogInformation(ref message);
					if (next.GetExtension() == "tres" && GD.Load(text) is TowerDefenseLevelConfig towerDefenseLevelConfig)
					{
						towerDefenseLevelConfig.Clear();
						ResourceSaver.Save(towerDefenseLevelConfig, "", ResourceSaver.SaverFlags.None);
					}
				}
				next = dirAccess.GetNext();
			}
		}
		else
		{
			ILogger logger = _logger;
			ILogger logger4 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(10, 0, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("尝试访问路径时出错。");
			}
			logger4.ZLogInformation(ref message);
		}
	}

	public Global()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/Global");
		StartupLoadDiagnostics.ObserveAutoloadTree();
	}
}
