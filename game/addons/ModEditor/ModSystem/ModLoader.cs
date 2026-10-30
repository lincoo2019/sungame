using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public static class ModLoader
{
	public enum ApplicationState
	{
		Extracted,
		Applying,
		Applied,
		Failed,
		PendingRollback,
		Unloaded
	}

	public enum EffectiveRuntimeMode
	{
		ResourceOnly,
		Managed,
		OptionalResourceFallback
	}

	public class LoadedMod
	{
		public ModExporter.ModInfo Info;

		public XWModManifest Manifest;

		public string FilePath = "";

		public string ExtractRoot = "";

		public List<string> ExtractedFiles = new List<string>();

		public List<string> RelativeFiles = new List<string>();

		public List<string> Diagnostics = new List<string>();

		public System.Collections.Generic.Dictionary<string, Resource> RuntimeResourceCache = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

		public XWModCharacterCompanionRuntime CharacterCompanionRuntime;

		public string RuntimeAssemblyPath = "";

		public int RejectedFileCount;

		public int AppliedResourceCount;

		public int CompanionBoundResourceCount;

		private WeakReference _unloadedContext;

		public long CachedRuntimeSourceBytes { get; private set; }

		public long CachedRuntimeTexturePixels { get; private set; }

		public long CachedRuntimeDecodedAudioBytes { get; private set; }

		public bool RuntimeEntryInitialized => CharacterCompanionRuntime?.RuntimeEntryInitialized ?? false;

		public int StateMachineCallbackCount => CharacterCompanionRuntime?.StateMachineCallbackCount ?? 0;

		public int CharacterComponentRuntimeCount => CharacterCompanionRuntime?.CharacterComponentRuntimeCount ?? 0;

		public ApplicationState State { get; internal set; }

		public EffectiveRuntimeMode EffectiveMode { get; internal set; }

		public string PackageSha256 { get; private set; } = "";

		public string LoadedId { get; private set; } = "";

		public string LoadedVersion { get; private set; } = "";

		public int LoadedRuntimeApiVersion { get; private set; }

		public string LoadedPackagePath { get; private set; } = "";

		public bool RuntimeAssemblyUnloadRequested { get; private set; }

		public bool RequiresRestart { get; private set; }

		public bool? UnloadedContextCollected
		{
			get
			{
				if (_unloadedContext != null)
				{
					return !_unloadedContext.IsAlive;
				}
				return null;
			}
		}

		public bool Applied => State == ApplicationState.Applied;

		internal bool HasAppliedContent
		{
			get
			{
				if (AppliedResourceCount <= 0 && !RuntimeEntryInitialized && StateMachineCallbackCount <= 0)
				{
					return CharacterComponentRuntimeCount > 0;
				}
				return true;
			}
		}

		internal void RecordIdentity(string hash)
		{
			PackageSha256 = hash;
			LoadedId = Manifest.Id.Trim().ToLowerInvariant();
			LoadedVersion = Manifest.Version ?? "";
			LoadedRuntimeApiVersion = Manifest.RuntimeApiVersion;
			LoadedPackagePath = Path.GetFullPath(FilePath);
		}

		public void ClearRuntimeResourceCache()
		{
			RuntimeResourceCache.Clear();
			CachedRuntimeSourceBytes = 0L;
			CachedRuntimeTexturePixels = 0L;
			CachedRuntimeDecodedAudioBytes = 0L;
		}

		public bool UnloadRuntimeAssembly()
		{
			StateMachineUnloadBlockers blockers;
			return UnloadRuntimeAssembly(out blockers);
		}

		public bool CanUnloadRuntimeAssembly(out StateMachineUnloadBlockers blockers)
		{
			blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
			if (CharacterCompanionRuntime != null)
			{
				return CharacterCompanionRuntime.CanUnload(out blockers);
			}
			return true;
		}

		public bool UnloadRuntimeAssembly(out StateMachineUnloadBlockers blockers)
		{
			blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
			if (CharacterCompanionRuntime != null && !CharacterCompanionRuntime.TryUnload(out blockers))
			{
				return false;
			}
			if (CharacterCompanionRuntime != null)
			{
				RuntimeAssemblyUnloadRequested = CharacterCompanionRuntime.UnloadRequested;
				RequiresRestart = CharacterCompanionRuntime.RequiresRestart;
				_unloadedContext = CharacterCompanionRuntime.LastUnloadContext;
			}
			CharacterCompanionRuntime = null;
			CompanionBoundResourceCount = 0;
			return true;
		}

		public bool NotifyAllModsLoaded()
		{
			if (CharacterCompanionRuntime == null)
			{
				return true;
			}
			if (CharacterCompanionRuntime.NotifyAllModsLoaded(out var diagnostic))
			{
				return true;
			}
			AddDiagnostic(this, "OnAllModsLoaded 失败：" + diagnostic);
			State = ApplicationState.Failed;
			XWModEnvironmentService.Invalidate("LifecycleFailed");
			return false;
		}

		internal void PrepareLifecycleRollback()
		{
			CharacterCompanionRuntime?.PrepareLifecycleRollback();
		}

		internal void AccountCachedRuntimeResource(long sourceBytes, long texturePixels, long decodedAudioBytes)
		{
			CachedRuntimeSourceBytes += sourceBytes;
			CachedRuntimeTexturePixels += texturePixels;
			CachedRuntimeDecodedAudioBytes += decodedAudioBytes;
		}
	}

	private sealed class RuntimeCandidate
	{
		public string RelativePath = "";

		public string ExtractedPath = "";

		public string Category = "";

		public string InferredKey = "";
	}

	private sealed class RuntimeBinding
	{
		public RuntimeCandidate Candidate;

		public string Key = "";

		public bool AllowOverride;

		public bool Required;
	}

	private const long MaxEntryBytes = 67108864L;

	private const long MaxArchiveBytes = 536870912L;

	private const int MaxArchiveEntries = 4096;

	private const int MaxRuntimeCacheEntriesPerMod = 512;

	private const int MaxRuntimeCacheEntriesTotal = 4096;

	private const long MaxRuntimeSourceBytesPerMod = 268435456L;

	private const long MaxRuntimeSourceBytesTotal = 536870912L;

	private const long MaxRuntimeTexturePixelsPerMod = 67108864L;

	private const long MaxRuntimeTexturePixelsTotal = 134217728L;

	private const long MaxRuntimeDecodedAudioBytesPerMod = 134217728L;

	private const long MaxRuntimeDecodedAudioBytesTotal = 268435456L;

	private static readonly List<LoadedMod> LoadedMods = new List<LoadedMod>();

	private static readonly HashSet<string> CompanionOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static int LoadAll()
	{
		return new XWModManager().LoadEnabledModsWithResult(recoverStorage: true);
	}

	public static XWModStartupReport LoadAllDetailed()
	{
		return new XWModManager().LoadEnabledModsDetailed(recoverStorage: true);
	}

	public static bool TryReadPackageMetadata(string pmodPath, out XWModManifest manifest, out ModExporter.ModInfo info, out string diagnostic)
	{
		return ValidatePackageArchive(pmodPath, out manifest, out info, out diagnostic);
	}

	public static bool ValidatePackageArchive(string pmodPath, out XWModManifest manifest, out ModExporter.ModInfo info, out string diagnostic)
	{
		manifest = null;
		info = null;
		diagnostic = "";
		try
		{
			if (string.IsNullOrWhiteSpace(pmodPath) || !File.Exists(pmodPath))
			{
				throw new FileNotFoundException("Mod package was not found.", pmodPath);
			}
			using FileStream stream = File.OpenRead(pmodPath);
			using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			if (zipArchive.Entries.Count > 4096)
			{
				throw new InvalidDataException($"archive entry limit exceeded: {zipArchive.Entries.Count} > {4096}");
			}
			string extractRoot = Path.Combine(Path.GetTempPath(), "PVZHE-PmodValidation");
			long num = 0L;
			List<string> relativeEntries = new List<string>(zipArchive.Entries.Count);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (!ValidateArchiveEntryPath(extractRoot, entry.FullName, out var relativePath, out var _))
				{
					throw new InvalidDataException("unsafe archive entry: " + entry.FullName);
				}
				relativeEntries.Add(relativePath);
				if (!hashSet.Add(relativePath))
				{
					throw new InvalidDataException("duplicate archive entry: " + relativePath);
				}
				if (!string.IsNullOrEmpty(entry.Name))
				{
					if (entry.Length > 67108864 || num + entry.Length > 536870912)
					{
						throw new InvalidDataException("archive size limit exceeded by: " + relativePath);
					}
					num += entry.Length;
				}
			}
			ZipArchiveEntry[] array = zipArchive.Entries.Where((ZipArchiveEntry entry, int index) => string.Equals(relativeEntries[index], "mod.json", StringComparison.OrdinalIgnoreCase)).ToArray();
			if (array.Length != 1)
			{
				throw new InvalidDataException("package must contain exactly one root mod.json");
			}
			if (array[0].Length > 1048576)
			{
				throw new InvalidDataException("mod.json exceeds the 1 MiB limit");
			}
			using Stream stream2 = array[0].Open();
			using StreamReader streamReader = new StreamReader(stream2);
			ParseManifest(streamReader.ReadToEnd(), out manifest, out info);
			NormalizeManifest(manifest, info, pmodPath);
			if (manifest == null)
			{
				manifest = CreateLegacyManifest(info, pmodPath);
			}
			ValidateDeclaredPackageExecutables(manifest, relativeEntries);
			return manifest != null;
		}
		catch (Exception ex)
		{
			diagnostic = ex.GetBaseException().Message;
			manifest = null;
			info = null;
			return false;
		}
	}

	private static void ValidateDeclaredPackageExecutables(XWModManifest manifest, IReadOnlyList<string> relativeEntries)
	{
		string text = (manifest?.RuntimeAssembly ?? "").Replace('\\', '/').Trim('/');
		if (!string.IsNullOrWhiteSpace(text) && !text.Equals("Runtime/ModAssembly.dll", StringComparison.Ordinal))
		{
			throw new InvalidDataException("invalid declared runtime assembly path: " + text);
		}
		bool flag = false;
		foreach (string relativeEntry in relativeEntries)
		{
			if (!string.IsNullOrWhiteSpace(relativeEntry))
			{
				if (string.Equals(relativeEntry, text, StringComparison.Ordinal))
				{
					flag = true;
				}
				if (IsExecutablePackageFile(relativeEntry) && !string.Equals(relativeEntry, text, StringComparison.Ordinal) && !IsRuntimeDependencyFile(relativeEntry))
				{
					throw new InvalidDataException("undeclared executable package file: " + relativeEntry);
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(text) && manifest.IsRuntimeAssemblyRequired() && !flag)
		{
			throw new InvalidDataException("declared runtime assembly is missing: " + text);
		}
	}

	public static LoadedMod LoadMod(string pmodPath)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return null;
		}
		if (!ValidatePackageArchive(pmodPath, out var _, out var _, out var diagnostic))
		{
			GD.PushWarning("[ModLoader] package rejected: " + pmodPath + ": " + diagnostic);
			return null;
		}
		if (string.IsNullOrWhiteSpace(pmodPath) || !File.Exists(pmodPath))
		{
			GD.PrintErr("[ModLoader] package does not exist: " + pmodPath);
			return null;
		}
		string fullPath = Path.GetFullPath(ProjectSettings.GlobalizePath("user://ModsCache/"));
		string extractDir = Path.GetFullPath(Path.Combine(fullPath, SanitizeCacheName(Path.GetFileNameWithoutExtension(pmodPath))));
		if (LoadedMods.Any((LoadedMod active) => string.Equals(active.ExtractRoot, extractDir, StringComparison.OrdinalIgnoreCase)))
		{
			extractDir = extractDir + "-" + Guid.NewGuid().ToString("N");
		}
		string value = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!extractDir.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			GD.PrintErr("[ModLoader] rejected unsafe cache destination");
			return null;
		}
		LoadedMod loadedMod = new LoadedMod
		{
			FilePath = pmodPath,
			ExtractRoot = extractDir
		};
		try
		{
			if (Directory.Exists(extractDir))
			{
				Directory.Delete(extractDir, recursive: true);
			}
			Directory.CreateDirectory(extractDir);
			long num = 0L;
			using FileStream fileStream = File.OpenRead(pmodPath);
			string hash = Convert.ToHexString(SHA256.HashData(fileStream)).ToLowerInvariant();
			fileStream.Position = 0L;
			using ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
			if (zipArchive.Entries.Count > 4096)
			{
				throw new InvalidDataException($"archive entry limit exceeded: {zipArchive.Entries.Count} > {4096}");
			}
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (!ValidateArchiveEntryPath(extractDir, entry.FullName, out var relativePath, out var outputPath))
				{
					throw new InvalidDataException("unsafe archive entry: " + entry.FullName);
				}
				if (string.IsNullOrEmpty(entry.Name))
				{
					Directory.CreateDirectory(outputPath);
					continue;
				}
				if (entry.Length > 67108864 || num + entry.Length > 536870912)
				{
					throw new InvalidDataException("archive size limit exceeded by: " + relativePath);
				}
				num += entry.Length;
				Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? extractDir);
				if (relativePath.Equals("mod.json", StringComparison.OrdinalIgnoreCase))
				{
					using Stream stream = entry.Open();
					using StreamReader streamReader = new StreamReader(stream);
					string text = streamReader.ReadToEnd();
					ParseManifest(text, loadedMod);
					File.WriteAllText(outputPath, text);
				}
				else
				{
					entry.ExtractToFile(outputPath, overwrite: true);
					loadedMod.RelativeFiles.Add(relativePath);
					loadedMod.ExtractedFiles.Add(outputPath);
				}
			}
			if (loadedMod.Manifest == null && loadedMod.Info == null)
			{
				AddDiagnostic(loadedMod, "missing or invalid root mod.json");
				GD.PushWarning("[ModLoader] package rejected: " + pmodPath + ": missing or invalid root mod.json");
				return null;
			}
			NormalizeManifest(loadedMod.Manifest, loadedMod.Info, pmodPath);
			LoadedMod loadedMod2 = loadedMod;
			if (loadedMod2.Manifest == null)
			{
				loadedMod2.Manifest = CreateLegacyManifest(loadedMod.Info, pmodPath);
			}
			if (!ResolveDeclaredRuntimeAssembly(loadedMod))
			{
				if (Directory.Exists(extractDir))
				{
					Directory.Delete(extractDir, recursive: true);
				}
				return null;
			}
			GD.Print($"[ModLoader] package extracted safely: {GetOwnerId(loadedMod)} ({loadedMod.RelativeFiles.Count} files)");
			loadedMod.RecordIdentity(hash);
			XWModEnvironmentService.Invalidate("PackageExtracted");
			return loadedMod;
		}
		catch (Exception ex)
		{
			loadedMod.RejectedFileCount++;
			AddDiagnostic(loadedMod, ex.Message);
			if (Directory.Exists(extractDir))
			{
				Directory.Delete(extractDir, recursive: true);
			}
			GD.PushWarning("[ModLoader] package rejected: " + pmodPath + ": " + ex.Message);
			return null;
		}
	}

	public static bool ApplyMod(LoadedMod mod)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return false;
		}
		if (mod == null || (mod.Manifest == null && mod.Info == null))
		{
			return false;
		}
		string ownerId = GetOwnerId(mod);
		if (!XWModRuntimeCompatibility.ValidatePackage(mod.Manifest, out var diagnostic))
		{
			AddDiagnostic(mod, diagnostic);
			return false;
		}
		if (!TryRemoveTrackedOwner(ownerId, out var _, out var blockers, mod))
		{
			AddDiagnostic(mod, XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(ownerId, blockers));
			return false;
		}
		XWModRuntimeRegistry.UnregisterOwner(ownerId);
		XWModEnvironmentService.Invalidate("Applying");
		mod.State = ApplicationState.Failed;
		mod.AppliedResourceCount = 0;
		mod.CompanionBoundResourceCount = 0;
		if (!mod.UnloadRuntimeAssembly(out var blockers2))
		{
			AddDiagnostic(mod, XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(ownerId, blockers2));
			return false;
		}
		if (!string.IsNullOrWhiteSpace(mod.RuntimeAssemblyPath))
		{
			try
			{
				mod.CharacterCompanionRuntime = new XWModCharacterCompanionRuntime();
				mod.CharacterCompanionRuntime.Load(ownerId, mod.RuntimeAssemblyPath);
			}
			catch (Exception ex)
			{
				AddDiagnostic(mod, "声明的运行程序集加载失败：" + ex.GetBaseException().Message);
				mod.UnloadRuntimeAssembly();
				if (mod.Manifest.IsRuntimeAssemblyRequired())
				{
					return false;
				}
			}
		}
		bool flag = HasExplicitRuntimeEntries(mod.Manifest);
		List<RuntimeCandidate> list = new List<RuntimeCandidate>();
		for (int i = 0; i < mod.RelativeFiles.Count && i < mod.ExtractedFiles.Count; i++)
		{
			string text = mod.RelativeFiles[i];
			string extractedPath = mod.ExtractedFiles[i];
			if (IsExecutablePackageFile(text))
			{
				if (!IsDeclaredRuntimeAssembly(mod, text) && !IsRuntimeDependencyFile(text))
				{
					AddDiagnostic(mod, "blocked executable package file: " + text);
				}
			}
			else
			{
				if (IsDeclaredRuntimeSymbols(mod, text))
				{
					continue;
				}
				if (!InferRuntimeEntry(text, out var category, out var key))
				{
					if (!IsCharacterPackageDependency(text) && !IsAnimationAtlasPackageDependency(text) && !IsLevelPackageDependency(text))
					{
						AddDiagnostic(mod, "unsupported package file: " + text);
					}
				}
				else
				{
					list.Add(new RuntimeCandidate
					{
						RelativePath = text,
						ExtractedPath = extractedPath,
						Category = category,
						InferredKey = key
					});
				}
			}
		}
		List<RuntimeBinding> list2 = new List<RuntimeBinding>();
		bool flag2 = false;
		foreach (RuntimeCandidate candidate in list)
		{
			if (list.Count((RuntimeCandidate runtimeCandidate) => NormalizeCategory(runtimeCandidate.Category).Equals(NormalizeCategory(candidate.Category), StringComparison.OrdinalIgnoreCase) && runtimeCandidate.InferredKey.Equals(candidate.InferredKey, StringComparison.OrdinalIgnoreCase)) != 1)
			{
				AddDiagnostic(mod, $"ambiguous automatic runtime key: {candidate.Category}/{candidate.InferredKey}: {candidate.RelativePath}");
				List<(string, bool)> manifestDeclarations = GetManifestDeclarations(mod.Manifest, candidate.Category);
				flag2 |= manifestDeclarations.Any<(string, bool)>(((string Key, bool Override) tuple) => tuple.Key.Equals(candidate.InferredKey, StringComparison.OrdinalIgnoreCase));
				continue;
			}
			string key2 = candidate.InferredKey;
			bool flag3 = ManifestContains(mod.Manifest?.Provides, candidate.Category, key2);
			bool flag4 = ManifestContains(mod.Manifest?.Overrides, candidate.Category, key2);
			if (flag && !flag3 && !flag4)
			{
				List<(string, bool)> manifestDeclarations2 = GetManifestDeclarations(mod.Manifest, candidate.Category);
				if (list.Count((RuntimeCandidate runtimeCandidate) => NormalizeCategory(runtimeCandidate.Category).Equals(NormalizeCategory(candidate.Category), StringComparison.OrdinalIgnoreCase)) != 1 || manifestDeclarations2.Count != 1)
				{
					AddDiagnostic(mod, "resource is not unambiguously declared by manifest: " + candidate.Category + "/" + candidate.InferredKey);
					continue;
				}
				key2 = manifestDeclarations2[0].Item1;
				flag4 = manifestDeclarations2[0].Item2;
				flag3 = !flag4;
			}
			list2.Add(new RuntimeBinding
			{
				Candidate = candidate,
				Key = key2,
				AllowOverride = flag4,
				Required = (flag3 || flag4)
			});
		}
		HashSet<string> hashSet = (from @group in list2.GroupBy<RuntimeBinding, string>((RuntimeBinding binding) => NormalizeCategory(binding.Candidate.Category) + "\n" + binding.Key, StringComparer.OrdinalIgnoreCase)
			where @group.Count() > 1
			select @group.Key).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (flag2)
		{
			TrackFailedApplyUntilRollback(mod, ownerId);
			return false;
		}
		foreach (RuntimeBinding item2 in list2)
		{
			RuntimeCandidate candidate2 = item2.Candidate;
			string item = NormalizeCategory(candidate2.Category) + "\n" + item2.Key;
			if (hashSet.Contains(item))
			{
				AddDiagnostic(mod, $"ambiguous manifest runtime key: {candidate2.Category}/{item2.Key}: {candidate2.RelativePath}");
				if (item2.Required)
				{
					TrackFailedApplyUntilRollback(mod, ownerId);
					return false;
				}
				continue;
			}
			if (!TryLoadRuntimeResource(mod, candidate2, out var resource))
			{
				if (item2.Required)
				{
					AddDiagnostic(mod, $"required runtime resource failed: {ownerId}: {candidate2.Category}/{item2.Key}: {candidate2.RelativePath}");
					TrackFailedApplyUntilRollback(mod, ownerId);
					return false;
				}
				continue;
			}
			if (candidate2.Category.Equals("Character", StringComparison.OrdinalIgnoreCase) && resource is PackedScene packedScene && CharacterRequiresCompanion(packedScene))
			{
				string diagnostic2 = "伴随脚本运行时不可用";
				Node runtimeRoot = null;
				if (mod.CharacterCompanionRuntime == null || !mod.CharacterCompanionRuntime.TryCreateInstance(packedScene, out runtimeRoot, out diagnostic2))
				{
					AddDiagnostic(mod, "角色伴随脚本绑定被拒绝：" + candidate2.RelativePath + "：" + diagnostic2);
					GD.PushWarning(TrackFailedApplyUntilRollback(mod, ownerId) ? ("[ModLoader] package rolled back after companion binding failure: " + ownerId) : ("[ModLoader] companion binding failure remains tracked pending rollback: " + ownerId));
					return false;
				}
				runtimeRoot.Free();
				mod.CompanionBoundResourceCount++;
			}
			string text2 = item2.Key;
			Variant value = Variant.From(in resource);
			if (resource is LevelCatalogConfig levelCatalogConfig)
			{
				value = Variant.From<Dictionary>(XWModContentCatalog.FromResource(levelCatalogConfig));
				if (!flag && !string.IsNullOrWhiteSpace(levelCatalogConfig.catalogKey))
				{
					text2 = levelCatalogConfig.catalogKey.Trim();
				}
			}
			if (!XWModRuntimeRegistry.Register(ownerId, candidate2.Category, text2, value, item2.AllowOverride, out var diagnostic3))
			{
				AddDiagnostic(mod, $"runtime registry rejected resource: {candidate2.Category}/{text2}: {diagnostic3}: {ownerId}: {candidate2.RelativePath}");
				if (item2.Required)
				{
					TrackFailedApplyUntilRollback(mod, ownerId);
					return false;
				}
			}
			else
			{
				mod.AppliedResourceCount++;
			}
		}
		if (mod.CharacterCompanionRuntime != null && !mod.CharacterCompanionRuntime.TryInitializeRuntimeEntry(mod.Manifest, mod.ExtractRoot, out var diagnostic4))
		{
			AddDiagnostic(mod, "运行入口初始化失败：" + diagnostic4);
			GD.PushWarning(TrackFailedApplyUntilRollback(mod, ownerId) ? ("[ModLoader] package rolled back after runtime entry failure: " + ownerId) : ("[ModLoader] runtime entry failure remains tracked pending rollback: " + ownerId));
			return false;
		}
		if (flag && !ValidateManifestRegistrations(mod, ownerId))
		{
			TrackFailedApplyUntilRollback(mod, ownerId);
			return false;
		}
		if (!XWModContentValidation.Validate(mod.Manifest, out var error))
		{
			AddDiagnostic(mod, "Mod 内容关联无效：" + error);
			TrackFailedApplyUntilRollback(mod, ownerId);
			return false;
		}
		if (!mod.HasAppliedContent)
		{
			TrackFailedApplyUntilRollback(mod, ownerId);
			GD.PushWarning("[ModLoader] package not applied: " + ownerId + "; no supported resources, runtime entry, or state-machine callbacks were registered");
			return false;
		}
		mod.State = ApplicationState.Applied;
		XWModCharacterCompanionRuntime characterCompanionRuntime = mod.CharacterCompanionRuntime;
		mod.EffectiveMode = ((characterCompanionRuntime != null && characterCompanionRuntime.IsLoaded) ? EffectiveRuntimeMode.Managed : (XWModRuntimeCompatibility.HasManagedCode(mod.Manifest) ? EffectiveRuntimeMode.OptionalResourceFallback : EffectiveRuntimeMode.ResourceOnly));
		LoadedMods.Add(mod);
		XWModEnvironmentService.Invalidate("RuntimeApplied");
		if (mod.CharacterCompanionRuntime != null)
		{
			CompanionOwners.Add(ownerId);
		}
		GD.Print($"[ModLoader] package applied: {ownerId}; resources={mod.AppliedResourceCount}; runtimeEntry={mod.RuntimeEntryInitialized}; callbacks={mod.StateMachineCallbackCount}; diagnostics={mod.Diagnostics.Count}");
		return true;
	}

	public static bool UnloadMod(string ownerId)
	{
		StateMachineUnloadBlockers blockers;
		return UnloadMod(ownerId, out blockers);
	}

	internal static bool RollbackMod(string ownerId, out StateMachineUnloadBlockers blockers)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return false;
		}
		LoadedMod loadedMod = LoadedMods.LastOrDefault((LoadedMod item) => string.Equals(GetOwnerId(item), ownerId, StringComparison.OrdinalIgnoreCase));
		loadedMod?.PrepareLifecycleRollback();
		bool num = UnloadMod(ownerId, out blockers);
		if (!num && loadedMod != null)
		{
			loadedMod.State = ApplicationState.PendingRollback;
		}
		XWModEnvironmentService.Invalidate("LifecycleRollback");
		return num;
	}

	internal static bool IsTracked(LoadedMod mod)
	{
		if (mod != null)
		{
			return LoadedMods.Contains(mod);
		}
		return false;
	}

	public static bool UnloadMod(string ownerId, out StateMachineUnloadBlockers blockers)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return false;
		}
		if (string.IsNullOrWhiteSpace(ownerId))
		{
			return false;
		}
		if (!TryRemoveTrackedOwner(ownerId, out var removedCount, out blockers))
		{
			GD.PushWarning("[ModLoader] " + XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(ownerId, blockers));
			return false;
		}
		int num = XWModRuntimeRegistry.UnregisterOwner(ownerId);
		return removedCount > 0 || num > 0;
	}

	private static bool TrackFailedApplyUntilRollback(LoadedMod mod, string ownerId)
	{
		mod.State = ApplicationState.Failed;
		XWModEnvironmentService.Invalidate("ApplyFailed");
		XWModRuntimeRegistry.UnregisterOwner(ownerId);
		mod.ClearRuntimeResourceCache();
		mod.AppliedResourceCount = 0;
		mod.CompanionBoundResourceCount = 0;
		mod.PrepareLifecycleRollback();
		if (mod.UnloadRuntimeAssembly(out var blockers))
		{
			return true;
		}
		mod.State = ApplicationState.PendingRollback;
		AddDiagnostic(mod, XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(ownerId, blockers));
		if (!LoadedMods.Contains(mod))
		{
			LoadedMods.Add(mod);
		}
		if (mod.CharacterCompanionRuntime != null)
		{
			CompanionOwners.Add(ownerId);
		}
		GD.PushWarning("[ModLoader] failed apply remains tracked: " + XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(ownerId, blockers));
		return false;
	}

	public static void UnloadAll()
	{
		TryUnloadAll();
	}

	public static bool TryUnloadAll()
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			GD.PushWarning("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
			return false;
		}
		foreach (LoadedMod loadedMod in LoadedMods)
		{
			if (!loadedMod.CanUnloadRuntimeAssembly(out var blockers))
			{
				GD.PushWarning("[ModLoader] " + XWModCharacterCompanionRuntime.FormatUnloadBlockedDiagnostic(GetOwnerId(loadedMod), blockers));
				return false;
			}
		}
		string[] array = (from value in LoadedMods.Select(GetOwnerId)
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct<string>(StringComparer.OrdinalIgnoreCase).Reverse().ToArray();
		bool flag = true;
		string[] array2 = array;
		foreach (string ownerId in array2)
		{
			flag &= UnloadMod(ownerId, out var _);
		}
		return flag;
	}

	public static IReadOnlyList<LoadedMod> GetLoadedMods()
	{
		return LoadedMods;
	}

	public static bool TryInstantiateCharacter(string ownerId, string characterKey, out Node runtimeRoot, out string diagnostic)
	{
		runtimeRoot = null;
		diagnostic = "";
		LoadedMod loadedMod = LoadedMods.LastOrDefault((LoadedMod item) => string.Equals(GetOwnerId(item), ownerId, StringComparison.OrdinalIgnoreCase));
		if (loadedMod?.CharacterCompanionRuntime == null)
		{
			diagnostic = "该 Mod 没有已加载的角色伴随脚本运行时。";
			return false;
		}
		if (!XWModRuntimeRegistry.TryGetEffectiveRegistration("Character", characterKey, out var registration) || !string.Equals(registration.OwnerMod, ownerId, StringComparison.OrdinalIgnoreCase) || !(registration.ModValue.AsGodotObject() is PackedScene authoredScene))
		{
			diagnostic = "Mod 角色注册不可用：" + characterKey + "。";
			return false;
		}
		return loadedMod.CharacterCompanionRuntime.TryCreateInstance(authoredScene, out runtimeRoot, out diagnostic);
	}

	public static bool TryInstantiateEffectiveCharacter(string characterKey, out TowerDefenseCharacter character, out string diagnostic)
	{
		character = null;
		diagnostic = "";
		if (CompanionOwners.Count == 0 || string.IsNullOrWhiteSpace(characterKey))
		{
			return false;
		}
		if (!XWModRuntimeRegistry.TryGetEffectiveRegistration("Character", characterKey, out var registration) || !CompanionOwners.Contains(registration.OwnerMod))
		{
			return false;
		}
		if (!TryInstantiateCharacter(registration.OwnerMod, characterKey, out var runtimeRoot, out diagnostic))
		{
			return false;
		}
		if (runtimeRoot is TowerDefenseCharacter towerDefenseCharacter)
		{
			character = towerDefenseCharacter;
			return true;
		}
		if (GodotObject.IsInstanceValid(runtimeRoot))
		{
			runtimeRoot.Free();
		}
		diagnostic = "当前生效的 Mod 角色根节点类型错误：" + characterKey + "。";
		return false;
	}

	private static bool TryRemoveTrackedOwner(string ownerId, out int removedCount, out StateMachineUnloadBlockers blockers, LoadedMod preservedRuntimeCache = null)
	{
		removedCount = 0;
		blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
		LoadedMod[] removed = LoadedMods.Where((LoadedMod mod) => string.Equals(GetOwnerId(mod), ownerId, StringComparison.OrdinalIgnoreCase)).ToArray();
		LoadedMod[] array = removed;
		for (int num = 0; num < array.Length; num++)
		{
			if (!array[num].UnloadRuntimeAssembly(out blockers))
			{
				return false;
			}
		}
		array = removed;
		foreach (LoadedMod loadedMod in array)
		{
			loadedMod.State = ApplicationState.Unloaded;
			if (loadedMod != preservedRuntimeCache)
			{
				loadedMod.ClearRuntimeResourceCache();
			}
		}
		CompanionOwners.Remove(ownerId);
		removedCount = LoadedMods.RemoveAll((LoadedMod mod) => removed.Contains(mod));
		if (removedCount > 0)
		{
			XWModEnvironmentService.Invalidate("RuntimeUnloaded");
		}
		return true;
	}

	private static void AddDiagnostic(LoadedMod mod, string diagnostic)
	{
		if (mod != null && !string.IsNullOrWhiteSpace(diagnostic) && !mod.Diagnostics.Contains<string>(diagnostic, StringComparer.Ordinal))
		{
			mod.Diagnostics.Add(diagnostic);
		}
	}

	private static bool ResolveDeclaredRuntimeAssembly(LoadedMod mod)
	{
		string declared = (mod?.Manifest?.RuntimeAssembly ?? "").Replace('\\', '/').Trim('/');
		if (string.IsNullOrWhiteSpace(declared))
		{
			return true;
		}
		if (!declared.Equals("Runtime/ModAssembly.dll", StringComparison.Ordinal) || Path.IsPathRooted(declared) || declared.Contains("..", StringComparison.Ordinal) || declared.Contains(':'))
		{
			AddDiagnostic(mod, "声明的运行程序集路径无效：" + declared);
			GD.PushWarning("[ModLoader] package rejected: invalid declared runtime assembly path: " + declared);
			return false;
		}
		int num = mod.RelativeFiles.FindIndex((string relative) => relative.Equals(declared, StringComparison.Ordinal));
		if (num < 0 || num >= mod.ExtractedFiles.Count || !File.Exists(mod.ExtractedFiles[num]))
		{
			AddDiagnostic(mod, "声明的运行程序集不存在：" + declared);
			if (mod.Manifest.IsRuntimeAssemblyRequired())
			{
				GD.PushWarning("[ModLoader] package rejected: declared runtime assembly is missing: " + declared);
				return false;
			}
			return true;
		}
		mod.RuntimeAssemblyPath = mod.ExtractedFiles[num];
		return true;
	}

	private static bool IsDeclaredRuntimeAssembly(LoadedMod mod, string relativePath)
	{
		string text = (mod?.Manifest?.RuntimeAssembly ?? "").Replace('\\', '/').Trim('/');
		if (!string.IsNullOrWhiteSpace(text))
		{
			return string.Equals((relativePath ?? "").Replace('\\', '/').Trim('/'), text, StringComparison.Ordinal);
		}
		return false;
	}

	private static bool IsDeclaredRuntimeSymbols(LoadedMod mod, string relativePath)
	{
		string text = (mod?.Manifest?.RuntimeAssembly ?? "").Replace('\\', '/').Trim('/');
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return string.Equals((relativePath ?? "").Replace('\\', '/').Trim('/'), Path.ChangeExtension(text, ".pdb").Replace('\\', '/'), StringComparison.Ordinal);
	}

	private static bool IsRuntimeDependencyFile(string relativePath)
	{
		string text = (relativePath ?? "").Replace('\\', '/').Trim('/');
		if (!text.StartsWith("Runtime/Dependencies/", StringComparison.Ordinal))
		{
			return false;
		}
		string text2 = text.Substring("Runtime/Dependencies/".Length);
		if (string.IsNullOrWhiteSpace(text2) || text2.Contains('/'))
		{
			return false;
		}
		string extension = Path.GetExtension(text2);
		if (!extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
		{
			return extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool CharacterRequiresCompanion(PackedScene scene)
	{
		Node node = null;
		try
		{
			node = scene?.Instantiate(PackedScene.GenEditState.Disabled);
			return GodotObject.IsInstanceValid(node) && node.HasMeta("mod_character_script_binding") && string.Equals(node.GetMeta("mod_character_script_binding").AsString(), "CompanionOnly", StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
		}
	}

	private static bool IsAnimationAtlasPackageDependency(string relativePath)
	{
		string text = (relativePath ?? "").Replace('\\', '/').Trim('/');
		string extension = Path.GetExtension(text);
		if (text.StartsWith("Resources/AnimationAtlasManifests/", StringComparison.OrdinalIgnoreCase))
		{
			return new string[10] { ".tres", ".res", ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga", ".exr", ".hdr" }.Contains<string>(extension, StringComparer.OrdinalIgnoreCase);
		}
		return false;
	}

	public static bool ValidateArchiveEntryPath(string extractRoot, string entryName, out string relativePath, out string outputPath)
	{
		relativePath = (entryName ?? "").Replace('\\', '/').TrimStart('/');
		outputPath = "";
		if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(entryName ?? "") || (entryName ?? "").StartsWith("/", StringComparison.Ordinal) || relativePath.Contains(':'))
		{
			return false;
		}
		string[] array = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0 || array.Any((string segment) => (segment == "." || segment == "..") ? true : false))
		{
			return false;
		}
		relativePath = string.Join('/', array);
		string text = Path.GetFullPath(extractRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		outputPath = Path.GetFullPath(Path.Combine(text, relativePath.Replace('/', Path.DirectorySeparatorChar)));
		string value = text + Path.DirectorySeparatorChar;
		if (!outputPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return string.Equals(Path.GetRelativePath(text, outputPath).Replace('\\', '/'), relativePath, StringComparison.Ordinal);
	}

	public static bool InferRuntimeEntry(string relativePath, out string category, out string key)
	{
		category = "";
		key = "";
		string text = (relativePath ?? "").Replace('\\', '/').Trim('/');
		string extension = Path.GetExtension(text);
		if (text.StartsWith("Assets/Audio/", StringComparison.OrdinalIgnoreCase) && (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) || extension.Equals(".flac", StringComparison.OrdinalIgnoreCase)))
		{
			category = "Audio";
			key = Path.GetFileNameWithoutExtension(text);
			return !string.IsNullOrWhiteSpace(key);
		}
		if ((text.StartsWith("Assets/Textures/", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Assets/Texture/", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Assets/Images/", StringComparison.OrdinalIgnoreCase)) && XWModExternalMediaLoader.IsSupportedTextureExtension(extension))
		{
			category = "Texture";
			key = Path.GetFileNameWithoutExtension(text);
			return !string.IsNullOrWhiteSpace(key);
		}
		if (TryInferCharacterScene(text, out key))
		{
			category = "Character";
			return true;
		}
		if (TryInferCharacterScene(text, out key, "Sprite"))
		{
			category = "CharacterSprite";
			return true;
		}
		if (!extension.Equals(".tres", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".res", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (text.StartsWith("Resources/Tutorials/Conditions/", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Resources/Tutorials/Steps/", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		(string, string)[] array = new(string, string)[17]
		{
			("Battle/Features/", "Feature"),
			("Battle/Processes/", "Process"),
			("Resources/LevelCatalogs/", "Level"),
			("Resources/Maps/", "Map"),
			("Resources/Cards/", "Packet"),
			("Resources/PacketBank/", "PacketBank"),
			("Resources/Projectiles/", "Projectile"),
			("Resources/ProjectileChanges/", "ProjectileChange"),
			("Resources/Collectables/", "Collectable"),
			("Resources/Mowers/", "Mower"),
			("Resources/Shovels/", "Shovel"),
			("Resources/Survivals/", "Survival"),
			("Resources/Tutorials/", "Tutorial"),
			("Resources/NpcTalks/", "NpcTalk"),
			("Resources/Shops/", "Shop"),
			("Resources/AnimationAtlasProfiles/", "AnimationAtlas"),
			("Resources/BGMConfigs/", "BGM")
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (value, text2) = array[i];
			if (text.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				category = text2;
				key = Path.GetFileNameWithoutExtension(text);
				return !string.IsNullOrWhiteSpace(key);
			}
		}
		return false;
	}

	private static bool TryLoadRuntimeResource(LoadedMod mod, RuntimeCandidate candidate, out Resource resource)
	{
		resource = null;
		string key = NormalizeCategory(candidate.Category) + "\n" + Path.GetFullPath(candidate.ExtractedPath);
		if (mod.RuntimeResourceCache.TryGetValue(key, out var value) && GodotObject.IsInstanceValid(value))
		{
			resource = value;
			return true;
		}
		if (!CanAdmitRuntimeCacheEntry(mod, candidate, out var sourceBytes, out var diagnostic))
		{
			AddDiagnostic(mod, "runtime media cache rejected: " + candidate.RelativePath + ": " + diagnostic);
			return false;
		}
		if (!TryGetGodotResourcePath(candidate.ExtractedPath, out var resourcePath))
		{
			AddDiagnostic(mod, "resource path is outside Godot resource roots: " + candidate.RelativePath);
			return false;
		}
		try
		{
			if (candidate.Category.Equals("Audio", StringComparison.OrdinalIgnoreCase))
			{
				if (!XWModExternalMediaLoader.TryLoadAudio(resourcePath, Path.GetExtension(candidate.RelativePath), out var audio, out var diagnostic2))
				{
					AddDiagnostic(mod, "audio runtime load rejected: " + candidate.RelativePath + ": " + diagnostic2);
					return false;
				}
				resource = audio;
			}
			else if (candidate.Category.Equals("Texture", StringComparison.OrdinalIgnoreCase))
			{
				if (!XWModExternalMediaLoader.TryLoadTexture(resourcePath, Path.GetExtension(candidate.RelativePath), out var texture, out var diagnostic3))
				{
					AddDiagnostic(mod, "texture runtime load rejected: " + candidate.RelativePath + ": " + diagnostic3);
					return false;
				}
				resource = texture;
			}
			else if (candidate.Category.Equals("Character", StringComparison.OrdinalIgnoreCase) || candidate.Category.Equals("CharacterSprite", StringComparison.OrdinalIgnoreCase))
			{
				if (!PrepareSafeCharacterPackage(mod, candidate))
				{
					return false;
				}
				resource = ResourceLoader.Load<PackedScene>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
			}
			else
			{
				resource = ResourceLoader.Load<Resource>(resourcePath, "", ResourceLoader.CacheMode.Ignore);
			}
			if (candidate.Category.Equals("AnimationAtlas", StringComparison.OrdinalIgnoreCase))
			{
				if (!(resource is AdobeAnimateAtlasProfile profile))
				{
					AddDiagnostic(mod, "animation atlas profile has the wrong resource type: " + candidate.RelativePath);
					return false;
				}
				if (!TryPreparePackagedAnimationAtlasProfile(mod, candidate, profile, out var diagnostic4))
				{
					AddDiagnostic(mod, "animation atlas profile rejected: " + candidate.RelativePath + ": " + diagnostic4);
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			AddDiagnostic(mod, "resource load exception: " + candidate.RelativePath + ": " + ex.Message);
			return false;
		}
		if (!GodotObject.IsInstanceValid(resource))
		{
			AddDiagnostic(mod, "resource load failed: " + candidate.RelativePath);
			return false;
		}
		if (candidate.Category.Equals("Audio", StringComparison.OrdinalIgnoreCase) && !(resource is AudioStream))
		{
			AddDiagnostic(mod, "audio file did not produce an AudioStream: " + candidate.RelativePath);
			return false;
		}
		if (candidate.Category.Equals("Texture", StringComparison.OrdinalIgnoreCase) && !(resource is Texture2D))
		{
			AddDiagnostic(mod, "texture file did not produce a Texture2D: " + candidate.RelativePath);
			return false;
		}
		if (candidate.Category.Equals("Character", StringComparison.OrdinalIgnoreCase) && !(resource is PackedScene))
		{
			AddDiagnostic(mod, "character scene did not produce a PackedScene: " + candidate.RelativePath);
			return false;
		}
		long texturePixels = ((resource is Texture2D texture2D) ? ((long)texture2D.GetWidth() * (long)texture2D.GetHeight()) : 0);
		long decodedAudioBytes = ((resource is AudioStreamWav audioStreamWav) ? audioStreamWav.Data.LongLength : 0);
		if (!CanAdmitDecodedTexture(mod, texturePixels, out diagnostic))
		{
			AddDiagnostic(mod, "runtime media cache rejected: " + candidate.RelativePath + ": " + diagnostic);
			resource.Dispose();
			resource = null;
			return false;
		}
		if (!CanAdmitDecodedAudio(mod, decodedAudioBytes, out diagnostic))
		{
			AddDiagnostic(mod, "runtime media cache rejected: " + candidate.RelativePath + ": " + diagnostic);
			resource.Dispose();
			resource = null;
			return false;
		}
		mod.RuntimeResourceCache[key] = resource;
		mod.AccountCachedRuntimeResource(sourceBytes, texturePixels, decodedAudioBytes);
		return true;
	}

	private static bool CanAdmitRuntimeCacheEntry(LoadedMod mod, RuntimeCandidate candidate, out long sourceBytes, out string diagnostic)
	{
		sourceBytes = 0L;
		diagnostic = "";
		try
		{
			sourceBytes = new FileInfo(candidate.ExtractedPath).Length;
		}
		catch (Exception ex)
		{
			diagnostic = "source size unavailable: " + ex.Message;
			return false;
		}
		int num = LoadedMods.Sum((LoadedMod item) => item.RuntimeResourceCache.Count) + mod.RuntimeResourceCache.Count;
		long num2 = LoadedMods.Sum((LoadedMod item) => item.CachedRuntimeSourceBytes) + mod.CachedRuntimeSourceBytes;
		if (mod.RuntimeResourceCache.Count >= 512 || num >= 4096)
		{
			diagnostic = "runtime resource cache entry limit exceeded";
			return false;
		}
		if (sourceBytes < 0 || mod.CachedRuntimeSourceBytes + sourceBytes > 268435456 || num2 + sourceBytes > 536870912)
		{
			diagnostic = "runtime resource cache byte limit exceeded";
			return false;
		}
		return true;
	}

	private static bool CanAdmitDecodedTexture(LoadedMod mod, long texturePixels, out string diagnostic)
	{
		diagnostic = "";
		if (texturePixels <= 0)
		{
			return true;
		}
		long num = LoadedMods.Sum((LoadedMod item) => item.CachedRuntimeTexturePixels) + mod.CachedRuntimeTexturePixels;
		if (mod.CachedRuntimeTexturePixels + texturePixels > 67108864 || num + texturePixels > 134217728)
		{
			diagnostic = "runtime decoded texture pixel budget exceeded";
			return false;
		}
		return true;
	}

	private static bool CanAdmitDecodedAudio(LoadedMod mod, long decodedAudioBytes, out string diagnostic)
	{
		diagnostic = "";
		if (decodedAudioBytes <= 0)
		{
			return true;
		}
		long num = LoadedMods.Sum((LoadedMod item) => item.CachedRuntimeDecodedAudioBytes) + mod.CachedRuntimeDecodedAudioBytes;
		if (mod.CachedRuntimeDecodedAudioBytes + decodedAudioBytes > 134217728 || num + decodedAudioBytes > 268435456)
		{
			diagnostic = "runtime decoded audio PCM byte budget exceeded";
			return false;
		}
		return true;
	}

	private static bool TryInferCharacterScene(string normalizedPath, out string key, string folder = "Scene")
	{
		key = "";
		string[] array = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 6 || !array[0].Equals("Resources", StringComparison.OrdinalIgnoreCase) || !array[1].Equals("Characters", StringComparison.OrdinalIgnoreCase) || !IsKnownCharacterCategory(array[2]) || !array[4].Equals(folder, StringComparison.OrdinalIgnoreCase) || !Path.GetExtension(array[5]).Equals(".tscn", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(array[5]);
		if (string.IsNullOrWhiteSpace(array[3]) || !fileNameWithoutExtension.Equals(array[3], StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		key = array[3];
		return true;
	}

	private static bool IsKnownCharacterCategory(string category)
	{
		if (!category.Equals("Plants", StringComparison.OrdinalIgnoreCase) && !category.Equals("Zombies", StringComparison.OrdinalIgnoreCase) && !category.Equals("Props", StringComparison.OrdinalIgnoreCase) && !category.Equals("Vases", StringComparison.OrdinalIgnoreCase) && !category.Equals("Mowers", StringComparison.OrdinalIgnoreCase) && !category.Equals("Items", StringComparison.OrdinalIgnoreCase) && !category.Equals("Graves", StringComparison.OrdinalIgnoreCase))
		{
			return category.Equals("Craters", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool IsCharacterPackageDependency(string relativePath)
	{
		string[] array = (relativePath ?? "").Replace('\\', '/').Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length >= 5 && array[0].Equals("Resources", StringComparison.OrdinalIgnoreCase) && array[1].Equals("Characters", StringComparison.OrdinalIgnoreCase))
		{
			return IsKnownCharacterCategory(array[2]);
		}
		return false;
	}

	private static bool IsExecutablePackageFile(string relativePath)
	{
		string text = Path.GetExtension(relativePath ?? "").ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 3)
			{
				if (length == 4)
				{
					switch (text[1])
					{
					case 'd':
						break;
					case 'e':
						goto IL_009b;
					case 'b':
						goto IL_00aa;
					case 'c':
						goto IL_00b9;
					case 'p':
						goto IL_00c8;
					default:
						goto IL_00d9;
					}
					if (text == ".dll")
					{
						goto IL_00d5;
					}
				}
			}
			else
			{
				char c = text[1];
				if (c != 'c')
				{
					if (c == 'g' && text == ".gd")
					{
						goto IL_00d5;
					}
				}
				else if (text == ".cs")
				{
					goto IL_00d5;
				}
			}
		}
		goto IL_00d9;
		IL_00aa:
		if (text == ".bat")
		{
			goto IL_00d5;
		}
		goto IL_00d9;
		IL_00d9:
		return false;
		IL_009b:
		if (text == ".exe")
		{
			goto IL_00d5;
		}
		goto IL_00d9;
		IL_00b9:
		if (text == ".cmd")
		{
			goto IL_00d5;
		}
		goto IL_00d9;
		IL_00d5:
		return true;
		IL_00c8:
		if (text == ".ps1")
		{
			goto IL_00d5;
		}
		goto IL_00d9;
	}

	private static bool PrepareSafeCharacterPackage(LoadedMod mod, RuntimeCandidate candidate)
	{
		string[] array = candidate.RelativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 6)
		{
			return false;
		}
		string value = string.Join('/', array.Take(4)) + "/";
		for (int i = 0; i < mod.RelativeFiles.Count && i < mod.ExtractedFiles.Count; i++)
		{
			string text = mod.RelativeFiles[i].Replace('\\', '/');
			if (text.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				string text2 = Path.GetExtension(text).ToLowerInvariant();
				if ((text2 == ".scn" || text2 == ".res") ? true : false)
				{
					AddDiagnostic(mod, "character package rejected unsafe binary dependency: " + text);
					return false;
				}
				bool flag = ((text2 == ".tscn" || text2 == ".tres") ? true : false);
				if (flag && !SanitizeCharacterTextResource(mod, text, mod.ExtractedFiles[i], text2 == ".tscn"))
				{
					return false;
				}
			}
		}
		return true;
	}

	private static bool SanitizeCharacterTextResource(LoadedMod mod, string relativePath, string absolutePath, bool canStripScripts)
	{
		string text = File.ReadAllText(absolutePath);
		if (text.Contains("type=\"GDScript\"", StringComparison.OrdinalIgnoreCase) || text.Contains("type=\"CSharpScript\"", StringComparison.OrdinalIgnoreCase))
		{
			AddDiagnostic(mod, "character package rejected embedded script: " + relativePath);
			return false;
		}
		List<string> blockedIds = new List<string>();
		List<string> list = new List<string>();
		bool flag = false;
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		foreach (string text2 in array)
		{
			if (text2.Contains("[ext_resource", StringComparison.OrdinalIgnoreCase) && text2.Contains("type=\"Script\"", StringComparison.OrdinalIgnoreCase))
			{
				string text3 = ExtractAttribute(text2, "path");
				if (!text3.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
				{
					if (!canStripScripts)
					{
						AddDiagnostic(mod, "character package rejected untrusted script reference: " + relativePath + ": " + text3);
						return false;
					}
					string text4 = ExtractAttribute(text2, "id");
					if (!string.IsNullOrWhiteSpace(text4))
					{
						blockedIds.Add(text4);
					}
					AddDiagnostic(mod, "blocked untrusted character script reference: " + relativePath + ": " + text3);
					flag = true;
					continue;
				}
			}
			list.Add(text2);
		}
		if (blockedIds.Count > 0)
		{
			list.RemoveAll((string line) => blockedIds.Any((string id) => line.Contains("ExtResource(\"" + id + "\")", StringComparison.Ordinal)));
		}
		if (flag)
		{
			File.WriteAllText(absolutePath, string.Join('\n', list));
		}
		return true;
	}

	private static string ExtractAttribute(string line, string attribute)
	{
		Match match = Regex.Match(line ?? "", "(?:^|\\s)" + Regex.Escape(attribute) + "=\"([^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (!match.Success)
		{
			return "";
		}
		return match.Groups[1].Value;
	}

	private static List<(string Key, bool Override)> GetManifestDeclarations(XWModManifest manifest, string category)
	{
		List<(string, bool)> list = new List<(string, bool)>();
		AddManifestDeclarations(list, manifest?.Provides, category, allowOverride: false);
		AddManifestDeclarations(list, manifest?.Overrides, category, allowOverride: true);
		return (from @group in list.GroupBy<(string, bool), string>(((string Key, bool Override) item) => item.Key, StringComparer.OrdinalIgnoreCase)
			select (Key: @group.Key, @group.Any(((string Key, bool Override) item) => item.Override))).ToList();
	}

	private static void AddManifestDeclarations(List<(string Key, bool Override)> result, System.Collections.Generic.Dictionary<string, List<string>> entries, string category, bool allowOverride)
	{
		if (entries == null)
		{
			return;
		}
		foreach (KeyValuePair<string, List<string>> entry in entries)
		{
			if (!NormalizeCategory(entry.Key).Equals(NormalizeCategory(category), StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			foreach (string item in entry.Value ?? new List<string>())
			{
				if (!string.IsNullOrWhiteSpace(item))
				{
					result.Add((item.Trim(), allowOverride));
				}
			}
		}
	}

	private static bool IsLevelPackageDependency(string relativePath)
	{
		string text = (relativePath ?? "").Replace('\\', '/').Trim('/');
		bool flag = text.StartsWith("Resources/Levels/", StringComparison.OrdinalIgnoreCase);
		if (flag)
		{
			string text2 = Path.GetExtension(text).ToLowerInvariant();
			bool flag2 = ((text2 == ".tres" || text2 == ".res") ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private static bool ValidateManifestRegistrations(LoadedMod mod, string ownerId)
	{
		return ValidateManifestRegistrations(mod, ownerId, mod.Manifest?.Provides, isOverride: false) & ValidateManifestRegistrations(mod, ownerId, mod.Manifest?.Overrides, isOverride: true);
	}

	private static bool ValidateManifestRegistrations(LoadedMod mod, string ownerId, System.Collections.Generic.Dictionary<string, List<string>> entries, bool isOverride)
	{
		if (entries == null)
		{
			return true;
		}
		bool result = true;
		foreach (KeyValuePair<string, List<string>> pair in entries)
		{
			foreach (string key in pair.Value ?? new List<string>())
			{
				if (!string.IsNullOrWhiteSpace(key) && !(from registration in XWModRuntimeRegistry.GetRegistrations().Values.SelectMany((System.Collections.Generic.Dictionary<string, XWModRuntimeRegistry.Registration> registrations) => registrations.Values)
					where NormalizeCategory(registration.Category).Equals(NormalizeCategory(pair.Key), StringComparison.OrdinalIgnoreCase) && registration.Key.Equals(key.Trim(), StringComparison.OrdinalIgnoreCase)
					select registration).SelectMany((XWModRuntimeRegistry.Registration registration) => XWModRuntimeRegistry.GetRegistrationStack(registration.Category, registration.Key)).Any((XWModRuntimeRegistry.Registration registration) => registration.OwnerMod.Equals(ownerId, StringComparison.OrdinalIgnoreCase) && registration.IsOverride == isOverride))
				{
					result = false;
					AddDiagnostic(mod, $"required manifest runtime entry is not registered: {ownerId}: {(isOverride ? "overrides" : "provides")}: {pair.Key}/{key.Trim()}");
				}
			}
		}
		return result;
	}

	private static void ParseManifest(string json, LoadedMod mod)
	{
		ParseManifest(json, out var manifest, out var info);
		mod.Manifest = manifest;
		mod.Info = info;
	}

	private static void ParseManifest(string json, out XWModManifest manifest, out ModExporter.ModInfo info)
	{
		manifest = null;
		info = null;
		using JsonDocument jsonDocument = JsonDocument.Parse(json);
		JsonElement rootElement = jsonDocument.RootElement;
		JsonElement value;
		bool flag = rootElement.TryGetProperty("schemaVersion", out value);
		if (flag || rootElement.TryGetProperty("id", out value) || rootElement.TryGetProperty("dependencies", out value) || rootElement.TryGetProperty("conflicts", out value) || rootElement.TryGetProperty("provides", out value) || rootElement.TryGetProperty("overrides", out value) || rootElement.TryGetProperty("scripts", out value) || rootElement.TryGetProperty("runtimeAssembly", out value) || rootElement.TryGetProperty("runtimeEntryType", out value) || rootElement.TryGetProperty("runtimeApiVersion", out value) || rootElement.TryGetProperty("runtimeAssemblyPolicy", out value) || rootElement.TryGetProperty("blueprints", out value) || rootElement.TryGetProperty("translations", out value) || rootElement.TryGetProperty("resources", out value))
		{
			manifest = JsonSerializer.Deserialize(json, XWModJsonContext.Default.XWModManifest);
			if (manifest != null && !flag)
			{
				manifest.SchemaVersion = 1;
			}
		}
		else
		{
			info = JsonSerializer.Deserialize(json, XWModJsonContext.Default.ModInfo);
		}
	}

	private static void NormalizeManifest(XWModManifest manifest, ModExporter.ModInfo info, string pmodPath)
	{
		if (manifest != null)
		{
			if (!XWModRuntimeCompatibility.ValidatePackage(manifest, out var diagnostic))
			{
				throw new InvalidDataException(diagnostic);
			}
			if (manifest.SchemaVersion <= 0)
			{
				manifest.SchemaVersion = 1;
			}
			if (manifest.SchemaVersion > 2)
			{
				throw new InvalidDataException($"unsupported mod schema version: {manifest.SchemaVersion}");
			}
			XWModManifestSyncService.NormalizeManifestCollections(manifest);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pmodPath ?? "mod");
			if (string.IsNullOrWhiteSpace(manifest.Id))
			{
				manifest.Id = fileNameWithoutExtension;
			}
			if (string.IsNullOrWhiteSpace(manifest.Name))
			{
				manifest.Name = (string.IsNullOrWhiteSpace(info?.Name) ? manifest.Id : info.Name);
			}
			if (string.IsNullOrWhiteSpace(manifest.Version))
			{
				manifest.Version = (string.IsNullOrWhiteSpace(info?.Version) ? "1.0.0" : info.Version);
			}
			string text = (manifest.RuntimeAssemblyPolicy ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !text.Equals("required", StringComparison.OrdinalIgnoreCase) && !text.Equals("optional", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("invalid runtimeAssemblyPolicy: " + manifest.RuntimeAssemblyPolicy);
			}
			if (!string.IsNullOrWhiteSpace(manifest.RuntimeEntryType) && string.IsNullOrWhiteSpace(manifest.RuntimeAssembly))
			{
				throw new InvalidDataException("runtimeEntryType requires runtimeAssembly");
			}
		}
	}

	private static XWModManifest CreateLegacyManifest(ModExporter.ModInfo info, string pmodPath)
	{
		if (info == null)
		{
			return null;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(pmodPath ?? "mod");
		return new XWModManifest
		{
			SchemaVersion = 1,
			Id = (string.IsNullOrWhiteSpace(info.Name) ? fileNameWithoutExtension : info.Name),
			Name = (string.IsNullOrWhiteSpace(info.Name) ? fileNameWithoutExtension : info.Name),
			Version = (string.IsNullOrWhiteSpace(info.Version) ? "1.0.0" : info.Version),
			Author = (info.Author ?? ""),
			Description = (info.Description ?? "")
		};
	}

	private static bool TryGetGodotResourcePath(string absolutePath, out string resourcePath)
	{
		resourcePath = ProjectSettings.LocalizePath(absolutePath).Replace('\\', '/');
		if (resourcePath.StartsWith("user://", StringComparison.OrdinalIgnoreCase) || resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string fullPath = Path.GetFullPath(absolutePath);
		(string, string)[] array = new(string, string)[2]
		{
			("user://", ProjectSettings.GlobalizePath("user://")),
			("res://", ProjectSettings.GlobalizePath("res://"))
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string item = tuple.Item1;
			string text = Path.GetFullPath(tuple.Item2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string value = text + Path.DirectorySeparatorChar;
			if (fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				string text2 = Path.GetRelativePath(text, fullPath).Replace('\\', '/');
				if (!text2.StartsWith("../", StringComparison.Ordinal) && !(text2 == ".."))
				{
					resourcePath = item + text2;
					return true;
				}
			}
		}
		resourcePath = "";
		return false;
	}

	private static bool TryPreparePackagedAnimationAtlasProfile(LoadedMod mod, RuntimeCandidate candidate, AdobeAnimateAtlasProfile profile, out string diagnostic)
	{
		diagnostic = "";
		string text = profile?.ManifestPath?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			diagnostic = "manifest path is empty";
			return false;
		}
		string resourcePath = "";
		string extractedResource;
		bool flag = TryResolvePackagedResource(mod, candidate.ExtractedPath, text, out extractedResource) && TryGetGodotResourcePath(extractedResource, out resourcePath);
		if (flag)
		{
			profile.ManifestPath = resourcePath;
		}
		else if (!IsBuiltInAtlasManifestPath(text))
		{
			diagnostic = "manifest is not a portable package resource: " + text;
			return false;
		}
		AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = (flag ? ResourceLoader.Load<AdobeAnimateGlobalAtlasManifest>(profile.ManifestPath, "", ResourceLoader.CacheMode.Replace) : profile.LoadManifest());
		if (!GodotObject.IsInstanceValid(adobeAnimateGlobalAtlasManifest))
		{
			diagnostic = "manifest could not be loaded after relocation: " + profile.ManifestPath;
			return false;
		}
		if (flag && !TryRelocatePackagedAtlasManifestPaths(mod, extractedResource, adobeAnimateGlobalAtlasManifest, out diagnostic))
		{
			return false;
		}
		return true;
	}

	private static bool TryRelocatePackagedAtlasManifestPaths(LoadedMod mod, string extractedManifest, AdobeAnimateGlobalAtlasManifest manifest, out string diagnostic)
	{
		diagnostic = "";
		if (!TryRelocatePackagedPath(mod, extractedManifest, manifest.AtlasTextureArrayPath, out var relocatedPath, out diagnostic) || !TryRelocatePackagedPath(mod, extractedManifest, manifest.PoseTextureArrayPath, out var relocatedPath2, out diagnostic))
		{
			return false;
		}
		manifest.AtlasTextureArrayPath = relocatedPath;
		manifest.PoseTextureArrayPath = relocatedPath2;
		for (int i = 0; i < manifest.PoseAtlasPagePaths.Count; i++)
		{
			if (!TryRelocatePackagedPath(mod, extractedManifest, manifest.PoseAtlasPagePaths[i], out var relocatedPath3, out diagnostic))
			{
				return false;
			}
			manifest.PoseAtlasPagePaths[i] = relocatedPath3;
		}
		for (int j = 0; j < manifest.ExternalTexturePaths.Count; j++)
		{
			if (!TryRelocatePackagedPath(mod, extractedManifest, manifest.ExternalTexturePaths[j], out var relocatedPath4, out diagnostic))
			{
				return false;
			}
			manifest.ExternalTexturePaths[j] = relocatedPath4;
		}
		for (int k = 0; k < manifest.SourceKeys.Count; k++)
		{
			string text = manifest.SourceKeys[k];
			if (LooksLikeResourcePath(text))
			{
				if (!TryRelocatePackagedPath(mod, extractedManifest, text, out var relocatedPath5, out diagnostic))
				{
					return false;
				}
				manifest.SourceKeys[k] = relocatedPath5;
			}
		}
		return true;
	}

	private static bool TryRelocatePackagedPath(LoadedMod mod, string ownerExtractedPath, string declaredPath, out string relocatedPath, out string diagnostic)
	{
		relocatedPath = declaredPath ?? "";
		diagnostic = "";
		if (string.IsNullOrWhiteSpace(declaredPath))
		{
			return true;
		}
		if (TryResolvePackagedResource(mod, ownerExtractedPath, declaredPath, out var extractedResource) && TryGetGodotResourcePath(extractedResource, out relocatedPath))
		{
			return true;
		}
		if (declaredPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) && ResourceLoader.Exists(declaredPath))
		{
			relocatedPath = declaredPath;
			return true;
		}
		diagnostic = "manifest dependency is not portable or missing: " + declaredPath;
		return false;
	}

	private static bool TryResolvePackagedResource(LoadedMod mod, string ownerExtractedPath, string declaredPath, out string extractedResource)
	{
		extractedResource = "";
		if (mod == null || string.IsNullOrWhiteSpace(ownerExtractedPath) || string.IsNullOrWhiteSpace(declaredPath))
		{
			return false;
		}
		string text = declaredPath.Replace('\\', '/').Trim();
		List<string> list = new List<string>();
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			list.Add(text.Substring(text.IndexOf("://", StringComparison.Ordinal) + 3));
		}
		else if (!Path.IsPathRooted(declaredPath))
		{
			try
			{
				string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ownerExtractedPath) ?? mod.ExtractRoot, text.Replace('/', Path.DirectorySeparatorChar)));
				string item = Path.GetRelativePath(mod.ExtractRoot, fullPath).Replace('\\', '/');
				list.Add(item);
			}
			catch
			{
			}
			string text2 = text.TrimStart('/');
			if (text2.Split('/', StringSplitOptions.RemoveEmptyEntries).All((string segment) => !(segment == ".") && !(segment == "..")))
			{
				list.Add(text2);
			}
		}
		else
		{
			string[] array = new string[3] { "/Resources/", "/Assets/", "/Battle/" };
			foreach (string value in array)
			{
				int num2 = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
				if (num2 >= 0)
				{
					list.Add(text.Substring(num2 + 1));
				}
			}
		}
		foreach (string item2 in list.Where((string path) => !string.IsNullOrWhiteSpace(path)).Distinct<string>(StringComparer.OrdinalIgnoreCase))
		{
			if (ValidateArchiveEntryPath(mod.ExtractRoot, item2, out var safeRelative, out var outputPath) && mod.RelativeFiles.Any((string path) => string.Equals(path.Replace('\\', '/'), safeRelative, StringComparison.OrdinalIgnoreCase)) && File.Exists(outputPath))
			{
				extractedResource = outputPath;
				return true;
			}
		}
		return false;
	}

	private static bool LooksLikeResourcePath(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string text = value.Replace('\\', '/');
		if (!text.Contains("://", StringComparison.Ordinal) && !text.StartsWith(".", StringComparison.Ordinal) && !text.StartsWith("/", StringComparison.Ordinal))
		{
			return !string.IsNullOrWhiteSpace(Path.GetExtension(text));
		}
		return true;
	}

	private static bool IsBuiltInAtlasManifestPath(string path)
	{
		return (path ?? "").Replace('\\', '/').StartsWith("res://addons/AdobeAnimateEditor/GeneratedAtlas/", StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasExplicitRuntimeEntries(XWModManifest manifest)
	{
		if (manifest != null)
		{
			System.Collections.Generic.Dictionary<string, List<string>> provides = manifest.Provides;
			if (provides == null || !provides.Any(delegate(KeyValuePair<string, List<string>> pair)
			{
				List<string> value = pair.Value;
				return value != null && value.Count > 0;
			}))
			{
				return manifest.Overrides?.Any(delegate(KeyValuePair<string, List<string>> pair)
				{
					List<string> value = pair.Value;
					return value != null && value.Count > 0;
				}) ?? false;
			}
			return true;
		}
		return false;
	}

	private static bool ManifestContains(System.Collections.Generic.Dictionary<string, List<string>> entries, string category, string key)
	{
		if (entries == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, List<string>> entry in entries)
		{
			if (NormalizeCategory(entry.Key).Equals(NormalizeCategory(category), StringComparison.OrdinalIgnoreCase))
			{
				return entry.Value?.Any((string item) => string.Equals(item, key, StringComparison.OrdinalIgnoreCase)) ?? false;
			}
		}
		return false;
	}

	private static string NormalizeCategory(string category)
	{
		string text = (category ?? "").Trim();
		switch (text.ToLowerInvariant())
		{
		case "level":
		case "levels":
			return "Level";
		case "image":
		case "images":
		case "texture":
		case "textures":
			return "Texture";
		case "audio":
		case "audios":
			return "Audio";
		case "animationatlases":
		case "atlasprofile":
		case "animationatlas":
		case "atlasprofiles":
			return "AnimationAtlas";
		case "bgm":
		case "bgms":
			return "BGM";
		case "map":
		case "maps":
			return "Map";
		case "projectile":
		case "projectiles":
			return "Projectile";
		case "charactersprite":
			return "CharacterSprite";
		case "characters":
		case "character":
			return "Character";
		case "cards":
		case "packet":
		case "packets":
		case "card":
			return "Packet";
		case "packetbank":
		case "packetbanks":
			return "PacketBank";
		case "tutorial":
		case "tutorials":
			return "Tutorial";
		case "talks":
		case "npctalk":
		case "npctalks":
		case "talk":
			return "NpcTalk";
		case "collectables":
		case "collectable":
			return "Collectable";
		case "shovel":
		case "shovels":
			return "Shovel";
		case "mower":
		case "mowers":
			return "Mower";
		case "shops":
		case "shop":
			return "Shop";
		case "survival":
		case "survivals":
			return "Survival";
		case "feature":
		case "features":
			return "Feature";
		case "process":
		case "processes":
			return "Process";
		case "projectilechange":
		case "projectilechanges":
			return "ProjectileChange";
		default:
			return text;
		}
	}

	private static string GetOwnerId(LoadedMod mod)
	{
		string text = mod?.Manifest?.Id;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = mod?.Info?.Name;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = Path.GetFileNameWithoutExtension(mod?.FilePath ?? "mod");
		}
		return text.Trim();
	}

	private static string SanitizeCacheName(string name)
	{
		char[] invalid = Path.GetInvalidFileNameChars();
		string text = new string((name ?? "mod").Select((char character) => (!invalid.Contains(character)) ? character : '_').ToArray());
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "mod";
	}
}
