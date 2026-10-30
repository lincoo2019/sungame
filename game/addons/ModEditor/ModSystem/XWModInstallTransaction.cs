using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Threading;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModInstallTransaction
{
	public sealed class Journal
	{
		public string Operation { get; set; } = "";

		public string ModId { get; set; } = "";

		public string TargetPath { get; set; } = "";

		public string StagedPath { get; set; } = "";

		public string BackupPath { get; set; } = "";

		public string PreviousSha256 { get; set; } = "";

		public string NewSha256 { get; set; } = "";

		public bool HadPreviousPackage { get; set; }

		public bool WasEnabled { get; set; }

		public bool DesiredEnabled { get; set; }

		public string Phase { get; set; } = "prepared";

		public string RequestId { get; set; } = "";
	}

	public sealed class PendingDelete
	{
		public string ModId { get; set; } = "";

		public string PackagePath { get; set; } = "";

		public string PackageSha256 { get; set; } = "";
	}

	public sealed class Mutation : IDisposable
	{
		private bool _released;

		internal Mutation()
		{
			Monitor.Enter(Gate);
		}

		public void Dispose()
		{
			if (!_released)
			{
				_released = true;
				Monitor.Exit(Gate);
			}
		}
	}

	private const string JournalFileName = ".mod_install_transaction.json";

	private const string PendingDeleteFileName = ".mod_pending_deletes.json";

	private static readonly object Gate = new object();

	public static Mutation Enter()
	{
		return new Mutation();
	}

	public static string JournalPath(string modsDirectory)
	{
		return Path.Combine(modsDirectory, ".mod_install_transaction.json");
	}

	public static string PendingDeletePath(string modsDirectory)
	{
		return Path.Combine(modsDirectory, ".mod_pending_deletes.json");
	}

	public static void BeginInstall(string modsDirectory, Journal journal)
	{
		if (File.Exists(JournalPath(modsDirectory)))
		{
			throw new IOException("已有未完成的 Mod 事务，请先恢复后重试。");
		}
		journal.Operation = "install";
		journal.Phase = "prepared";
		WriteAtomic(JournalPath(modsDirectory), Serialize(journal));
	}

	public static void BeginDelete(string modsDirectory, Journal journal)
	{
		if (File.Exists(JournalPath(modsDirectory)))
		{
			throw new IOException("已有未完成的 Mod 事务，请先恢复后重试。");
		}
		journal.Operation = "delete";
		journal.Phase = "prepared";
		WriteAtomic(JournalPath(modsDirectory), Serialize(journal));
	}

	public static void SetPhase(string modsDirectory, Journal journal, string phase)
	{
		journal.Phase = phase;
		WriteAtomic(JournalPath(modsDirectory), Serialize(journal));
	}

	public static void Complete(string modsDirectory)
	{
		string path = JournalPath(modsDirectory);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public static IReadOnlyList<PendingDelete> LoadPendingDeletes(string modsDirectory)
	{
		if (!TryLoadPendingDeletes(modsDirectory, out var result, out var _))
		{
			return Array.Empty<PendingDelete>();
		}
		return result;
	}

	public static bool TryLoadPendingDeletes(string modsDirectory, out IReadOnlyList<PendingDelete> result, out string diagnostic)
	{
		result = Array.Empty<PendingDelete>();
		diagnostic = "";
		string path = PendingDeletePath(modsDirectory);
		if (!File.Exists(path))
		{
			return true;
		}
		try
		{
			JsonArray obj = (JsonNode.Parse(File.ReadAllText(path)) as JsonArray) ?? throw new InvalidDataException("待删除 Mod 记录必须是数组。");
			List<PendingDelete> list = new List<PendingDelete>();
			foreach (JsonNode item2 in obj)
			{
				if (!(item2 is JsonObject item))
				{
					throw new InvalidDataException("待删除 Mod 记录项无效。");
				}
				PendingDelete pendingDelete = new PendingDelete
				{
					ModId = ReadString(item, "ModId"),
					PackagePath = ReadString(item, "PackagePath"),
					PackageSha256 = ReadString(item, "PackageSha256")
				};
				if (string.IsNullOrWhiteSpace(pendingDelete.ModId) || string.IsNullOrWhiteSpace(pendingDelete.PackagePath) || !IsSafeModId(pendingDelete.ModId) || !IsSha256(pendingDelete.PackageSha256))
				{
					throw new InvalidDataException("待删除 Mod 记录缺少有效身份。");
				}
				ValidatePath(pendingDelete.PackagePath, modsDirectory, ".pmod");
				list.Add(pendingDelete);
			}
			result = list;
			return true;
		}
		catch (Exception ex)
		{
			diagnostic = ex.GetBaseException().Message;
			return false;
		}
	}

	public static void AddPendingDelete(string modsDirectory, PendingDelete pending)
	{
		ValidatePath(pending?.PackagePath ?? "", modsDirectory, ".pmod");
		if (pending == null || string.IsNullOrWhiteSpace(pending.ModId) || !IsSha256(pending.PackageSha256))
		{
			throw new InvalidDataException("待删除 Mod 身份无效。");
		}
		if (!TryLoadPendingDeletes(modsDirectory, out var result, out var diagnostic))
		{
			throw new InvalidDataException("待删除 Mod 记录损坏，拒绝覆盖：" + diagnostic);
		}
		List<PendingDelete> list = result.Where((PendingDelete item) => !string.Equals(item.ModId, pending.ModId, StringComparison.OrdinalIgnoreCase)).ToList();
		list.Add(pending);
		WriteAtomic(PendingDeletePath(modsDirectory), SerializePending(list));
	}

	public static void RemovePendingDelete(string modsDirectory, string modId)
	{
		if (!TryLoadPendingDeletes(modsDirectory, out var result, out var diagnostic))
		{
			throw new InvalidDataException("待删除 Mod 记录损坏，拒绝覆盖：" + diagnostic);
		}
		List<PendingDelete> list = result.Where((PendingDelete item) => !string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase)).ToList();
		string path = PendingDeletePath(modsDirectory);
		if (list.Count == 0)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		else
		{
			WriteAtomic(path, SerializePending(list));
		}
	}

	public static bool TryRecover(XWModManager manager, out string diagnostic)
	{
		diagnostic = "";
		if (manager == null)
		{
			return true;
		}
		using (Enter())
		{
			using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
			if (environmentMutation == null)
			{
				diagnostic = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。";
				return false;
			}
			string modsDirectory = manager.ModsDirectory;
			string path = JournalPath(modsDirectory);
			if (!File.Exists(path))
			{
				return true;
			}
			try
			{
				Journal journal = ParseJournal(JsonNode.Parse(File.ReadAllText(path)));
				if (journal == null || string.IsNullOrWhiteSpace(journal.Operation))
				{
					throw new InvalidDataException("Mod 事务记录无效。");
				}
				if (ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(mod.LoadedId, journal.ModId, StringComparison.OrdinalIgnoreCase)))
				{
					throw new InvalidOperationException("Mod 事务对应的运行实例仍在使用：" + journal.ModId + "。");
				}
				ValidatePath(journal.TargetPath, modsDirectory, ".pmod");
				if (!string.IsNullOrWhiteSpace(journal.BackupPath))
				{
					ValidateBackupPath(journal.BackupPath, journal.TargetPath, modsDirectory);
				}
				if (!string.IsNullOrWhiteSpace(journal.StagedPath))
				{
					ValidateStagedPath(journal.StagedPath, modsDirectory);
				}
				if (journal.Operation.Equals("install", StringComparison.OrdinalIgnoreCase))
				{
					if (journal.Phase.Equals("committed", StringComparison.OrdinalIgnoreCase))
					{
						if (!File.Exists(journal.TargetPath) || !MatchesHash(journal.TargetPath, journal.NewSha256))
						{
							throw new InvalidDataException("已提交安装的目标文件已被替换，保留事务备份。");
						}
						if (File.Exists(journal.BackupPath) && journal.HadPreviousPackage && !MatchesHash(journal.BackupPath, journal.PreviousSha256))
						{
							throw new InvalidDataException("已提交安装备份校验失败，保留事务文件。");
						}
						if (File.Exists(journal.StagedPath) && !MatchesHash(journal.StagedPath, journal.NewSha256))
						{
							throw new InvalidDataException("已提交安装暂存包校验失败，保留事务文件。");
						}
						AndroidModInstallIntent.RecordTransactionOutcome(modsDirectory, journal, committed: true);
						TryDelete(journal.BackupPath);
						TryDelete(journal.StagedPath);
						Complete(modsDirectory);
						return true;
					}
					if (journal.HadPreviousPackage && File.Exists(journal.BackupPath))
					{
						if (!MatchesHash(journal.BackupPath, journal.PreviousSha256))
						{
							throw new InvalidDataException("安装事务备份校验失败，拒绝覆盖当前文件。");
						}
						if (File.Exists(journal.TargetPath) && !MatchesHash(journal.TargetPath, journal.NewSha256))
						{
							throw new InvalidDataException("安装事务目标已被外部替换，拒绝回滚覆盖。");
						}
						if (File.Exists(journal.TargetPath))
						{
							File.Delete(journal.TargetPath);
						}
						File.Move(journal.BackupPath, journal.TargetPath);
					}
					else
					{
						if (journal.HadPreviousPackage && !File.Exists(journal.BackupPath) && (!File.Exists(journal.TargetPath) || !MatchesHash(journal.TargetPath, journal.PreviousSha256)))
						{
							throw new InvalidDataException("安装事务缺少备份且目标不是旧包，拒绝确认恢复。");
						}
						if (!journal.HadPreviousPackage && File.Exists(journal.TargetPath) && !MatchesHash(journal.TargetPath, journal.NewSha256))
						{
							throw new InvalidDataException("安装事务目标已被外部替换，拒绝删除。");
						}
						if (!journal.HadPreviousPackage && File.Exists(journal.TargetPath))
						{
							File.Delete(journal.TargetPath);
						}
					}
					if (File.Exists(journal.StagedPath) && !MatchesHash(journal.StagedPath, journal.NewSha256))
					{
						throw new InvalidDataException("安装暂存包已变化，保留事务文件。");
					}
					TryDelete(journal.StagedPath);
					manager.RestoreEnabledState(journal.ModId, journal.WasEnabled);
					AndroidModInstallIntent.RecordTransactionOutcome(modsDirectory, journal, committed: false);
					Complete(modsDirectory);
					return true;
				}
				if (journal.Operation.Equals("delete", StringComparison.OrdinalIgnoreCase))
				{
					if (journal.Phase.Equals("committed", StringComparison.OrdinalIgnoreCase))
					{
						if (File.Exists(journal.TargetPath) && !string.IsNullOrWhiteSpace(journal.PreviousSha256))
						{
							throw new InvalidDataException("已提交删除事务仍有目标文件，保留文件供诊断。");
						}
						if (File.Exists(journal.BackupPath) && !MatchesHash(journal.BackupPath, journal.PreviousSha256))
						{
							throw new InvalidDataException("已提交删除备份校验失败，保留事务文件。");
						}
						TryDelete(journal.BackupPath);
						Complete(modsDirectory);
						return true;
					}
					if (File.Exists(journal.TargetPath) && File.Exists(journal.BackupPath) && MatchesHash(journal.TargetPath, journal.PreviousSha256))
					{
						if (!MatchesHash(journal.BackupPath, journal.PreviousSha256))
						{
							throw new InvalidDataException("删除事务备份已变化，保留文件。");
						}
						TryDelete(journal.BackupPath);
					}
					else if (File.Exists(journal.TargetPath) && File.Exists(journal.BackupPath))
					{
						throw new InvalidDataException("删除事务目标已被替换，拒绝覆盖或删除。");
					}
					if (!File.Exists(journal.TargetPath) && File.Exists(journal.BackupPath) && !MatchesHash(journal.BackupPath, journal.PreviousSha256))
					{
						throw new InvalidDataException("删除事务备份校验失败。");
					}
					if (!File.Exists(journal.TargetPath) && File.Exists(journal.BackupPath))
					{
						File.Move(journal.BackupPath, journal.TargetPath);
					}
					if (!File.Exists(journal.TargetPath) && !File.Exists(journal.BackupPath))
					{
						throw new InvalidDataException("删除事务同时缺少目标和备份，拒绝确认恢复。");
					}
					if (File.Exists(journal.TargetPath) && !File.Exists(journal.BackupPath) && !MatchesHash(journal.TargetPath, journal.PreviousSha256))
					{
						throw new InvalidDataException("删除事务缺少备份且目标已被替换，拒绝恢复。");
					}
					manager.RestoreEnabledState(journal.ModId, journal.WasEnabled);
					Complete(modsDirectory);
					return true;
				}
				throw new InvalidDataException("未知 Mod 事务操作：" + journal.Operation + "。");
			}
			catch (Exception ex)
			{
				diagnostic = ex.GetBaseException().Message;
				return false;
			}
		}
	}

	public static bool TryHash(string path, out string sha256)
	{
		sha256 = "";
		try
		{
			using FileStream source = File.OpenRead(path);
			sha256 = Convert.ToHexString(SHA256.HashData(source));
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string Serialize(Journal journal)
	{
		return new JsonObject
		{
			["Operation"] = journal.Operation,
			["ModId"] = journal.ModId,
			["TargetPath"] = journal.TargetPath,
			["StagedPath"] = journal.StagedPath,
			["BackupPath"] = journal.BackupPath,
			["PreviousSha256"] = journal.PreviousSha256,
			["NewSha256"] = journal.NewSha256,
			["HadPreviousPackage"] = journal.HadPreviousPackage,
			["WasEnabled"] = journal.WasEnabled,
			["DesiredEnabled"] = journal.DesiredEnabled,
			["Phase"] = journal.Phase,
			["RequestId"] = journal.RequestId
		}.ToJsonString();
	}

	private static string SerializePending(IEnumerable<PendingDelete> pending)
	{
		JsonArray jsonArray = new JsonArray();
		foreach (PendingDelete item in pending)
		{
			jsonArray.Add((JsonNode?)new JsonObject
			{
				["ModId"] = item.ModId,
				["PackagePath"] = item.PackagePath,
				["PackageSha256"] = item.PackageSha256
			});
		}
		return jsonArray.ToJsonString();
	}

	private static Journal ParseJournal(JsonNode node)
	{
		JsonObject item = node as JsonObject;
		if (item == null)
		{
			throw new InvalidDataException("Mod 事务记录必须是对象。");
		}
		if (new string[11]
		{
			"Operation", "ModId", "TargetPath", "StagedPath", "BackupPath", "PreviousSha256", "NewSha256", "HadPreviousPackage", "WasEnabled", "DesiredEnabled",
			"Phase"
		}.Any((string key) => !item.ContainsKey(key)))
		{
			throw new InvalidDataException("Mod 事务记录缺少字段。");
		}
		Journal journal = new Journal
		{
			Operation = ReadString(item, "Operation"),
			ModId = ReadString(item, "ModId"),
			TargetPath = ReadString(item, "TargetPath"),
			StagedPath = ReadString(item, "StagedPath"),
			BackupPath = ReadString(item, "BackupPath"),
			PreviousSha256 = ReadString(item, "PreviousSha256"),
			NewSha256 = ReadString(item, "NewSha256"),
			HadPreviousPackage = ReadBool(item, "HadPreviousPackage"),
			WasEnabled = ReadBool(item, "WasEnabled"),
			DesiredEnabled = ReadBool(item, "DesiredEnabled"),
			Phase = ReadString(item, "Phase"),
			RequestId = ReadString(item, "RequestId")
		};
		if (!IsSafeModId(journal.ModId) || (journal.Operation.Equals("install", StringComparison.OrdinalIgnoreCase) && (!IsSha256(journal.NewSha256) || (journal.HadPreviousPackage && !IsSha256(journal.PreviousSha256)))) || (journal.Operation.Equals("delete", StringComparison.OrdinalIgnoreCase) && !IsSha256(journal.PreviousSha256)))
		{
			throw new InvalidDataException("Mod 事务身份或哈希无效。");
		}
		bool flag2;
		if (journal.Operation.Equals("install", StringComparison.OrdinalIgnoreCase))
		{
			bool flag;
			switch (journal.Phase)
			{
			case "prepared":
			case "package-replaced":
			case "committed":
			case "rollback-complete":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			flag2 = flag;
		}
		else
		{
			bool flag = journal.Operation.Equals("delete", StringComparison.OrdinalIgnoreCase);
			if (flag)
			{
				bool flag3;
				switch (journal.Phase)
				{
				case "prepared":
				case "package-moved":
				case "committed":
					flag3 = true;
					break;
				default:
					flag3 = false;
					break;
				}
				flag = flag3;
			}
			flag2 = flag;
		}
		if (!flag2)
		{
			throw new InvalidDataException("Mod 事务阶段无效。");
		}
		if (journal.RequestId.Length > 0 && (!Guid.TryParse(journal.RequestId, out var _) || journal.Operation != "install"))
		{
			throw new InvalidDataException("安装事务请求 ID 无效。");
		}
		return journal;
	}

	private static string ReadString(JsonObject item, string key)
	{
		return item[key]?.GetValue<string>() ?? "";
	}

	private static bool ReadBool(JsonObject item, string key)
	{
		return (item[key] ?? throw new InvalidDataException("Mod 事务字段无效：" + key + "。")).GetValue<bool>();
	}

	private static void WriteAtomic(string path, string json)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
		string text = path + ".tmp";
		using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			using StreamWriter streamWriter = new StreamWriter(fileStream);
			streamWriter.Write(json);
			streamWriter.Flush();
			fileStream.Flush(flushToDisk: true);
		}
		File.Move(text, path, overwrite: true);
	}

	private static void ValidatePath(string path, string root, string requiredSuffix)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new InvalidDataException("Mod 事务路径为空。");
		}
		string fullPath = Path.GetFullPath(path);
		string value = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("Mod 事务路径越过安装目录。");
		}
		if (requiredSuffix == ".pmod" && !string.Equals(Path.GetDirectoryName(fullPath), Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("安装包必须位于 Mods 顶层。");
		}
		if (!string.IsNullOrWhiteSpace(requiredSuffix) && !fullPath.EndsWith(requiredSuffix, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("Mod 事务文件类型无效。");
		}
		if (string.IsNullOrWhiteSpace(requiredSuffix) && !fullPath.EndsWith(".pmod", StringComparison.OrdinalIgnoreCase) && !fullPath.Contains(".pmod.backup-", StringComparison.OrdinalIgnoreCase) && !fullPath.Contains(".pmod.delete-backup-", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("Mod 事务备份文件类型无效。");
		}
	}

	private static void ValidateBackupPath(string path, string targetPath, string root)
	{
		ValidatePath(path, root, "");
		string fullPath = Path.GetFullPath(targetPath);
		string fullPath2 = Path.GetFullPath(path);
		string text = fullPath + ".";
		bool num = fullPath2.StartsWith(text + "backup-", StringComparison.OrdinalIgnoreCase) || fullPath2.StartsWith(text + "delete-backup-", StringComparison.OrdinalIgnoreCase);
		string text2 = fullPath2;
		int num2 = fullPath2.LastIndexOf('-') + 1;
		string text3 = text2.Substring(num2, text2.Length - num2);
		if (!num || text3.Length != 32 || !text3.All(Uri.IsHexDigit))
		{
			throw new InvalidDataException("Mod 事务备份路径无效。");
		}
	}

	private static void ValidateStagedPath(string path, string root)
	{
		ValidatePath(path, root, ".pmod.part");
		string b = Path.Combine(Path.GetFullPath(root), ".incoming") + Path.DirectorySeparatorChar;
		string fullPath = Path.GetFullPath(path);
		string fileName = Path.GetFileName(fullPath);
		if (!string.Equals(Path.GetDirectoryName(fullPath) + Path.DirectorySeparatorChar, b, StringComparison.OrdinalIgnoreCase) || fileName.Length != 32 + ".pmod.part".Length || !fileName.Substring(0, 32).All(Uri.IsHexDigit))
		{
			throw new InvalidDataException("Mod 事务暂存路径无效。");
		}
	}

	private static bool IsSha256(string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length == 64)
		{
			return value.Trim().All(Uri.IsHexDigit);
		}
		return false;
	}

	private static bool IsSafeModId(string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && value.Length <= 128)
		{
			return value.All(delegate(char character)
			{
				bool flag = char.IsLetterOrDigit(character);
				if (!flag)
				{
					bool flag2 = ((character == '-' || character == '.' || character == '_') ? true : false);
					flag = flag2;
				}
				return flag;
			});
		}
		return false;
	}

	private static bool MatchesHash(string path, string expected)
	{
		if (IsSha256(expected) && TryHash(path, out var sha))
		{
			return string.Equals(sha, expected, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static void TryDelete(string path)
	{
		if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
