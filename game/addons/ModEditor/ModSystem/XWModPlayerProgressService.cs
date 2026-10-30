using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModPlayerProgressService
{
	public enum ProgressStatus
	{
		Missing,
		Healthy,
		Recoverable,
		Corrupt
	}

	private sealed record Profile(ConfigFile File, string Path, ProgressStatus Status);

	private static readonly System.Collections.Generic.Dictionary<string, Profile> Profiles = new System.Collections.Generic.Dictionary<string, Profile>(StringComparer.Ordinal);

	private static string User => GameSaveManager.Instance?.GetUserCurrent() ?? "";

	private static string Root => "user://Csharp/ModProgress/" + Hash(User);

	private static ConfigFile NewProfile(string owner)
	{
		ConfigFile configFile = new ConfigFile();
		configFile.SetValue("meta", "schema", 1);
		configFile.SetValue("meta", "owner", owner);
		return configFile;
	}

	private static bool TryReadProfile(string path, string owner, out ConfigFile file)
	{
		file = new ConfigFile();
		if (!Godot.FileAccess.FileExists(path))
		{
			return false;
		}
		try
		{
			return file.Load(path) == Error.Ok && file.GetValue("meta", "schema", 0).AsInt32() == 1 && string.Equals(file.GetValue("meta", "owner", "").AsString(), owner, StringComparison.OrdinalIgnoreCase) && ValidProfileValues(file);
		}
		catch
		{
			return false;
		}
	}

	private static bool ValidProfileValues(ConfigFile file)
	{
		string[] array = new string[2] { "levels", "packets" };
		foreach (string section in array)
		{
			if (!file.HasSection(section))
			{
				continue;
			}
			string[] sectionKeys = file.GetSectionKeys(section);
			foreach (string key in sectionKeys)
			{
				Variant value = file.GetValue(section, key);
				if (value.VariantType != Variant.Type.Dictionary)
				{
					return false;
				}
				Dictionary dictionary = value.AsGodotDictionary();
				string[] array2 = new string[5] { "Normal", "Difficult", "Ultimate", "Mower", "Love" };
				foreach (string text in array2)
				{
					if (dictionary.TryGetValue(text, out var value2) && value2.VariantType != Variant.Type.Bool)
					{
						return false;
					}
				}
				if (!dictionary.TryGetValue("Key", out var value3))
				{
					continue;
				}
				if (value3.VariantType != Variant.Type.Dictionary)
				{
					return false;
				}
				array2 = new string[3] { "Finish", "Played", "Like" };
				foreach (string text2 in array2)
				{
					if (value3.AsGodotDictionary().TryGetValue(text2, out var value4) && value4.VariantType != Variant.Type.Int)
					{
						return false;
					}
				}
			}
		}
		if (file.HasSection("unlocks"))
		{
			array = file.GetSectionKeys("unlocks");
			foreach (string key2 in array)
			{
				if (file.GetValue("unlocks", key2).VariantType != Variant.Type.Bool)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static ProgressStatus GetStatus(string owner)
	{
		if (User.Length != 0)
		{
			return GetProfile(owner).Status;
		}
		return ProgressStatus.Missing;
	}

	private static bool CanWrite(string owner)
	{
		ProgressStatus status = GetStatus(owner);
		if ((uint)status <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool CanPlay(string owner, out string reason)
	{
		reason = GetStatus(owner) switch
		{
			ProgressStatus.Recoverable => "此 Mod 的进度文件无法读取，原文件已保护。请在选项 → Mod 管理中从备份恢复后继续。", 
			ProgressStatus.Corrupt => "此 Mod 的进度文件损坏，原文件已保护。请在选项 → Mod 管理中确认重置后继续。", 
			_ => "", 
		};
		return reason.Length == 0;
	}

	public static bool TryRestoreBackup(string owner, out string reason)
	{
		return RepairProfile(owner, reset: false, out reason);
	}

	public static bool TryResetProgress(string owner, out string reason)
	{
		return RepairProfile(owner, reset: true, out reason);
	}

	private static bool RepairProfile(string owner, bool reset, out string reason)
	{
		reason = "";
		if (User.Length == 0)
		{
			reason = "请先选择游戏账号。";
			return false;
		}
		try
		{
			Profile profile = GetProfile(owner);
			string text = ProjectSettings.GlobalizePath(profile.Path);
			ConfigFile file;
			if (reset)
			{
				file = NewProfile(owner);
			}
			else if (!TryReadProfile(text + ".bak", owner, out file))
			{
				reason = "没有可用的进度备份，原文件未修改。";
				return false;
			}
			string text2 = Path.Combine(Path.GetDirectoryName(text), "Recovery", Path.GetFileNameWithoutExtension(text), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(text2);
			string[] array = new string[2]
			{
				text,
				text + ".bak"
			};
			foreach (string text3 in array)
			{
				if (File.Exists(text3))
				{
					File.Copy(text3, Path.Combine(text2, Path.GetFileName(text3)), overwrite: false);
				}
			}
			string text4 = text + ".repair.tmp";
			Error error = file.Save(text4);
			if (error != Error.Ok || !TryReadProfile(text4, owner, out var _))
			{
				throw new IOException("恢复文件未能完整写入：" + error);
			}
			File.Move(text4, text, overwrite: true);
			Profiles.Remove(profile.Path);
			reason = (reset ? "已重置此账号的 Mod 进度配置。" : "已从备份恢复 Mod 进度。") + "中途战斗存档保留。原文件备份：" + text2;
			return true;
		}
		catch (Exception ex)
		{
			reason = "进度恢复未完成，原文件已保留：" + ex.Message;
			return false;
		}
	}

	private static string Hash(string value)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
	}

	private static Profile GetProfile(string owner)
	{
		if (string.IsNullOrWhiteSpace(owner))
		{
			throw new ArgumentException("Mod owner is required", "owner");
		}
		string text = Root + "/" + Hash(owner.ToLowerInvariant()) + ".cfg";
		if (Profiles.TryGetValue(text, out var value))
		{
			return value;
		}
		ConfigFile file = new ConfigFile();
		if (Godot.FileAccess.FileExists(text))
		{
			if (!TryReadProfile(text, owner, out file))
			{
				ConfigFile file2;
				bool flag = TryReadProfile(text + ".bak", owner, out file2);
				return Profiles[text] = new Profile(flag ? file2 : NewProfile(owner), text, flag ? ProgressStatus.Recoverable : ProgressStatus.Corrupt);
			}
		}
		else
		{
			if (Godot.FileAccess.FileExists(text + ".bak"))
			{
				ConfigFile file3;
				bool flag2 = TryReadProfile(text + ".bak", owner, out file3);
				return Profiles[text] = new Profile(flag2 ? file3 : NewProfile(owner), text, flag2 ? ProgressStatus.Recoverable : ProgressStatus.Corrupt);
			}
			file.SetValue("meta", "schema", 1);
			file.SetValue("meta", "owner", owner);
		}
		return Profiles[text] = new Profile(file, text, Godot.FileAccess.FileExists(text) ? ProgressStatus.Healthy : ProgressStatus.Missing);
	}

	public static Dictionary GetLevel(XWModLevelIdentity identity)
	{
		if (identity == null || User.Length == 0)
		{
			return new Dictionary();
		}
		ConfigFile file = GetProfile(identity.OwnerModId).File;
		string key = LevelKey(identity);
		if (!file.HasSectionKey("levels", key))
		{
			file.SetValue("levels", key, new Dictionary
			{
				["Normal"] = false,
				["Difficult"] = false,
				["Ultimate"] = false,
				["Mower"] = false,
				["Key"] = new Dictionary
				{
					["Finish"] = 0,
					["Played"] = 0,
					["Like"] = 0
				}
			});
		}
		return file.GetValue("levels", key).AsGodotDictionary();
	}

	public static void SetLevel(XWModLevelIdentity identity, Dictionary value)
	{
		if (!(identity == null) && User.Length != 0 && CanWrite(identity.OwnerModId))
		{
			GetProfile(identity.OwnerModId).File.SetValue("levels", LevelKey(identity), value);
			Flush(identity.OwnerModId);
		}
	}

	private static string LevelKey(XWModLevelIdentity identity)
	{
		return Hash(Json.Stringify(new Godot.Collections.Array { identity.CatalogKey, identity.LevelSaveKey }));
	}

	public static string ProgressPath(XWModLevelIdentity identity)
	{
		return Root + "/Battles/" + Hash(Json.Stringify(identity.ToDictionary())) + ".tres";
	}

	public static int FinishCount(XWModLevelIdentity identity)
	{
		return GetLevel(identity).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Finish", 0)
			.AsInt32();
	}

	public static bool IsUnlocked(string owner, string category, string key)
	{
		if (User.Length > 0 && CanWrite(owner))
		{
			return GetProfile(owner).File.GetValue("unlocks", category + "/" + key, false).AsBool();
		}
		return false;
	}

	public static void Unlock(string owner, string category, string key)
	{
		if (User.Length != 0 && !string.IsNullOrEmpty(key) && CanWrite(owner))
		{
			GetProfile(owner).File.SetValue("unlocks", category + "/" + key, true);
			Flush(owner);
		}
	}

	public static bool TryPacketUnlock(TowerDefensePacketConfig packet, out bool unlocked)
	{
		unlocked = false;
		string contentOwner = XWModContentCatalog.GetContentOwner("Packet", packet.saveKey);
		if (contentOwner.Length == 0)
		{
			return false;
		}
		if (packet.unlockCheckList == null || packet.unlockCheckList.Count == 0)
		{
			unlocked = true;
			return true;
		}
		if (!CanWrite(contentOwner))
		{
			return true;
		}
		if (IsUnlocked(contentOwner, "Packet", packet.saveKey))
		{
			unlocked = true;
			return true;
		}
		foreach (UnlockConditionBaseConfig unlockCheck in packet.unlockCheckList)
		{
			if (!(unlockCheck is XWModProgressUnlockCondition xWModProgressUnlockCondition) || !xWModProgressUnlockCondition.Check())
			{
				return true;
			}
		}
		Unlock(contentOwner, "Packet", packet.saveKey);
		unlocked = true;
		return true;
	}

	public static void Flush(string owner)
	{
		if (User.Length == 0)
		{
			return;
		}
		Profile profile = GetProfile(owner);
		if (CanWrite(owner))
		{
			string text = ProjectSettings.GlobalizePath(profile.Path);
			Directory.CreateDirectory(Path.GetDirectoryName(text));
			string text2 = text + ".tmp";
			Error error = profile.File.Save(text2);
			if (error != Error.Ok)
			{
				throw new IOException($"Mod 进度保存失败：{error}");
			}
			if (File.Exists(text) && TryReadProfile(text, owner, out var _))
			{
				File.Copy(text, text + ".bak.tmp", overwrite: true);
				File.Move(text + ".bak.tmp", text + ".bak", overwrite: true);
			}
			File.Move(text2, text, overwrite: true);
			Profiles[profile.Path] = profile with
			{
				Status = ProgressStatus.Healthy
			};
		}
	}

	internal static void ClearCache()
	{
		Profiles.Clear();
	}

	public static Dictionary GetPacketState(string key)
	{
		string contentOwner = XWModContentCatalog.GetContentOwner("Packet", key);
		if (contentOwner.Length == 0)
		{
			return GameSaveManager.Instance.GetTowerDefensePacketValue(key);
		}
		if (User.Length == 0)
		{
			return new Dictionary();
		}
		return GetProfile(contentOwner).File.GetValue("packets", key, new Dictionary { ["Love"] = false }).AsGodotDictionary();
	}

	public static void SetPacketState(string key, Dictionary value)
	{
		string contentOwner = XWModContentCatalog.GetContentOwner("Packet", key);
		if (contentOwner.Length == 0)
		{
			GameSaveManager.Instance.SetTowerDefensePacketValue(key, value);
			GameSaveManager.Instance.Save();
		}
		else if (User.Length > 0 && CanWrite(contentOwner))
		{
			GetProfile(contentOwner).File.SetValue("packets", key, value);
			Flush(contentOwner);
		}
	}
}
