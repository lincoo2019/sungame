using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using PVZHE.ModEditor.ModSystem.Validation;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModManager
{
	public sealed class ModEntry
	{
		public string Id { get; set; } = "";

		public string DisplayName { get; set; } = "";

		public string Version { get; set; } = "";

		public string PackagePath { get; set; } = "";

		public string DirectoryPath
		{
			get
			{
				return PackagePath;
			}
			set
			{
				PackagePath = value ?? "";
			}
		}

		public bool Enabled { get; set; }

		public bool HasManifest { get; set; }

		public XWModManifest Manifest { get; set; }

		public string Diagnostic { get; set; } = "";

		public bool Loaded { get; set; }

		public bool PendingDelete { get; set; }

		public string PackageSha256 { get; set; } = "";

		public string EffectiveState { get; set; } = "未应用";

		public string InstalledVersion { get; set; } = "";

		public string RunningVersion { get; set; } = "";

		public string RunningSha256 { get; set; } = "";

		public string EffectiveMode { get; set; } = "";

		public string LastApplyFailure { get; set; } = "";
	}

	public sealed class DeleteResult
	{
		public bool Success { get; set; }

		public bool PendingRestart { get; set; }

		public bool Blocked { get; set; }

		public string Message { get; set; } = "";

		public List<XWValidationIssue> Issues { get; set; } = new List<XWValidationIssue>();
	}

	public sealed class ApplyFailureRecord
	{
		public string ModId { get; internal set; } = "";

		public string Message { get; internal set; } = "";

		public string Version { get; internal set; } = "";

		public string PackageSha256 { get; internal set; } = "";
	}

	public sealed class SetEnabledResult
	{
		public bool Success { get; set; }

		public bool Changed { get; set; }

		public bool Blocked { get; set; }

		public List<XWValidationIssue> Issues { get; set; } = new List<XWValidationIssue>();
	}

	public sealed class ResolveEnabledResult
	{
		public List<ModEntry> LoadOrder { get; set; } = new List<ModEntry>();

		public List<XWValidationIssue> Issues { get; set; } = new List<XWValidationIssue>();
	}

	public const string ModsDirectoryName = "Mods";

	public const string EnabledStateFileName = "enabled_mods.json";

	private readonly Dictionary<string, XWModManifest> _loadedManifests = new Dictionary<string, XWModManifest>(StringComparer.OrdinalIgnoreCase);

	private static readonly object ApplyFailureGate = new object();

	private static readonly Dictionary<string, Dictionary<string, string>> RecentApplyFailures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

	private readonly string _modsDirectory;

	public string ModsDirectory => _modsDirectory;

	public string EnabledStatePath => Path.Combine(_modsDirectory, "enabled_mods.json");

	public IReadOnlyDictionary<string, XWModManifest> LoadedManifests => _loadedManifests;

	private string FailureDomain => Path.GetFullPath(_modsDirectory);

	public XWModManager(string modsDirectory = "")
	{
		_modsDirectory = (string.IsNullOrWhiteSpace(modsDirectory) ? GetDefaultModsDirectory() : Path.GetFullPath(modsDirectory));
	}

	public IReadOnlyList<ApplyFailureRecord> GetRecentApplyFailures()
	{
		lock (ApplyFailureGate)
		{
			if (!RecentApplyFailures.TryGetValue(FailureDomain, out var value))
			{
				return Array.Empty<ApplyFailureRecord>();
			}
			return value.Select((KeyValuePair<string, string> pair) => new ApplyFailureRecord
			{
				ModId = pair.Key,
				Message = pair.Value
			}).ToArray();
		}
	}

	private void RecordApplyFailure(string modId, string message)
	{
		if (string.IsNullOrWhiteSpace(modId))
		{
			return;
		}
		lock (ApplyFailureGate)
		{
			if (!RecentApplyFailures.TryGetValue(FailureDomain, out var value))
			{
				value = (RecentApplyFailures[FailureDomain] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
			}
			value[modId] = message ?? "Mod 应用失败。";
		}
	}

	private void ClearApplyFailure(string modId)
	{
		lock (ApplyFailureGate)
		{
			if (RecentApplyFailures.TryGetValue(FailureDomain, out var value))
			{
				value.Remove(modId);
			}
		}
	}

	public static string GetDefaultModsDirectory()
	{
		return Path.GetFullPath(ProjectSettings.GlobalizePath("user://Mods/"));
	}

	public static string GetGameDirectory()
	{
		if (Engine.IsEditorHint())
		{
			return ProjectSettings.GlobalizePath("res://");
		}
		string directoryName = Path.GetDirectoryName(OS.GetExecutablePath());
		if (!string.IsNullOrWhiteSpace(directoryName))
		{
			return directoryName;
		}
		return Directory.GetCurrentDirectory();
	}

	public void EnsureModsDirectory()
	{
		Directory.CreateDirectory(_modsDirectory);
	}

	public bool RecoverPendingStorage(out string diagnostic)
	{
		diagnostic = "";
		using (XWModInstallTransaction.Enter())
		{
			using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
			if (environmentMutation == null)
			{
				diagnostic = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。";
				return false;
			}
			if (ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(Path.GetDirectoryName(mod.LoadedPackagePath), _modsDirectory, StringComparison.OrdinalIgnoreCase)))
			{
				diagnostic = "当前安装目录仍有运行中的 Mod，不能执行冷启动恢复。";
				return false;
			}
			EnsureModsDirectory();
			if (!XWModInstallTransaction.TryRecover(this, out diagnostic))
			{
				return false;
			}
			if (!XWModInstallTransaction.TryLoadPendingDeletes(_modsDirectory, out var result, out var diagnostic2))
			{
				diagnostic = diagnostic2;
				return false;
			}
			XWModInstallTransaction.PendingDelete[] array = result.ToArray();
			foreach (XWModInstallTransaction.PendingDelete pending in array)
			{
				if (ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(mod.LoadedId, pending.ModId, StringComparison.OrdinalIgnoreCase)))
				{
					diagnostic = "待删除 Mod 仍在运行，延后处理：" + pending.ModId + "。";
					continue;
				}
				try
				{
					RestoreEnabledState(pending.ModId, enabled: false);
				}
				catch (Exception ex)
				{
					diagnostic = "无法更新待删除 Mod 启用状态：" + ex.GetBaseException().Message;
					continue;
				}
				if (!File.Exists(pending.PackagePath))
				{
					XWModInstallTransaction.RemovePendingDelete(_modsDirectory, pending.ModId);
					continue;
				}
				if (!XWModInstallTransaction.TryHash(pending.PackagePath, out var sha) || !string.Equals(sha, pending.PackageSha256, StringComparison.OrdinalIgnoreCase))
				{
					diagnostic = "待删除 Mod 文件已被替换，拒绝删除：" + pending.ModId + "。";
					continue;
				}
				if (!ModLoader.TryReadPackageMetadata(pending.PackagePath, out var manifest, out var _, out var _) || !string.Equals(manifest?.Id, pending.ModId, StringComparison.OrdinalIgnoreCase))
				{
					diagnostic = "待删除文件的 Mod ID 不匹配，保留安装包：" + pending.ModId + "。";
					continue;
				}
				try
				{
					File.Delete(pending.PackagePath);
					XWModInstallTransaction.RemovePendingDelete(_modsDirectory, pending.ModId);
				}
				catch (Exception ex2)
				{
					diagnostic = ex2.GetBaseException().Message;
				}
			}
			return string.IsNullOrWhiteSpace(diagnostic);
		}
	}

	internal void RestoreEnabledState(string modId, bool enabled)
	{
		if (!TryLoadEnabledIds(out var ids, out var diagnostic))
		{
			throw new InvalidDataException("启用列表损坏，拒绝覆盖：" + diagnostic);
		}
		List<string> list = ids.ToList();
		list.RemoveAll((string id) => string.Equals(id, modId, StringComparison.OrdinalIgnoreCase));
		if (enabled)
		{
			list.Add(modId);
		}
		SaveEnabledIds(list);
	}

	public List<ModEntry> ScanMods(bool includeHashes = false)
	{
		EnsureModsDirectory();
		HashSet<string> hashSet = new HashSet<string>(LoadEnabledIds(), StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet2 = new HashSet<string>(from item in XWModInstallTransaction.LoadPendingDeletes(_modsDirectory)
			select item.ModId, StringComparer.OrdinalIgnoreCase);
		List<ModEntry> list = new List<ModEntry>();
		foreach (string item in Directory.EnumerateFiles(_modsDirectory, "*.pmod", SearchOption.TopDirectoryOnly).OrderBy<string, string>((string path) => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
		{
			XWModManifest manifest;
			ModExporter.ModInfo info;
			string diagnostic;
			bool hasManifest = ModLoader.TryReadPackageMetadata(item, out manifest, out info, out diagnostic);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
			string id = (string.IsNullOrWhiteSpace(manifest?.Id) ? fileNameWithoutExtension : manifest.Id.Trim());
			list.Add(new ModEntry
			{
				Id = id,
				DisplayName = (string.IsNullOrWhiteSpace(manifest?.Name) ? id : manifest.Name),
				Version = (manifest?.Version ?? ""),
				PackagePath = item,
				Enabled = hashSet.Contains(id),
				HasManifest = hasManifest,
				Manifest = manifest,
				Diagnostic = diagnostic,
				PendingDelete = hashSet2.Contains(id),
				Loaded = ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(mod.LoadedId, id, StringComparison.OrdinalIgnoreCase))
			});
		}
		foreach (ModEntry entry in list)
		{
			if (!entry.HasManifest && string.IsNullOrWhiteSpace(entry.PackagePath))
			{
				entry.EffectiveState = "缺少安装包";
				continue;
			}
			if (includeHashes && XWModInstallTransaction.TryHash(entry.PackagePath, out var sha))
			{
				entry.PackageSha256 = sha;
			}
			ModLoader.LoadedMod loadedMod = ModLoader.GetLoadedMods().FirstOrDefault((ModLoader.LoadedMod mod) => string.Equals(mod.LoadedId, entry.Id, StringComparison.OrdinalIgnoreCase) && SameDirectory(mod.LoadedPackagePath, _modsDirectory));
			entry.Loaded = loadedMod?.Applied ?? false;
			if (entry.PendingDelete)
			{
				entry.EffectiveState = "待删除";
			}
			else if (loadedMod != null && loadedMod.State == ModLoader.ApplicationState.PendingRollback)
			{
				entry.EffectiveState = "回滚受阻";
			}
			else if (loadedMod != null && loadedMod.State == ModLoader.ApplicationState.Failed)
			{
				entry.EffectiveState = "加载失败";
			}
			else if (loadedMod != null && loadedMod.Applied)
			{
				entry.EffectiveState = ((string.IsNullOrWhiteSpace(entry.PackageSha256) || string.Equals(entry.PackageSha256, loadedMod.PackageSha256, StringComparison.OrdinalIgnoreCase)) ? "已加载" : "已加载旧版本");
			}
			else
			{
				entry.EffectiveState = (entry.Enabled ? "待重启" : "未启用");
			}
			entry.InstalledVersion = entry.Version;
			if (loadedMod != null)
			{
				entry.RunningVersion = loadedMod.LoadedVersion;
				entry.RunningSha256 = loadedMod.PackageSha256;
				ModEntry modEntry = entry;
				modEntry.EffectiveMode = loadedMod.EffectiveMode.ToString() switch
				{
					"ResourceOnly" => "仅资源", 
					"Managed" => "托管代码与资源", 
					"OptionalResourceFallback" => "可选代码失败，已回退为资源模式", 
					_ => loadedMod.EffectiveMode.ToString(), 
				};
			}
			lock (ApplyFailureGate)
			{
				if (RecentApplyFailures.TryGetValue(FailureDomain, out var value) && value.TryGetValue(entry.Id, out var value2))
				{
					entry.LastApplyFailure = value2;
				}
			}
			if (entry.Enabled && !string.IsNullOrWhiteSpace(entry.LastApplyFailure) && !entry.PendingDelete && entry.EffectiveState != "回滚受阻")
			{
				entry.EffectiveState = "加载失败";
			}
		}
		return list.OrderBy<ModEntry, string>((ModEntry modEntry2) => modEntry2.Id, StringComparer.OrdinalIgnoreCase).ThenBy<ModEntry, string>((ModEntry modEntry2) => modEntry2.PackagePath, StringComparer.OrdinalIgnoreCase).ToList();
	}

	public List<ModEntry> GetManagementSnapshot(bool includeHashes = true)
	{
		List<ModEntry> list = ScanMods(includeHashes);
		HashSet<string> present = list.Select((ModEntry entry) => entry.Id).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in from id in LoadEnabledIds()
			where !present.Contains(id)
			select id)
		{
			list.Add(new ModEntry
			{
				Id = item,
				DisplayName = item,
				Enabled = true,
				HasManifest = false,
				Diagnostic = "启用列表中缺少安装包。",
				EffectiveState = "缺少安装包"
			});
		}
		return list.OrderBy<ModEntry, string>((ModEntry entry) => entry.Id, StringComparer.OrdinalIgnoreCase).ToList();
	}

	public IReadOnlyList<string> LoadEnabledIds()
	{
		if (!File.Exists(EnabledStatePath))
		{
			return Array.Empty<string>();
		}
		try
		{
			return (from id in JsonSerializer.Deserialize(File.ReadAllText(EnabledStatePath), XWModJsonContext.Default.ListString) ?? new List<string>()
				where !string.IsNullOrWhiteSpace(id)
				select id.Trim()).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch (Exception ex)
		{
			GD.PushWarning("[ModManager] enabled state ignored: " + ex.GetBaseException().Message);
			return Array.Empty<string>();
		}
	}

	public bool TryLoadEnabledIds(out IReadOnlyList<string> ids, out string diagnostic)
	{
		ids = Array.Empty<string>();
		diagnostic = "";
		if (!File.Exists(EnabledStatePath))
		{
			return true;
		}
		try
		{
			List<string> list = JsonSerializer.Deserialize(File.ReadAllText(EnabledStatePath), XWModJsonContext.Default.ListString);
			if (list == null || list.Any(string.IsNullOrWhiteSpace))
			{
				throw new InvalidDataException("启用列表必须是非空 ID 字符串组成的数组。");
			}
			ids = list.Select((string id) => id.Trim()).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			return true;
		}
		catch (Exception ex)
		{
			diagnostic = ex.GetBaseException().Message;
			return false;
		}
	}

	public void SaveEnabledIds(IEnumerable<string> modIds)
	{
		using (XWModInstallTransaction.Enter())
		{
			using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
			if (environmentMutation == null)
			{
				throw new InvalidOperationException("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			}
			EnsureModsDirectory();
			List<string> list = (from id in modIds ?? Array.Empty<string>()
				where !string.IsNullOrWhiteSpace(id)
				select id.Trim()).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase).ToList();
			string value = JsonSerializer.Serialize(list, XWModJsonContext.Default.ListString);
			string text = EnabledStatePath + ".tmp";
			try
			{
				using (FileStream fileStream = new FileStream(text, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
				{
					using StreamWriter streamWriter = new StreamWriter(fileStream);
					streamWriter.Write(value);
					streamWriter.Flush();
					fileStream.Flush(flushToDisk: true);
				}
				File.Move(text, EnabledStatePath, overwrite: true);
				XWModEnvironmentService.EnabledChanged(_modsDirectory, list);
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}
	}

	public bool SetEnabled(string modId, bool enabled)
	{
		return TrySetEnabled(modId, enabled).Changed;
	}

	public SetEnabledResult TrySetEnabled(string modId, bool enabled)
	{
		using (XWModInstallTransaction.Enter())
		{
			using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
			if (environmentMutation == null)
			{
				return new SetEnabledResult
				{
					Success = false,
					Blocked = true,
					Issues = new List<XWValidationIssue>
					{
						new XWValidationIssue
						{
							Code = XWValidationIssue.IssueCode.RuntimeLoadFailed,
							Message = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。",
							ResourceKey = modId
						}
					}
				};
			}
			if (string.IsNullOrWhiteSpace(modId))
			{
				return new SetEnabledResult
				{
					Success = false,
					Blocked = true
				};
			}
			if (enabled)
			{
				if (!XWModInstallTransaction.TryLoadPendingDeletes(_modsDirectory, out var result, out var diagnostic))
				{
					return new SetEnabledResult
					{
						Success = false,
						Blocked = true,
						Issues = new List<XWValidationIssue>
						{
							new XWValidationIssue
							{
								Code = XWValidationIssue.IssueCode.RuntimeLoadFailed,
								Message = "待删除 Mod 记录损坏，不能启用：" + diagnostic,
								ResourceKey = modId
							}
						}
					};
				}
				if (result.Any((XWModInstallTransaction.PendingDelete item) => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase)))
				{
					return new SetEnabledResult
					{
						Success = false,
						Blocked = true,
						Issues = new List<XWValidationIssue>
						{
							new XWValidationIssue
							{
								Code = XWValidationIssue.IssueCode.RuntimeLoadFailed,
								Message = "Mod 正在等待删除，不能重新启用：" + modId + "。",
								ResourceKey = modId
							}
						}
					};
				}
			}
			if (!TryLoadEnabledIds(out var ids, out var diagnostic2))
			{
				return new SetEnabledResult
				{
					Success = false,
					Blocked = true,
					Issues = new List<XWValidationIssue>
					{
						new XWValidationIssue
						{
							Code = XWValidationIssue.IssueCode.RuntimeLoadFailed,
							Message = "启用列表损坏，拒绝覆盖：" + diagnostic2,
							ResourceKey = modId
						}
					}
				};
			}
			List<string> list = ids.ToList();
			bool flag;
			if (enabled)
			{
				flag = !list.Contains<string>(modId, StringComparer.OrdinalIgnoreCase);
				if (flag)
				{
					list.Add(modId.Trim());
				}
			}
			else
			{
				flag = list.RemoveAll((string id) => string.Equals(id, modId, StringComparison.OrdinalIgnoreCase)) > 0;
			}
			List<string> list2 = list.Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase).ToList();
			List<XWValidationIssue> list3 = ValidateEnableSet(list2);
			if (!enabled && flag && HasEnabledDependents(modId, list2))
			{
				return new SetEnabledResult
				{
					Success = false,
					Blocked = true,
					Issues = list3
				};
			}
			if (enabled && list3.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error))
			{
				return new SetEnabledResult
				{
					Success = false,
					Blocked = true,
					Issues = list3
				};
			}
			if (!OperatingSystem.IsAndroid() && !enabled && ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(mod.Manifest?.Id ?? mod.Info?.Name, modId, StringComparison.OrdinalIgnoreCase)) && !TryUnloadMod(modId))
			{
				list3.Add(new XWValidationIssue
				{
					Code = XWValidationIssue.IssueCode.RuntimeLoadFailed,
					Message = "RuntimeLoadFailed: " + modId + " is still in use and could not be disabled.",
					ResourceKey = modId
				});
				return new SetEnabledResult
				{
					Success = false,
					Blocked = true,
					Issues = list3
				};
			}
			if (flag)
			{
				SaveEnabledIds(list2);
			}
			return new SetEnabledResult
			{
				Success = true,
				Changed = flag,
				Issues = list3
			};
		}
	}

	public bool Enable(string modId)
	{
		return SetEnabled(modId, enabled: true);
	}

	public bool Disable(string modId)
	{
		return SetEnabled(modId, enabled: false);
	}

	public DeleteResult TryDeleteMod(string modId, string expectedSha256 = "")
	{
		using (XWModInstallTransaction.Enter())
		{
			using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
			if (environmentMutation == null)
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。"
				};
			}
			if (File.Exists(XWModInstallTransaction.JournalPath(_modsDirectory)))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "已有未完成的 Mod 事务，请先重启游戏完成恢复。"
				};
			}
			if (string.IsNullOrWhiteSpace(modId))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "未指定 Mod。"
				};
			}
			ModEntry modEntry = ScanMods().FirstOrDefault((ModEntry item) => string.Equals(item.Id, modId, StringComparison.OrdinalIgnoreCase));
			if (modEntry == null)
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "未找到 Mod：" + modId + "。"
				};
			}
			if (!XWModInstallTransaction.TryHash(modEntry.PackagePath, out var sha))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "无法读取 Mod 文件。"
				};
			}
			if (!string.IsNullOrWhiteSpace(expectedSha256) && !string.Equals(sha, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "Mod 文件已变化，拒绝删除。"
				};
			}
			if (!TryLoadEnabledIds(out var ids, out var diagnostic))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "启用列表损坏，拒绝删除：" + diagnostic
				};
			}
			if (!XWModInstallTransaction.TryLoadPendingDeletes(_modsDirectory, out var _, out var diagnostic2))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "待删除 Mod 记录损坏，拒绝删除：" + diagnostic2
				};
			}
			if (HasEnabledDependents(modId, ids))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "删除会破坏其他 Mod 的依赖关系。"
				};
			}
			bool flag = ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(mod.LoadedId, modId, StringComparison.OrdinalIgnoreCase));
			bool wasEnabled = ids.Contains<string>(modId, StringComparer.OrdinalIgnoreCase);
			if (OperatingSystem.IsAndroid() && flag)
			{
				XWModInstallTransaction.AddPendingDelete(_modsDirectory, new XWModInstallTransaction.PendingDelete
				{
					ModId = modEntry.Id,
					PackagePath = modEntry.PackagePath,
					PackageSha256 = sha
				});
				RestoreEnabledState(modId, enabled: false);
				return new DeleteResult
				{
					Success = true,
					PendingRestart = true,
					Message = modEntry.DisplayName + " 已标记为删除，将在下次启动前移除。"
				};
			}
			if (flag && !TryUnloadMod(modId))
			{
				return new DeleteResult
				{
					Blocked = true,
					Message = "Mod 当前仍被运行时使用，无法删除。"
				};
			}
			XWModInstallTransaction.Journal journal = new XWModInstallTransaction.Journal
			{
				ModId = modEntry.Id,
				TargetPath = modEntry.PackagePath,
				PreviousSha256 = sha,
				WasEnabled = wasEnabled,
				BackupPath = modEntry.PackagePath + $".delete-backup-{Guid.NewGuid():N}"
			};
			bool flag2 = false;
			try
			{
				XWModInstallTransaction.BeginDelete(_modsDirectory, journal);
				File.Move(modEntry.PackagePath, journal.BackupPath);
				XWModInstallTransaction.SetPhase(_modsDirectory, journal, "package-moved");
				RestoreEnabledState(modId, enabled: false);
				XWModInstallTransaction.SetPhase(_modsDirectory, journal, "committed");
				flag2 = true;
				XWModEnvironmentService.PackageInstalled(_modsDirectory, modId);
				File.Delete(journal.BackupPath);
				XWModInstallTransaction.Complete(_modsDirectory);
				return new DeleteResult
				{
					Success = true,
					Message = "已删除 Mod：" + modEntry.DisplayName + "。Mod 进度已保留。"
				};
			}
			catch (Exception ex)
			{
				if (flag2)
				{
					return new DeleteResult
					{
						Success = true,
						Message = "安装包已移除，旧备份将在下次启动时继续清理。Mod 进度已保留。"
					};
				}
				if (!XWModInstallTransaction.TryRecover(this, out var diagnostic3))
				{
					return new DeleteResult
					{
						Blocked = true,
						Message = "删除失败，恢复仍受阻：" + diagnostic3
					};
				}
				return new DeleteResult
				{
					Blocked = true,
					Message = "删除失败：" + ex.GetBaseException().Message
				};
			}
		}
	}

	public bool LoadMod(string modId)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return false;
		}
		ModEntry modEntry = ScanMods().FirstOrDefault((ModEntry item) => string.Equals(item.Id, modId, StringComparison.OrdinalIgnoreCase));
		ModLoader.LoadedMod loadedMod = ((modEntry?.Manifest == null) ? null : ModLoader.LoadMod(modEntry.PackagePath));
		if (loadedMod == null)
		{
			return false;
		}
		if (!ModLoader.ApplyMod(loadedMod))
		{
			if (ModLoader.IsTracked(loadedMod))
			{
				_loadedManifests[modEntry.Id] = modEntry.Manifest;
			}
			return false;
		}
		if (!loadedMod.NotifyAllModsLoaded())
		{
			if (!ModLoader.RollbackMod(modEntry.Id, out var blockers))
			{
				_loadedManifests[modEntry.Id] = modEntry.Manifest;
				GD.PushWarning("[ModManager] lifecycle rollback remains tracked: " + XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(modEntry.Id, blockers));
			}
			return false;
		}
		_loadedManifests[modEntry.Id] = modEntry.Manifest;
		return true;
	}

	public void LoadEnabledMods()
	{
		LoadEnabledModsWithResult();
	}

	public int LoadEnabledModsWithResult(bool recoverStorage = false)
	{
		return LoadEnabledModsDetailed(recoverStorage).LegacyCount;
	}

	public XWModStartupReport LoadEnabledModsDetailed(bool recoverStorage = false)
	{
		int legacyCount = LoadEnabledModsCore(recoverStorage);
		IReadOnlyList<string> configured;
		string diagnostic;
		bool flag = TryLoadEnabledIds(out configured, out diagnostic);
		Dictionary<string, string> failures = (from item in GetRecentApplyFailures()
			where configured.Contains<string>(item.ModId, StringComparer.OrdinalIgnoreCase)
			select item).ToDictionary<ApplyFailureRecord, string, string>((ApplyFailureRecord item) => item.ModId, (ApplyFailureRecord item) => item.Message, StringComparer.OrdinalIgnoreCase);
		string[] array = (from mod in ModLoader.GetLoadedMods()
			where mod.Applied && SameDirectory(mod.LoadedPackagePath, _modsDirectory) && !failures.ContainsKey(mod.LoadedId)
			select mod.LoadedId).ToArray();
		foreach (string item in configured)
		{
			if (!array.Contains<string>(item, StringComparer.OrdinalIgnoreCase) && !failures.ContainsKey(item))
			{
				failures[item] = "安装包或依赖不可用，请查看诊断。";
			}
		}
		XWModEnvironmentStatus status = XWModEnvironmentService.GetStatus();
		return new XWModStartupReport
		{
			LegacyCount = legacyCount,
			ConfiguredIds = configured,
			LoadedIds = array,
			FailedById = failures,
			Environment = status,
			BlockingReason = (flag ? status.Reason : diagnostic)
		};
	}

	private int LoadEnabledModsCore(bool recoverStorage)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return -1;
		}
		if (recoverStorage && !RecoverPendingStorage(out var diagnostic))
		{
			GD.PushWarning("[ModManager] storage recovery: " + diagnostic);
			XWModEnvironmentService.Invalidate("StorageRecoveryBlocked");
			return -1;
		}
		if (!TryLoadEnabledIds(out var ids, out var diagnostic2))
		{
			XWModEnvironmentService.Invalidate("InvalidEnabledState");
			GD.PushWarning("[ModManager] invalid enabled state: " + diagnostic2);
			return -1;
		}
		XWModEnvironmentService.EnabledChanged(_modsDirectory, ids);
		if (!ModLoader.TryUnloadAll())
		{
			GD.PushWarning("[ModManager] reload blocked because an active Mod is still in use");
			return -1;
		}
		_loadedManifests.Clear();
		XWModEnvironmentService.Invalidate("ApplyingEnabledSet");
		ResolveEnabledResult resolveEnabledResult = ResolveEnabledMods(ids);
		foreach (XWValidationIssue issue in resolveEnabledResult.Issues)
		{
			if (!string.IsNullOrWhiteSpace(issue.ResourceKey))
			{
				RecordApplyFailure(issue.ResourceKey, issue.Message);
			}
			GD.PushWarning("[ModManager] " + issue.Message);
		}
		Dictionary<string, ModLoader.LoadedMod> loadedById = new Dictionary<string, ModLoader.LoadedMod>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool flag = false;
		foreach (ModEntry item in resolveEnabledResult.LoadOrder)
		{
			if (item.Manifest.Dependencies.Any((XWModDependency dependency) => failed.Contains(dependency.Id)))
			{
				failed.Add(item.Id);
				RecordApplyFailure(item.Id, "依赖 Mod 应用失败，当前包未加载。");
				GD.PushWarning("[ModManager] skipped " + item.Id + ": a dependency failed to load");
				continue;
			}
			ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(item.PackagePath);
			if (loadedMod == null || !ModLoader.ApplyMod(loadedMod))
			{
				failed.Add(item.Id);
				RecordApplyFailure(item.Id, (loadedMod == null) ? "Mod 包加载失败。" : string.Join("；", loadedMod.Diagnostics.DefaultIfEmpty("Mod 应用失败。")));
				if (loadedMod != null)
				{
					if (ModLoader.IsTracked(loadedMod))
					{
						flag = true;
						_loadedManifests[item.Id] = item.Manifest;
					}
					foreach (string diagnostic3 in loadedMod.Diagnostics)
					{
						GD.PushWarning("[ModManager] " + item.Id + ": " + diagnostic3);
					}
				}
				GD.PushWarning("[ModManager] failed to apply enabled package: " + item.Id);
			}
			else
			{
				loadedById[item.Id] = loadedMod;
				_loadedManifests[item.Id] = item.Manifest;
				ClearApplyFailure(item.Id);
			}
		}
		foreach (ModEntry item2 in resolveEnabledResult.LoadOrder)
		{
			if (loadedById.TryGetValue(item2.Id, out var value) && (item2.Manifest.Dependencies.Any((XWModDependency dependency) => failed.Contains(dependency.Id)) || !value.NotifyAllModsLoaded()))
			{
				failed.Add(item2.Id);
				RecordApplyFailure(item2.Id, "Mod 运行入口初始化失败，已回滚。");
				if (ModLoader.RollbackMod(item2.Id, out var blockers))
				{
					loadedById.Remove(item2.Id);
					_loadedManifests.Remove(item2.Id);
				}
				else
				{
					flag = true;
					GD.PushWarning("[ModManager] lifecycle rollback remains tracked: " + XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(item2.Id, blockers));
				}
				GD.PushWarning("[ModManager] runtime lifecycle failed: " + item2.Id);
			}
		}
		GD.Print($"[ModManager] enabled runtime apply complete: {_loadedManifests.Count}/{resolveEnabledResult.LoadOrder.Count} packages; rollbackBlocked={flag}");
		if (!flag && failed.Count == 0 && resolveEnabledResult.LoadOrder.Count == ids.Count && !resolveEnabledResult.Issues.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error))
		{
			XWModEnvironmentService.CompleteFormalApply(this, ids, resolveEnabledResult.LoadOrder.Select((ModEntry entry) => loadedById[entry.Id]).ToArray());
			XWModEnvironmentService.CaptureAsync();
		}
		return flag ? (-1) : _loadedManifests.Count;
	}

	public void UnloadMod(string modId)
	{
		TryUnloadMod(modId);
	}

	public bool TryUnloadMod(string modId)
	{
		if (string.IsNullOrWhiteSpace(modId))
		{
			return false;
		}
		if (!ModLoader.UnloadMod(modId))
		{
			return false;
		}
		_loadedManifests.Remove(modId);
		return true;
	}

	public void UnloadAll()
	{
		TryUnloadAll();
	}

	public bool TryUnloadAll()
	{
		if (!ModLoader.TryUnloadAll())
		{
			return false;
		}
		_loadedManifests.Clear();
		return true;
	}

	public List<XWValidationIssue> ValidateEnableSet(IEnumerable<string> enabledIds)
	{
		return ResolveEnabledMods(enabledIds).Issues;
	}

	internal bool HasEnabledDependents(string modId, IEnumerable<string> enabledIds)
	{
		HashSet<string> enabled = new HashSet<string>(enabledIds, StringComparer.OrdinalIgnoreCase);
		return ScanMods().Any((ModEntry entry) => enabled.Contains(entry.Id) && !string.Equals(entry.Id, modId, StringComparison.OrdinalIgnoreCase) && (entry.Manifest?.Dependencies.Any((XWModDependency dependency) => string.Equals(dependency?.Id, modId, StringComparison.OrdinalIgnoreCase)) ?? false));
	}

	internal List<XWValidationIssue> ValidateReplacement(IEnumerable<string> enabledIds, XWModManifest replacement)
	{
		List<ModEntry> list = ScanMods();
		list.RemoveAll((ModEntry entry) => string.Equals(entry.Id, replacement.Id, StringComparison.OrdinalIgnoreCase));
		list.Add(new ModEntry
		{
			Id = replacement.Id,
			Manifest = replacement,
			HasManifest = true
		});
		return ResolveEnabledMods(enabledIds, list).Issues;
	}

	public ResolveEnabledResult ResolveEnabledMods(IEnumerable<string> enabledIds)
	{
		return ResolveEnabledMods(enabledIds, ScanMods());
	}

	private ResolveEnabledResult ResolveEnabledMods(IEnumerable<string> enabledIds, List<ModEntry> entries)
	{
		ResolveEnabledResult resolveEnabledResult = new ResolveEnabledResult();
		HashSet<string> enabled = new HashSet<string>(from id in enabledIds ?? Array.Empty<string>()
			where !string.IsNullOrWhiteSpace(id)
			select id.Trim(), StringComparer.OrdinalIgnoreCase);
		if (!XWModInstallTransaction.TryLoadPendingDeletes(_modsDirectory, out var result, out var diagnostic))
		{
			AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.RuntimeLoadFailed, "待删除 Mod 记录损坏，拒绝加载：" + diagnostic, "");
			return resolveEnabledResult;
		}
		foreach (XWModInstallTransaction.PendingDelete item in result)
		{
			if (enabled.Contains(item.ModId))
			{
				enabled.Remove(item.ModId);
				AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.RuntimeLoadFailed, "Mod 正在等待删除，不能启用：" + item.ModId + "。", item.ModId);
			}
		}
		HashSet<string> invalid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, ModEntry> manifests = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (IGrouping<string, ModEntry> item2 in entries.GroupBy<ModEntry, string>((ModEntry entry) => entry.Id, StringComparer.OrdinalIgnoreCase))
		{
			ModEntry[] array = item2.ToArray();
			if (array.Length > 1)
			{
				if (enabled.Contains(item2.Key))
				{
					invalid.Add(item2.Key);
					AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.ManifestError, "ManifestError: duplicate installed mod id '" + item2.Key + "'.", item2.Key);
				}
			}
			else
			{
				manifests[item2.Key] = array[0];
			}
		}
		foreach (string item3 in enabled.OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase))
		{
			if (!manifests.TryGetValue(item3, out var value) || value.Manifest == null)
			{
				invalid.Add(item3);
				AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.ManifestError, "ManifestError: enabled mod '" + item3 + "' is missing or invalid: " + value?.Diagnostic, item3);
				continue;
			}
			foreach (XWModDependency dependency in value.Manifest.Dependencies)
			{
				if (string.IsNullOrWhiteSpace(dependency?.Id) || !enabled.Contains(dependency.Id) || !manifests.TryGetValue(dependency.Id, out var value2) || value2.Manifest == null)
				{
					invalid.Add(item3);
					AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.DependencyConflict, $"DependencyConflict: {item3} requires {dependency?.Id}.", item3);
				}
				else if (!VersionMatches(dependency.Version, value2.Manifest.Version))
				{
					invalid.Add(item3);
					AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.VersionMismatch, $"VersionMismatch: {item3} requires {dependency.Id} {dependency.Version}, installed {value2.Manifest.Version}.", item3);
				}
			}
			foreach (XWModDependency conflict in value.Manifest.Conflicts)
			{
				if (!string.IsNullOrWhiteSpace(conflict?.Id) && enabled.Contains(conflict.Id) && manifests.TryGetValue(conflict.Id, out var value3) && value3.Manifest != null && VersionMatches(conflict.Version, value3.Manifest.Version))
				{
					invalid.Add(item3);
					invalid.Add(conflict.Id);
					AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.DependencyConflict, $"DependencyConflict: {item3} conflicts with {conflict.Id}.", item3);
				}
			}
		}
		foreach (IGrouping<string, string> item4 in from @group in manifests.Where((KeyValuePair<string, ModEntry> pair) => enabled.Contains(pair.Key) && pair.Value.Manifest != null).SelectMany((KeyValuePair<string, ModEntry> pair) => pair.Value.Manifest.Overrides.SelectMany((KeyValuePair<string, List<string>> category) => (category.Value ?? new List<string>()).Select((string key) => new
			{
				Owner = pair.Key,
				Target = category.Key + "/" + key
			}))).GroupBy(item => item.Target, item => item.Owner, StringComparer.OrdinalIgnoreCase)
			where @group.Distinct<string>(StringComparer.OrdinalIgnoreCase).Count() > 1
			select @group)
		{
			foreach (string item5 in item4)
			{
				invalid.Add(item5);
			}
			AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.OverrideConflict, "OverrideConflict: " + item4.Key + " is overridden by multiple enabled mods.", item4.Key);
		}
		bool flag;
		do
		{
			flag = false;
			foreach (string item6 in enabled)
			{
				if (!invalid.Contains(item6) && manifests.TryGetValue(item6, out var value4) && value4.Manifest != null && value4.Manifest.Dependencies.Any((XWModDependency dependency) => invalid.Contains(dependency.Id)))
				{
					invalid.Add(item6);
					flag = true;
					AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.DependencyConflict, "DependencyConflict: " + item6 + " depends on an invalid mod.", item6);
				}
			}
		}
		while (flag);
		HashSet<string> valid = enabled.Where((string id) => !invalid.Contains(id) && manifests.ContainsKey(id)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, int> dictionary = valid.ToDictionary<string, string, int>((string id) => id, (string id) => manifests[id].Manifest.Dependencies.Count((XWModDependency dependency) => valid.Contains(dependency.Id)), StringComparer.OrdinalIgnoreCase);
		Dictionary<string, List<string>> dictionary2 = valid.ToDictionary<string, string, List<string>>((string id) => id, (string _) => new List<string>(), StringComparer.OrdinalIgnoreCase);
		foreach (string item7 in valid)
		{
			foreach (XWModDependency dependency2 in manifests[item7].Manifest.Dependencies)
			{
				if (valid.Contains(dependency2.Id))
				{
					dictionary2[dependency2.Id].Add(item7);
				}
			}
		}
		SortedSet<string> sortedSet = new SortedSet<string>(from pair in dictionary
			where pair.Value == 0
			select pair.Key, StringComparer.OrdinalIgnoreCase);
		while (sortedSet.Count > 0)
		{
			string min = sortedSet.Min;
			sortedSet.Remove(min);
			resolveEnabledResult.LoadOrder.Add(manifests[min]);
			foreach (string item8 in dictionary2[min].OrderBy<string, string>((string id) => id, StringComparer.OrdinalIgnoreCase))
			{
				dictionary[item8]--;
				if (dictionary[item8] == 0)
				{
					sortedSet.Add(item8);
				}
			}
		}
		foreach (string item9 in valid.Except<string>(resolveEnabledResult.LoadOrder.Select((ModEntry entry) => entry.Id), StringComparer.OrdinalIgnoreCase))
		{
			AddIssue(resolveEnabledResult, XWValidationIssue.IssueCode.DependencyCycle, "DependencyCycle: " + item9 + " is part of, or depends on, a dependency cycle.", item9);
		}
		return resolveEnabledResult;
	}

	private static bool VersionMatches(string required, string actual)
	{
		if (!string.IsNullOrWhiteSpace(required))
		{
			return string.Equals(required.Trim(), actual?.Trim(), StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool SameDirectory(string path, string directory)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
		{
			return false;
		}
		return string.Equals((Path.GetDirectoryName(Path.GetFullPath(path)) ?? "").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
	}

	private static void AddIssue(ResolveEnabledResult result, XWValidationIssue.IssueCode code, string message, string resourceKey)
	{
		if (!result.Issues.Any((XWValidationIssue issue) => issue.Code == code && string.Equals(issue.Message, message, StringComparison.Ordinal)))
		{
			result.Issues.Add(new XWValidationIssue
			{
				Code = code,
				Message = message,
				ResourceKey = resourceKey
			});
		}
	}
}
