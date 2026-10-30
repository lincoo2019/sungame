using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using ZLogger;

[GlobalClass]
public partial class InternetServerManager : Node2D
{
	public delegate void OnlineLevelGetEventHandler(Dictionary data);

	public delegate void WorkshopTagsGetEventHandler(Godot.Collections.Array data);

	public delegate void ShareLevelSuccessEventHandler(string code, long expireAt, long expireSeconds);

	public delegate void ShareLevelFailedEventHandler(string message);

	public delegate void GetSharedLevelSuccessEventHandler(byte[] data);

	public delegate void GetSharedLevelFailedEventHandler(string message);

	public delegate void DailyLevelMonthLoadedEventHandler(int year, int month);

	private static readonly ILogger _logger = Log.CreateLogger<InternetServerManager>();

	public NativeHttpRequest versionHttpRequest;

	public NativeHttpRequest dailyLevelHTTPRequest;

	public NativeHttpRequest onlineLevelHTTPRequest;

	private OnlineLevelStatisticsReporter _onlineLevelStatistics;

	public NativeHttpRequest workshopTagsHTTPRequest;

	public NativeHttpRequest exportFileHTTPRequest;

	public NativeHttpRequest loadFileHTTPRequest;

	private bool dailyLevelRequesting;

	public bool versionGetOver { get; set; }

	public string newVersion { get; set; } = "";

	public string versionMessage { get; set; } = "";

	public bool dailyLevelGetOver { get; set; }

	public Dictionary dailyLevelLoadedMonths { get; set; } = new Dictionary();

	public int dailyLevelRequestYear { get; set; }

	public int dailyLevelRequestMonth { get; set; }

	public static InternetServerManager Instance { get; private set; }

	public event OnlineLevelGetEventHandler OnOnlineLevelGet;

	public event WorkshopTagsGetEventHandler OnWorkshopTagsGet;

	public event ShareLevelSuccessEventHandler OnShareLevelSuccess;

	public event ShareLevelFailedEventHandler OnShareLevelFailed;

	public event GetSharedLevelSuccessEventHandler OnGetSharedLevelSuccess;

	public event GetSharedLevelFailedEventHandler OnGetSharedLevelFailed;

	public event DailyLevelMonthLoadedEventHandler OnDailyLevelMonthLoaded;

	public override void _Ready()
	{
		Instance = this;
		versionHttpRequest = CreateHttpRequest();
		dailyLevelHTTPRequest = CreateHttpRequest();
		onlineLevelHTTPRequest = CreateHttpRequest();
		_onlineLevelStatistics = new OnlineLevelStatisticsReporter();
		AddChild(_onlineLevelStatistics, forceReadableName: false, InternalMode.Disabled);
		workshopTagsHTTPRequest = CreateHttpRequest();
		exportFileHTTPRequest = CreateHttpRequest();
		loadFileHTTPRequest = CreateHttpRequest();
		versionHttpRequest.RequestCompleted += VersionHTTPRequestCompleted;
		dailyLevelHTTPRequest.RequestCompleted += DailyLevelHTTPRequestCompleted;
		onlineLevelHTTPRequest.RequestCompleted += OnlineLevelHTTPRequestCompleted;
		workshopTagsHTTPRequest.RequestCompleted += WorkshopTagsHTTPRequestCompleted;
		exportFileHTTPRequest.RequestCompleted += ExportFileHTTPRequestCompleted;
		loadFileHTTPRequest.RequestCompleted += LoadFileHTTPRequestCompleted;
		versionHttpRequest.Request("https://api.pvzhe.com/new_version", Global.Instance.header);
	}

	private NativeHttpRequest CreateHttpRequest()
	{
		NativeHttpRequest nativeHttpRequest = new NativeHttpRequest();
		AddChild(nativeHttpRequest, forceReadableName: false, InternalMode.Disabled);
		return nativeHttpRequest;
	}

	public void VersionHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		ZLoggerInformationInterpolatedStringHandler message;
		bool enabled;
		if (result != 0L)
		{
			ILogger logger = _logger;
			ILogger logger2 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(8, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("版本检查失败: ");
				message.AppendFormatted(Global.Instance.GetHTTPRequestErrorMessage(result), 0, null, "Global.Instance.GetHTTPRequestErrorMessage(result)");
			}
			logger2.ZLogInformation(ref message);
			return;
		}
		JsonDocument jsonDocument;
		try
		{
			jsonDocument = JsonDocument.Parse(body);
		}
		catch (JsonException)
		{
			return;
		}
		using (jsonDocument)
		{
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				return;
			}
			string propertyName = (Global.IsMobile ? "android" : "pc");
			if (!rootElement.TryGetProperty(propertyName, out var value))
			{
				return;
			}
			versionGetOver = true;
			List<int> list = new List<int>(4);
			if (value.TryGetProperty("version", out var value2) && value2.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value2.EnumerateArray())
				{
					list.Add(item.GetInt32());
				}
			}
			else
			{
				list.AddRange(new int[4] { -1, -1, -1, -1 });
			}
			JsonElement value3;
			string text = ((value.TryGetProperty("base64", out value3) && value3.ValueKind == JsonValueKind.String) ? (value3.GetString() ?? "error") : "error");
			Global.Instance.uri = ((value.TryGetProperty("url", out var value4) && value4.ValueKind == JsonValueKind.String) ? (value4.GetString() ?? "error") : "error");
			versionMessage = ((value.TryGetProperty("message", out var value5) && value5.ValueKind == JsonValueKind.String) ? (value5.GetString() ?? "") : "");
			newVersion = string.Join(".", list);
			ILogger logger = _logger;
			ILogger logger3 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(12, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("newVersion: ");
				message.AppendFormatted(newVersion, 0, null, "newVersion");
			}
			logger3.ZLogInformation(ref message);
			logger = _logger;
			ILogger logger4 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(5, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("uri: ");
				message.AppendFormatted(Global.Instance.uri, 0, null, "Global.Instance.uri");
			}
			logger4.ZLogInformation(ref message);
			logger = _logger;
			ILogger logger5 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(8, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("base64: ");
				message.AppendFormatted(text, 0, null, "base64");
			}
			logger5.ZLogInformation(ref message);
			if (!(text != "error"))
			{
				return;
			}
			string text2 = Marshalls.Base64ToUtf8(text);
			logger = _logger;
			ILogger logger6 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(13, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("checkString: ");
				message.AppendFormatted(text2, 0, null, "checkString");
			}
			logger6.ZLogInformation(ref message);
			if (!(text2 == newVersion + Global.Instance.uri))
			{
				return;
			}
			Global.Instance.newVersion = newVersion;
			bool hasNewVersion = false;
			string[] array = Global.Instance.version.Split(".");
			for (int i = 0; i < list.Count; i++)
			{
				if (int.Parse(array[i]) < list[i])
				{
					hasNewVersion = true;
					break;
				}
				if (int.Parse(array[i]) > list[i])
				{
					break;
				}
			}
			Global.Instance.hasNewVersion = hasNewVersion;
		}
	}

	public bool DailyLevelMonthNeedRefresh(int year, int month)
	{
		string text = $"{year:D}-{month:D2}";
		if (!dailyLevelLoadedMonths.ContainsKey(text))
		{
			return true;
		}
		Dictionary datetimeDictFromSystem = Time.GetDatetimeDictFromSystem();
		if ((int)(long)datetimeDictFromSystem["year"] == year && (int)(long)datetimeDictFromSystem["month"] == month)
		{
			string text2 = string.Format("{0}-{1:D2}-{2:D2}", (int)(long)datetimeDictFromSystem["year"], (int)(long)datetimeDictFromSystem["month"], (int)(long)datetimeDictFromSystem["day"]);
			System.Collections.Generic.Dictionary<string, Variant> dAILY_LEVEL_DATA = ResourceManager.Instance.DAILY_LEVEL_DATA;
			if (dAILY_LEVEL_DATA == null || dAILY_LEVEL_DATA.Count == 0 || !dAILY_LEVEL_DATA.ContainsKey("LevelDateMap"))
			{
				return true;
			}
			if (!((Dictionary)dAILY_LEVEL_DATA["LevelDateMap"]).ContainsKey(text2))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDailyLevelRequestPending(int year, int month)
	{
		if (dailyLevelRequesting && dailyLevelRequestYear == year)
		{
			return dailyLevelRequestMonth == month;
		}
		return false;
	}

	public void GetDailyLevel(int year = 0, int month = 0, bool forceRefresh = false)
	{
		if (year == 0 || month == 0)
		{
			Dictionary datetimeDictFromSystem = Time.GetDatetimeDictFromSystem();
			year = (int)(long)datetimeDictFromSystem["year"];
			month = (int)(long)datetimeDictFromSystem["month"];
		}
		string text = $"{year:D}-{month:D2}";
		if (!forceRefresh && dailyLevelLoadedMonths.ContainsKey(text))
		{
			this.OnDailyLevelMonthLoaded?.Invoke(year, month);
		}
		else if (!dailyLevelRequesting || dailyLevelRequestYear != year || dailyLevelRequestMonth != month)
		{
			dailyLevelRequestYear = year;
			dailyLevelRequestMonth = month;
			dailyLevelRequesting = true;
			dailyLevelHTTPRequest.CancelRequest();
			dailyLevelHTTPRequest.Request($"https://api.pvzhe.com/get_daily_levels?year={year}&month={month}", Global.Instance.header);
		}
	}

	public void DailyLevelHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			ILogger logger = _logger;
			bool enabled;
			ZLoggerInformationInterpolatedStringHandler message = new ZLoggerInformationInterpolatedStringHandler(10, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("每日关卡获取失败: ");
				message.AppendFormatted(Global.Instance.GetHTTPRequestErrorMessage(result), 0, null, "Global.Instance.GetHTTPRequestErrorMessage(result)");
			}
			logger.ZLogInformation(ref message);
			dailyLevelRequesting = false;
			return;
		}
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		if (json.Data.VariantType != Variant.Type.Nil)
		{
			Dictionary dictionary = (Dictionary)json.Data;
			if (ResourceManager.Instance.DAILY_LEVEL_DATA.Count == 0)
			{
				foreach (Variant key in dictionary.Keys)
				{
					string text = (string)key;
					ResourceManager.Instance.DAILY_LEVEL_DATA[text] = dictionary[text];
				}
			}
			else
			{
				if (dictionary.ContainsKey("LevelDateMap"))
				{
					Dictionary dictionary2 = (Dictionary)dictionary["LevelDateMap"];
					Dictionary dictionary3 = (Dictionary)ResourceManager.Instance.DAILY_LEVEL_DATA["LevelDateMap"];
					foreach (Variant key2 in dictionary2.Keys)
					{
						string text2 = (string)key2;
						dictionary3[text2] = dictionary2[text2];
					}
				}
				if (dictionary.ContainsKey("LevelMeta"))
				{
					Dictionary dictionary4 = (Dictionary)dictionary["LevelMeta"];
					if (!ResourceManager.Instance.DAILY_LEVEL_DATA.ContainsKey("LevelMeta") || ResourceManager.Instance.DAILY_LEVEL_DATA["LevelMeta"].VariantType != Variant.Type.Dictionary)
					{
						ResourceManager.Instance.DAILY_LEVEL_DATA["LevelMeta"] = new Dictionary();
					}
					Dictionary dictionary5 = (Dictionary)ResourceManager.Instance.DAILY_LEVEL_DATA["LevelMeta"];
					foreach (Variant key3 in dictionary4.Keys)
					{
						string text3 = (string)key3;
						dictionary5[text3] = dictionary4[text3];
					}
				}
				if (dictionary.ContainsKey("LevelDateMeta"))
				{
					Dictionary dictionary6 = (Dictionary)dictionary["LevelDateMeta"];
					if (!ResourceManager.Instance.DAILY_LEVEL_DATA.ContainsKey("LevelDateMeta") || ResourceManager.Instance.DAILY_LEVEL_DATA["LevelDateMeta"].VariantType != Variant.Type.Dictionary)
					{
						ResourceManager.Instance.DAILY_LEVEL_DATA["LevelDateMeta"] = new Dictionary();
					}
					Dictionary dictionary7 = (Dictionary)ResourceManager.Instance.DAILY_LEVEL_DATA["LevelDateMeta"];
					foreach (Variant key4 in dictionary6.Keys)
					{
						string text4 = (string)key4;
						dictionary7[text4] = dictionary6[text4];
					}
				}
			}
			string text5 = $"{dailyLevelRequestYear:D}-{dailyLevelRequestMonth:D2}";
			dailyLevelLoadedMonths[text5] = true;
			dailyLevelGetOver = true;
			dailyLevelRequesting = false;
			this.OnDailyLevelMonthLoaded?.Invoke(dailyLevelRequestYear, dailyLevelRequestMonth);
		}
		else
		{
			dailyLevelRequesting = false;
		}
	}

	public void GetOnlineLevelPage(int pageIndex = 1, string suffix = "", string suffix2 = "", string search = "", string tags = "", string authorUid = "")
	{
		onlineLevelHTTPRequest.CancelRequest();
		onlineLevelHTTPRequest.Request(BuildOnlineLevelPageUrl(pageIndex, suffix, suffix2, search, tags, authorUid), Global.Instance.header);
	}

	internal static string BuildOnlineLevelPageUrl(int pageIndex = 1, string suffix = "", string suffix2 = "", string search = "", string tags = "", string authorUid = "")
	{
		pageIndex = Mathf.Clamp(pageIndex, 1, 100000);
		string text = $"https://api.pvzhe.com/workshop/levels?page={pageIndex}{suffix}{suffix2}";
		if (search != "")
		{
			text = text + "&search=" + Uri.EscapeDataString(search);
		}
		if (tags != "")
		{
			text = text + "&tags=" + Uri.EscapeDataString(tags);
		}
		if (!string.IsNullOrEmpty(authorUid))
		{
			text = text + "&authorUid=" + Uri.EscapeDataString(authorUid);
		}
		return text;
	}

	public void OnlineLevelHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L || responseCode != 200)
		{
			this.OnOnlineLevelGet?.Invoke(new Dictionary());
			return;
		}
		Json json = new Json();
		if (json.Parse(body.GetStringFromUtf8()) == Error.Ok && json.Data.VariantType == Variant.Type.Dictionary)
		{
			this.OnOnlineLevelGet?.Invoke((Dictionary)json.Data);
		}
		else
		{
			this.OnOnlineLevelGet?.Invoke(new Dictionary());
		}
	}

	public void GetWorkshopTags()
	{
		workshopTagsHTTPRequest.CancelRequest();
		workshopTagsHTTPRequest.Request("https://api.pvzhe.com/workshop/tags", Global.Instance.header);
	}

	public void WorkshopTagsHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			this.OnWorkshopTagsGet?.Invoke(new Godot.Collections.Array());
			return;
		}
		Json json = new Json();
		json.Parse(body.GetStringFromUtf8());
		if (json.Data.VariantType != Variant.Type.Nil && json.Data.VariantType == Variant.Type.Array)
		{
			this.OnWorkshopTagsGet?.Invoke((Godot.Collections.Array)json.Data);
		}
		else
		{
			this.OnWorkshopTagsGet?.Invoke(new Godot.Collections.Array());
		}
	}

	public void OnlineLevelPost(string levelId, string suffix = "abandon")
	{
		_onlineLevelStatistics.Enqueue(levelId, suffix, Global.Instance.header);
	}

	public void ShareFile(byte[] data)
	{
		exportFileHTTPRequest.CancelRequest();
		List<string> list = new List<string>();
		string[] header = Global.Instance.header;
		foreach (string item in header)
		{
			list.Add(item);
		}
		list.Add("Content-Type: application/octet-stream");
		byte[] body = data.Compress(FileAccess.CompressionMode.GZip);
		exportFileHTTPRequest.RequestRaw("https://api.pvzhe.com/save_share", list.ToArray(), HttpMethod.Post, body);
	}

	public void ShareLevel(byte[] data)
	{
		exportFileHTTPRequest.CancelRequest();
		List<string> list = new List<string>();
		string[] header = Global.Instance.header;
		foreach (string item in header)
		{
			list.Add(item);
		}
		list.Add("Content-Type: application/octet-stream");
		byte[] body = data.Compress(FileAccess.CompressionMode.GZip);
		exportFileHTTPRequest.RequestRaw("https://api.pvzhe.com/save_share", list.ToArray(), HttpMethod.Post, body);
	}

	public void GetSharedFile(string code)
	{
		loadFileHTTPRequest.CancelRequest();
		loadFileHTTPRequest.Request("https://api.pvzhe.com/save_share?code=" + code, Global.Instance.header);
	}

	public void ExportFileHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			this.OnShareLevelFailed?.Invoke(Global.Instance.GetHTTPRequestErrorMessage(result));
			return;
		}
		JsonDocument jsonDocument = null;
		try
		{
			jsonDocument = JsonDocument.Parse(body);
		}
		catch (JsonException)
		{
			this.OnShareLevelFailed?.Invoke($"请求失败，状态码: {responseCode}");
			return;
		}
		using (jsonDocument)
		{
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				this.OnShareLevelFailed?.Invoke($"请求失败，状态码: {responseCode}");
			}
			else if (responseCode == 200)
			{
				JsonElement value;
				string code = ((rootElement.TryGetProperty("code", out value) && value.ValueKind == JsonValueKind.String) ? (value.GetString() ?? "") : "");
				JsonElement value2;
				long expireAt = ((rootElement.TryGetProperty("expireAt", out value2) && value2.ValueKind == JsonValueKind.Number) ? value2.GetInt64() : 0);
				JsonElement value3;
				long expireSeconds = ((rootElement.TryGetProperty("expireSeconds", out value3) && value3.ValueKind == JsonValueKind.Number) ? value3.GetInt64() : 0);
				this.OnShareLevelSuccess?.Invoke(code, expireAt, expireSeconds);
			}
			else
			{
				JsonElement value4;
				string message = ((rootElement.TryGetProperty("message", out value4) && value4.ValueKind == JsonValueKind.String) ? (value4.GetString() ?? "未知错误") : "未知错误");
				this.OnShareLevelFailed?.Invoke(message);
			}
		}
	}

	public void LoadFileHTTPRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (result != 0L)
		{
			this.OnGetSharedLevelFailed?.Invoke(Global.Instance.GetHTTPRequestErrorMessage(result));
			return;
		}
		if (responseCode == 200)
		{
			this.OnGetSharedLevelSuccess?.Invoke(body);
			return;
		}
		string message = "未知错误";
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(body);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind == JsonValueKind.Object && rootElement.TryGetProperty("message", out var value) && value.ValueKind == JsonValueKind.String)
			{
				message = value.GetString() ?? "未知错误";
			}
		}
		catch (JsonException)
		{
		}
		this.OnGetSharedLevelFailed?.Invoke(message);
	}

	public InternetServerManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/InternetServerManager");
	}
}
