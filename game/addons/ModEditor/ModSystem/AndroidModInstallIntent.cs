using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

public static class AndroidModInstallIntent
{
	public sealed class StartupResult
	{
		public bool Handled { get; internal set; }

		public bool Success { get; internal set; }

		public string Message { get; internal set; } = "";

		public XWModPackageInstaller.InstallResult InstallResult { get; internal set; }
	}

	public sealed class PreparedStartupRequest : IDisposable
	{
		private bool _installAttempted;

		internal XWModPackageInstaller.PreparedImport PreparedImport { get; init; }

		public string RequestId { get; init; } = "";

		public string SourceAuthority { get; init; } = "";

		public string Sha256 => PreparedImport?.Sha256 ?? "";

		public XWModManifest Manifest => PreparedImport?.Manifest;

		internal bool TryBeginInstall()
		{
			if (_installAttempted)
			{
				return false;
			}
			_installAttempted = true;
			return true;
		}

		public void Dispose()
		{
			PreparedImport?.Dispose();
		}
	}

	public sealed class PrepareResult
	{
		public bool Handled { get; internal set; }

		public bool Success { get; internal set; }

		public string Message { get; internal set; } = "";

		public PreparedStartupRequest Request { get; internal set; }

		public bool RequiresConfirmation => Request != null;
	}

	public const string InstallAction = "xvwugame.PlantsVsZombies.action.INSTALL_PMOD";

	public const string PackageMimeType = "application/vnd.pvzhybrid.pmod";

	private const int ProtocolVersion = 1;

	private const int MaxReceipts = 32;

	private const string ReceiptFileName = ".android_install_receipts";

	private const string PendingReceiptState = "pending";

	private const string CompletedReceiptState = "completed";

	public static PrepareResult PrepareStartupIntent()
	{
		if (!OperatingSystem.IsAndroid())
		{
			return new PrepareResult();
		}
		try
		{
			if (!TryGetActivity(out var activity))
			{
				return FailedPreparation("AndroidRuntime 不可用，无法读取 Mod 安装请求。");
			}
			if (!(activity.Call("getIntent").AsGodotObject() is JavaObject javaObject) || !string.Equals(javaObject.Call("getAction").AsString(), "xvwugame.PlantsVsZombies.action.INSTALL_PMOD", StringComparison.Ordinal))
			{
				return new PrepareResult();
			}
			string text = javaObject.Call("getStringExtra", "request_id").AsString();
			if (!Guid.TryParse(text, out var _))
			{
				return FailedPreparation("Mod 安装请求 ID 无效。");
			}
			int num = javaObject.Call("getIntExtra", "protocol_version", 0).AsInt32();
			if (num != 1)
			{
				return FailedPreparation($"不支持的 Mod 安装协议版本：{num}。");
			}
			string text2 = javaObject.Call("getType").AsString();
			if (!string.Equals(text2, "application/vnd.pvzhybrid.pmod", StringComparison.Ordinal))
			{
				return FailedPreparation("不支持的 Mod 安装 MIME：" + text2 + "。");
			}
			string text3 = (javaObject.Call("getData").AsGodotObject() as JavaObject)?.Call("toString").AsString() ?? "";
			if (!TryValidateContentSource(text3, out var sourceAuthority))
			{
				return FailedPreparation("Mod 安装请求必须提供有效的 content:// URI。");
			}
			string text4 = javaObject.Call("getStringExtra", "sha256").AsString();
			if (!IsSha256(text4))
			{
				return FailedPreparation("自动安装请求缺少有效的 SHA256。");
			}
			if (!XWModPackageInstaller.TryPrepare(text3, text4, "", out var prepared, out var diagnostic))
			{
				return FailedPreparation(diagnostic);
			}
			try
			{
				if (CheckReceipt(text, prepared) == "completed")
				{
					prepared.Dispose();
					return new PrepareResult
					{
						Handled = true,
						Success = true,
						Message = "该 Mod 安装请求已经处理。"
					};
				}
			}
			catch
			{
				prepared.Dispose();
				throw;
			}
			return new PrepareResult
			{
				Handled = true,
				Request = new PreparedStartupRequest
				{
					RequestId = text,
					SourceAuthority = sourceAuthority,
					PreparedImport = prepared
				}
			};
		}
		catch (Exception ex)
		{
			return FailedPreparation("处理 Android Mod 安装请求失败：" + ex.GetBaseException().Message);
		}
	}

	public static StartupResult InstallPreparedRequest(PreparedStartupRequest request)
	{
		if (request?.PreparedImport?.Manifest == null)
		{
			return FailedHandled("待确认的 Mod 安装请求不存在或已经失效。");
		}
		if (!request.TryBeginInstall())
		{
			return FailedHandled("该 Mod 安装请求已尝试处理。");
		}
		try
		{
			using (XWModInstallTransaction.Enter())
			{
				if (CheckReceipt(request.RequestId, request.PreparedImport) == "completed")
				{
					return new StartupResult
					{
						Handled = true,
						Success = true,
						Message = "该 Mod 安装请求已经处理。"
					};
				}
				SavePendingReceipt(request.RequestId, request.PreparedImport);
				request.PreparedImport.AndroidRequestId = request.RequestId;
				XWModPackageInstaller.InstallResult installResult = XWModPackageInstaller.Install(request.PreparedImport, enableAfterInstall: true);
				return new StartupResult
				{
					Handled = true,
					Success = installResult.Success,
					Message = installResult.Message,
					InstallResult = installResult
				};
			}
		}
		catch (Exception ex)
		{
			return FailedHandled("处理 Android Mod 安装请求失败：" + ex.GetBaseException().Message);
		}
	}

	private static bool TryValidateContentSource(string sourceUri, out string sourceAuthority)
	{
		sourceAuthority = "";
		if (!Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri result) || !string.Equals(result.Scheme, "content", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(result.Authority))
		{
			return false;
		}
		sourceAuthority = result.Authority;
		return true;
	}

	private static bool TryGetActivity(out JavaObject activity)
	{
		activity = null;
		if (!Engine.HasSingleton("AndroidRuntime"))
		{
			return false;
		}
		GodotObject singleton = Engine.GetSingleton("AndroidRuntime");
		activity = singleton.Call("getActivity").AsGodotObject() as JavaObject;
		return activity != null;
	}

	internal static string CheckReceipt(string requestId, XWModPackageInstaller.PreparedImport prepared)
	{
		if (!Guid.TryParse(requestId, out var _) || prepared?.Manifest == null)
		{
			throw new InvalidDataException("安装请求身份无效。");
		}
		string defaultModsDirectory = XWModManager.GetDefaultModsDirectory();
		if (File.Exists(XWModInstallTransaction.JournalPath(defaultModsDirectory)))
		{
			throw new InvalidDataException("仍有未完成的安装事务，请完成恢复后重试。");
		}
		string[] array = ReadReceipt(defaultModsDirectory, requestId);
		if (array == null)
		{
			return "";
		}
		if (array.Length < 3 || !IsSha256(array[1]) || !string.Equals(array[1], prepared.Sha256, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("同一安装请求的包身份发生变化，拒绝重复使用请求 ID。");
		}
		if (array.Length > 3 && !string.Equals(array[3], prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("安装回执的 Mod ID 不匹配。");
		}
		if (array[2] == "completed")
		{
			return "completed";
		}
		bool flag = array.Length != 7;
		if (!flag)
		{
			string text = array[2];
			bool flag2 = ((text == "pending" || text == "retry") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			throw new InvalidDataException("旧安装回执无法确认事务结果，保留记录并拒绝自动重试。");
		}
		string a = Encoding.UTF8.GetString(Convert.FromBase64String(array[4]));
		if (!bool.TryParse(array[6], out var result2) || !string.Equals(a, prepared.ExistingPackagePath, StringComparison.OrdinalIgnoreCase) || !string.Equals(array[5], prepared.ExistingPackageSha256, StringComparison.OrdinalIgnoreCase) || result2 != prepared.ExistingPackageWasEnabled)
		{
			throw new InvalidDataException("安装回执与恢复后的旧状态不一致，保留记录供诊断。");
		}
		return "retry";
	}

	internal static void SavePendingReceipt(string requestId, XWModPackageInstaller.PreparedImport prepared)
	{
		CheckReceipt(requestId, prepared);
		WriteReceipt(XWModManager.GetDefaultModsDirectory(), new string[7]
		{
			requestId,
			prepared.Sha256,
			"pending",
			prepared.Manifest.Id,
			Convert.ToBase64String(Encoding.UTF8.GetBytes(prepared.ExistingPackagePath)),
			prepared.ExistingPackageSha256,
			prepared.ExistingPackageWasEnabled.ToString()
		});
	}

	internal static void RecordTransactionOutcome(string root, XWModInstallTransaction.Journal journal, bool committed)
	{
		if (!string.IsNullOrWhiteSpace(journal.RequestId))
		{
			string[] array = ReadReceipt(root, journal.RequestId);
			if (array == null || array.Length != 7 || !string.Equals(array[1], journal.NewSha256, StringComparison.OrdinalIgnoreCase) || !string.Equals(array[3], journal.ModId, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("事务与 Android 回执身份不一致，保留事务记录。");
			}
			array[2] = (committed ? "completed" : "retry");
			WriteReceipt(root, array);
		}
	}

	private static string[] ReadReceipt(string root, string requestId)
	{
		string path = Path.Combine(root, ".android_install_receipts");
		if (!File.Exists(path))
		{
			return null;
		}
		return File.ReadLines(path).LastOrDefault((string line) => line.StartsWith(requestId + "|", StringComparison.OrdinalIgnoreCase))?.Split('|');
	}

	private static void WriteReceipt(string root, string[] fields)
	{
		string text = Path.Combine(root, ".android_install_receipts");
		Directory.CreateDirectory(root);
		List<string> list = (File.Exists(text) ? (from line in File.ReadAllLines(text)
			where !string.IsNullOrWhiteSpace(line) && !line.StartsWith(fields[0] + "|", StringComparison.OrdinalIgnoreCase)
			select line).ToList() : new List<string>());
		list.Add(string.Join('|', fields));
		int num = list.Count((string line) => line.Split('|').ElementAtOrDefault(2) == "completed");
		int num2 = 0;
		while (num > 32 && num2 < list.Count)
		{
			if (list[num2].Split('|').ElementAtOrDefault(2) == "completed")
			{
				list.RemoveAt(num2);
				num--;
			}
			else
			{
				num2++;
			}
		}
		string text2 = text + ".tmp";
		using (FileStream fileStream = new FileStream(text2, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
		{
			using StreamWriter streamWriter = new StreamWriter(fileStream);
			foreach (string item in list)
			{
				streamWriter.WriteLine(item);
			}
			streamWriter.Flush();
			fileStream.Flush(flushToDisk: true);
		}
		File.Move(text2, text, overwrite: true);
	}

	private static bool IsSha256(string value)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 64)
		{
			return false;
		}
		return value.Trim().All(Uri.IsHexDigit);
	}

	private static string GetReceiptPath()
	{
		return Path.Combine(XWModManager.GetDefaultModsDirectory(), ".android_install_receipts");
	}

	private static PrepareResult FailedPreparation(string message)
	{
		return new PrepareResult
		{
			Handled = true,
			Success = false,
			Message = message
		};
	}

	private static StartupResult FailedHandled(string message)
	{
		return new StartupResult
		{
			Handled = true,
			Success = false,
			Message = message
		};
	}
}
