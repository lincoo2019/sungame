using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using PVZHE.ModEditor.ModSystem.Validation;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModPackageInstaller
{
	public sealed class PreparedImport : IDisposable
	{
		internal bool Committed;

		internal bool Consumed;

		internal string ExistingPackagePath = "";

		internal string ExistingPackageSha256 = "";

		internal bool ExistingPackageWasEnabled;

		internal bool ExistingStateCaptured;

		internal string AndroidRequestId = "";

		public string StagedPath { get; internal set; } = "";

		public string Sha256 { get; internal set; } = "";

		public XWModManifest Manifest { get; internal set; }

		public void Dispose()
		{
			if (!Committed && File.Exists(StagedPath))
			{
				File.Delete(StagedPath);
			}
		}
	}

	public sealed class InstallResult
	{
		public bool Success { get; internal set; }

		public bool Enabled { get; internal set; }

		public bool RestartRequired { get; internal set; }

		public string PackagePath { get; internal set; } = "";

		public string Message { get; internal set; } = "";

		public IReadOnlyList<XWValidationIssue> Issues { get; internal set; } = Array.Empty<XWValidationIssue>();
	}

	private const ulong MaxPackageBytes = 536870912uL;

	private const int CopyBufferBytes = 81920;

	public static bool TryPrepare(string sourcePath, string expectedSha256, string expectedModId, out PreparedImport prepared, out string diagnostic)
	{
		prepared = null;
		diagnostic = "";
		string text = "";
		try
		{
			if (string.IsNullOrWhiteSpace(sourcePath))
			{
				throw new InvalidDataException("未提供 Mod 文件。");
			}
			string text2 = Path.Combine(XWModManager.GetDefaultModsDirectory(), ".incoming");
			Directory.CreateDirectory(text2);
			text = Path.Combine(text2, $"{Guid.NewGuid():N}.pmod.part");
			CopyExternalFile(sourcePath, text);
			string text3 = ComputeSha256(text);
			string text4 = NormalizeSha256(expectedSha256);
			if (!string.IsNullOrEmpty(text4) && !string.Equals(text3, text4, StringComparison.Ordinal))
			{
				throw new InvalidDataException($"Mod SHA256 不匹配，期望 {text4}，实际 {text3}。");
			}
			if (!ModLoader.ValidatePackageArchive(text, out var manifest, out var _, out var diagnostic2))
			{
				throw new InvalidDataException("Mod 包校验失败：" + diagnostic2);
			}
			if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id))
			{
				throw new InvalidDataException("Mod 清单缺少有效 ID。");
			}
			if (!string.IsNullOrWhiteSpace(expectedModId) && !string.Equals(manifest.Id.Trim(), expectedModId.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException($"Mod ID 不匹配，期望 {expectedModId}，实际 {manifest.Id}。");
			}
			XWModManager xWModManager = new XWModManager();
			if (!CanInstallPackage(xWModManager.ModsDirectory, manifest.Id, out var diagnostic3))
			{
				throw new InvalidDataException(diagnostic3);
			}
			XWModManager.ModEntry modEntry = xWModManager.ScanMods().FirstOrDefault((XWModManager.ModEntry entry) => string.Equals(entry.Id, manifest.Id, StringComparison.OrdinalIgnoreCase));
			if (!xWModManager.TryLoadEnabledIds(out var ids, out var diagnostic4))
			{
				throw new InvalidDataException("启用列表损坏：" + diagnostic4);
			}
			bool existingPackageWasEnabled = ids.Contains<string>(manifest.Id, StringComparer.OrdinalIgnoreCase);
			prepared = new PreparedImport
			{
				StagedPath = text,
				Sha256 = text3,
				Manifest = manifest,
				ExistingPackagePath = (modEntry?.PackagePath ?? ""),
				ExistingPackageSha256 = ((modEntry == null || !XWModInstallTransaction.TryHash(modEntry.PackagePath, out var sha)) ? "" : sha),
				ExistingPackageWasEnabled = existingPackageWasEnabled,
				ExistingStateCaptured = true
			};
			return true;
		}
		catch (Exception ex)
		{
			diagnostic = ex.GetBaseException().Message;
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				File.Delete(text);
			}
			return false;
		}
	}

	private static bool CanInstallPackage(string modsDirectory, string modId, out string diagnostic)
	{
		if (!XWModInstallTransaction.TryLoadPendingDeletes(modsDirectory, out var result, out diagnostic))
		{
			diagnostic = "待删除记录损坏，拒绝安装：" + diagnostic;
			return false;
		}
		if (result.Any((XWModInstallTransaction.PendingDelete item) => string.Equals(item.ModId, modId, StringComparison.OrdinalIgnoreCase)))
		{
			diagnostic = "此 Mod 正在等待删除，请重启游戏完成删除后再安装。";
			return false;
		}
		return true;
	}

	public static InstallResult Install(PreparedImport prepared, bool enableAfterInstall)
	{
		using (XWModInstallTransaction.Enter())
		{
			using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
			if (environmentMutation == null)
			{
				return Failed("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			}
			if (prepared?.Manifest == null || prepared.Consumed || !File.Exists(prepared.StagedPath))
			{
				return Failed("待安装的 Mod 包不存在或尚未通过校验。");
			}
			if (File.Exists(XWModInstallTransaction.JournalPath(XWModManager.GetDefaultModsDirectory())))
			{
				return Failed("已有未完成的 Mod 事务，请先重启游戏完成恢复。");
			}
			prepared.Consumed = true;
			if (!XWModInstallTransaction.TryHash(prepared.StagedPath, out var sha) || !string.Equals(sha, prepared.Sha256, StringComparison.OrdinalIgnoreCase))
			{
				return Failed("待安装的 Mod 包在确认后发生变化，已拒绝安装。");
			}
			if (!ModLoader.ValidatePackageArchive(prepared.StagedPath, out var manifest, out var _, out var diagnostic) || manifest == null || !string.Equals(manifest.Id, prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase))
			{
				return Failed("待安装的 Mod 包重新校验失败：" + diagnostic);
			}
			XWModManager xWModManager = new XWModManager();
			if (!CanInstallPackage(xWModManager.ModsDirectory, manifest.Id, out var diagnostic2))
			{
				return Failed(diagnostic2);
			}
			List<XWModManager.ModEntry> list = (from entry in xWModManager.ScanMods()
				where string.Equals(entry.Id, prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase)
				select entry).ToList();
			if (list.Count > 1)
			{
				return Failed("已安装目录中存在多个相同 ID 的 Mod：" + prepared.Manifest.Id + "。");
			}
			xWModManager.EnsureModsDirectory();
			if (!xWModManager.TryLoadEnabledIds(out var ids, out var diagnostic3))
			{
				return Failed("启用列表损坏，拒绝安装：" + diagnostic3);
			}
			bool flag = ids.Contains<string>(prepared.Manifest.Id, StringComparer.OrdinalIgnoreCase);
			string text = ((list.Count == 1) ? list[0].PackagePath : Path.Combine(xWModManager.ModsDirectory, BuildPackageFileName(prepared.Manifest.Id)));
			string text2 = text + $".backup-{Guid.NewGuid():N}";
			bool flag2 = File.Exists(text);
			bool flag3 = enableAfterInstall;
			if (!XWModInstallTransaction.TryHash(text, out var sha2))
			{
				sha2 = "";
			}
			bool flag4 = ((!flag2) ? string.IsNullOrWhiteSpace(prepared.ExistingPackagePath) : string.Equals(prepared.ExistingPackagePath, text, StringComparison.OrdinalIgnoreCase));
			if (prepared.ExistingStateCaptured && (!flag4 || !string.Equals(prepared.ExistingPackageSha256, sha2, StringComparison.OrdinalIgnoreCase) || prepared.ExistingPackageWasEnabled != flag))
			{
				return Failed("确认后的已安装 Mod 状态发生变化，请重新选择并确认安装。");
			}
			bool flag5 = ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => string.Equals(mod.LoadedId, prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase));
			List<string> list2 = ids.Where((string id) => !string.Equals(id, prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase)).ToList();
			if (flag3)
			{
				list2.Add(prepared.Manifest.Id);
			}
			if (flag && !flag3 && xWModManager.HasEnabledDependents(prepared.Manifest.Id, list2))
			{
				return Failed("安装后停用此 Mod 会破坏其他 Mod 的依赖关系，原包保持不变。");
			}
			List<XWValidationIssue> list3 = (flag3 ? xWModManager.ValidateReplacement(list2, manifest) : new List<XWValidationIssue>());
			if (list3.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error))
			{
				return new InstallResult
				{
					Message = "安装后的启用集合存在依赖或冲突，原包保持不变。",
					Issues = list3
				};
			}
			if (flag5 && !OperatingSystem.IsAndroid())
			{
				ModLoader.LoadedMod loadedMod = ModLoader.GetLoadedMods().FirstOrDefault((ModLoader.LoadedMod mod) => !string.Equals(mod.LoadedId, prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase) && (mod.Manifest?.Dependencies.Any((XWModDependency item) => string.Equals(item?.Id, prepared.Manifest.Id, StringComparison.OrdinalIgnoreCase)) ?? false));
				if (loadedMod != null)
				{
					return Failed($"Mod 当前被 {loadedMod.LoadedId} 依赖，无法安全替换：{prepared.Manifest.Id}。");
				}
				if (!xWModManager.TryUnloadMod(prepared.Manifest.Id))
				{
					return Failed("Mod 当前正在运行，无法替换：" + prepared.Manifest.Id + "。");
				}
			}
			XWModInstallTransaction.Journal journal = new XWModInstallTransaction.Journal
			{
				ModId = prepared.Manifest.Id,
				TargetPath = text,
				StagedPath = prepared.StagedPath,
				BackupPath = text2,
				PreviousSha256 = sha2,
				NewSha256 = prepared.Sha256,
				HadPreviousPackage = flag2,
				WasEnabled = flag,
				DesiredEnabled = flag3,
				RequestId = prepared.AndroidRequestId
			};
			bool flag6 = false;
			try
			{
				XWModInstallTransaction.BeginInstall(xWModManager.ModsDirectory, journal);
				if (flag2)
				{
					File.Replace(prepared.StagedPath, text, text2, ignoreMetadataErrors: true);
				}
				else
				{
					File.Move(prepared.StagedPath, text);
				}
				prepared.Committed = true;
				XWModInstallTransaction.SetPhase(xWModManager.ModsDirectory, journal, "package-replaced");
				if (flag3 != flag)
				{
					XWModManager.SetEnabledResult setEnabledResult = xWModManager.TrySetEnabled(prepared.Manifest.Id, flag3);
					if (!setEnabledResult.Success)
					{
						RollBackPackage(text, text2, flag2);
						XWModInstallTransaction.SetPhase(xWModManager.ModsDirectory, journal, "rollback-complete");
						XWModInstallTransaction.Complete(xWModManager.ModsDirectory);
						prepared.Committed = false;
						return new InstallResult
						{
							Success = false,
							Message = "Mod 已通过包校验，但启用集合检查失败，已恢复原版本：" + prepared.Manifest.Id + "。",
							Issues = setEnabledResult.Issues
						};
					}
				}
				if (!xWModManager.TryLoadEnabledIds(out var ids2, out var diagnostic4))
				{
					throw new InvalidDataException("安装后启用列表损坏：" + diagnostic4);
				}
				bool flag7 = ids2.Contains<string>(prepared.Manifest.Id, StringComparer.OrdinalIgnoreCase);
				List<XWValidationIssue> list4 = (flag3 ? xWModManager.ValidateEnableSet(ids2) : new List<XWValidationIssue>());
				if (list4.Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error))
				{
					RollBackPackage(text, text2, flag2);
					xWModManager.RestoreEnabledState(prepared.Manifest.Id, flag);
					XWModInstallTransaction.Complete(xWModManager.ModsDirectory);
					prepared.Committed = false;
					return new InstallResult
					{
						Success = false,
						Message = "新版本启用集合校验失败，已恢复原版本：" + prepared.Manifest.Id + "。",
						Issues = list4
					};
				}
				XWModInstallTransaction.SetPhase(xWModManager.ModsDirectory, journal, "committed");
				flag6 = true;
				XWModEnvironmentService.PackageInstalled(xWModManager.ModsDirectory, prepared.Manifest.Id);
				AndroidModInstallIntent.RecordTransactionOutcome(xWModManager.ModsDirectory, journal, committed: true);
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
				XWModInstallTransaction.Complete(xWModManager.ModsDirectory);
				return new InstallResult
				{
					Success = true,
					Enabled = flag7,
					RestartRequired = OperatingSystem.IsAndroid(),
					PackagePath = text,
					Message = (flag7 ? $"Mod 已安装并启用：{prepared.Manifest.Name} {prepared.Manifest.Version}。" : $"Mod 已安装：{prepared.Manifest.Name} {prepared.Manifest.Version}。")
				};
			}
			catch (Exception ex)
			{
				string diagnostic5;
				if (flag6)
				{
					prepared.Committed = true;
					IReadOnlyList<string> ids3;
					return new InstallResult
					{
						Success = true,
						Enabled = (xWModManager.TryLoadEnabledIds(out ids3, out diagnostic5) && ids3.Contains<string>(prepared.Manifest.Id, StringComparer.OrdinalIgnoreCase)),
						RestartRequired = OperatingSystem.IsAndroid(),
						PackagePath = text,
						Message = "Mod 已安装，但旧备份清理将在下次启动重试：" + prepared.Manifest.Name + "。"
					};
				}
				try
				{
					if (!XWModInstallTransaction.TryRecover(xWModManager, out var diagnostic6))
					{
						throw new IOException("事务回滚失败：" + diagnostic6);
					}
					if (xWModManager.TryLoadEnabledIds(out var ids4, out diagnostic5) && ids4.Contains<string>(prepared.Manifest.Id, StringComparer.OrdinalIgnoreCase) != flag)
					{
						xWModManager.RestoreEnabledState(prepared.Manifest.Id, flag);
					}
					prepared.Committed = false;
					XWModInstallTransaction.Complete(xWModManager.ModsDirectory);
				}
				catch (Exception ex2)
				{
					return Failed("Mod 安装失败：" + ex.GetBaseException().Message + "；回滚也失败：" + ex2.GetBaseException().Message);
				}
				return Failed("Mod 安装失败：" + ex.GetBaseException().Message);
			}
		}
	}

	private static void CopyExternalFile(string sourcePath, string stagedPath)
	{
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(sourcePath, Godot.FileAccess.ModeFlags.Read);
		if (fileAccess == null)
		{
			throw new IOException("无法读取 Mod 文件：" + sourcePath);
		}
		ulong length = fileAccess.GetLength();
		if (length == 0L || length > 536870912)
		{
			throw new InvalidDataException("Mod 文件为空或超过 512 MiB 限制。");
		}
		using Godot.FileAccess fileAccess2 = Godot.FileAccess.Open(stagedPath, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess2 == null)
		{
			throw new IOException("无法创建 Mod 导入临时文件。");
		}
		while (fileAccess.GetPosition() < length)
		{
			int num = (int)Math.Min(81920uL, length - fileAccess.GetPosition());
			byte[] buffer = fileAccess.GetBuffer(num);
			if (buffer.Length != num)
			{
				throw new EndOfStreamException("读取 Mod 文件时提前到达结尾。");
			}
			fileAccess2.StoreBuffer(buffer);
		}
		fileAccess2.Flush();
	}

	private static string ComputeSha256(string path)
	{
		using FileStream source = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(source));
	}

	private static string NormalizeSha256(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		string text = value.Trim().ToUpperInvariant();
		if (text.Length != 64 || text.Any((char character) => !Uri.IsHexDigit(character)))
		{
			throw new InvalidDataException("安装请求中的 SHA256 格式无效。");
		}
		return text;
	}

	private static string BuildPackageFileName(string modId)
	{
		string text = new string((modId ?? "mod").Trim().Select(delegate(char character)
		{
			bool flag = char.IsLetterOrDigit(character);
			if (!flag)
			{
				bool flag2 = ((character == '-' || character == '.' || character == '_') ? true : false);
				flag = flag2;
			}
			return (!flag) ? '_' : character;
		}).ToArray()).Trim('.', ' ');
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "mod";
		}
		if (text.Length > 80)
		{
			text = text.Substring(0, 80);
		}
		string text2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(modId ?? "mod"))).Substring(0, 8);
		return text + "-" + text2 + ".pmod";
	}

	private static void RollBackPackage(string targetPath, string backupPath, bool hadPreviousPackage)
	{
		if (hadPreviousPackage && File.Exists(backupPath))
		{
			File.Replace(backupPath, targetPath, null, ignoreMetadataErrors: true);
		}
		else if (!hadPreviousPackage && File.Exists(targetPath))
		{
			File.Delete(targetPath);
		}
	}

	public static void CleanupInstallArtifacts()
	{
		using (XWModInstallTransaction.Enter())
		{
			XWModManager xWModManager = new XWModManager();
			if (!xWModManager.RecoverPendingStorage(out var diagnostic))
			{
				if (!string.IsNullOrWhiteSpace(diagnostic))
				{
					GD.PushWarning("[ModLoader] 无法恢复 Mod 安装事务，保留事务文件：" + diagnostic);
				}
				return;
			}
			string path = Path.Combine(xWModManager.ModsDirectory, ".incoming");
			try
			{
				if (!Directory.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != FileAttributes.None)
				{
					return;
				}
				foreach (string item in Directory.EnumerateFiles(path, "*.pmod.part", SearchOption.TopDirectoryOnly))
				{
					string fileName = Path.GetFileName(item);
					int length = ".pmod.part".Length;
					if (Guid.TryParseExact(fileName.Substring(0, fileName.Length - length), "N", out var _) && (File.GetAttributes(item) & FileAttributes.ReparsePoint) == 0)
					{
						TryDelete(item);
					}
				}
			}
			catch (Exception ex)
			{
				GD.PushWarning("[ModLoader] 无法清理安装暂存目录：" + ex.GetBaseException().Message);
			}
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception ex)
		{
			GD.PushWarning("[ModLoader] 无法清理安装临时文件 " + path + ": " + ex.Message);
		}
	}

	private static InstallResult Failed(string message)
	{
		return new InstallResult
		{
			Success = false,
			Message = message
		};
	}
}
