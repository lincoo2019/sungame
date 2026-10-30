using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ScriptEditor;

public sealed class XWModAssemblyLoader
{
	public sealed class LoadedModAssembly
	{
		private int _callbackCount;

		internal bool CallbacksRegistered;

		internal bool ComponentTypesRegistered;

		internal bool RegistrationsDetached;

		internal bool IsStaged;

		internal bool UnloadRequestInProgress;

		public string ModId { get; init; } = "";

		public string AssemblyPath { get; init; } = "";

		public Assembly Assembly { get; init; }

		public AssemblyLoadContext LoadContext { get; init; }

		public int StateMachineCallbackCount
		{
			get
			{
				return _callbackCount;
			}
			init
			{
				_callbackCount = value;
			}
		}

		public string LastUnloadDiagnostic { get; internal set; } = "";

		public int CharacterComponentRuntimeCount { get; internal set; }

		public bool SupportsCollectibleUnload { get; init; }

		internal void SetCallbackCount(int count)
		{
			_callbackCount = count;
		}
	}

	private sealed class AndroidLoadedAssembly
	{
		public Assembly Assembly { get; init; }

		public AssemblyName Identity { get; init; }

		public string Sha256 { get; init; } = "";

		public string OwnerId { get; init; } = "";

		public bool IsMainAssembly { get; init; }
	}

	private sealed class ModLoadContext : AssemblyLoadContext
	{
		private readonly AssemblyDependencyResolver _resolver;

		private readonly List<string> _probingRoots = new List<string>();

		public ModLoadContext(string assemblyPath, IEnumerable<string> probingRoots, bool isCollectible)
			: base(isCollectible)
		{
			_resolver = new AssemblyDependencyResolver(assemblyPath);
			if (probingRoots == null)
			{
				return;
			}
			foreach (string probingRoot in probingRoots)
			{
				if (!string.IsNullOrWhiteSpace(probingRoot))
				{
					_probingRoots.Add(probingRoot);
				}
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Runtime-loaded Mod assemblies are inspected and invoked by design.")]
		public Assembly LoadAssemblyWithoutLock(string path)
		{
			using FileStream assembly = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			string path2 = Path.ChangeExtension(path, ".pdb");
			if (File.Exists(path2))
			{
				using (FileStream assemblySymbols = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					return LoadFromStream(assembly, assemblySymbols);
				}
			}
			return LoadFromStream(assembly);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Runtime-loaded Mod dependency probing is expected for in-game Mod builds.")]
		protected override Assembly Load(AssemblyName assemblyName)
		{
			Assembly assembly = typeof(XWModAssemblyLoader).Assembly;
			if (AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName))
			{
				return assembly;
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly2 in assemblies)
			{
				if (!(AssemblyLoadContext.GetLoadContext(assembly2) is ModLoadContext) && AssemblyName.ReferenceMatchesDefinition(assembly2.GetName(), assemblyName))
				{
					return assembly2;
				}
			}
			string text = _resolver.ResolveAssemblyToPath(assemblyName);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return LoadAssemblyWithoutLock(text);
			}
			foreach (string probingRoot in _probingRoots)
			{
				string path = Path.Combine(probingRoot, assemblyName.Name + ".dll");
				if (File.Exists(path))
				{
					return LoadAssemblyWithoutLock(path);
				}
			}
			return null;
		}
	}

	private readonly Dictionary<string, LoadedModAssembly> _loaded = new Dictionary<string, LoadedModAssembly>(StringComparer.Ordinal);

	private static readonly HashSet<string> RestartRequired = new HashSet<string>(StringComparer.Ordinal);

	private static readonly object AndroidAssemblyLoadLock = new object();

	private static readonly Dictionary<string, AndroidLoadedAssembly> AndroidLoadedAssemblies = new Dictionary<string, AndroidLoadedAssembly>(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, LoadedModAssembly> LoadedAssemblies => _loaded;

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Mod assemblies are loaded from player-created build output at runtime by design.")]
	public LoadedModAssembly LoadModAssembly(string modId, string assemblyPath, IEnumerable<string> probingRoots = null, bool registerCharacterComponentRuntimes = true)
	{
		return LoadModAssemblyCore(modId, assemblyPath, probingRoots, registerCharacterComponentRuntimes, staged: false);
	}

	internal LoadedModAssembly LoadStagedModAssembly(string modId, string assemblyPath, IEnumerable<string> probingRoots = null)
	{
		return LoadModAssemblyCore(modId, assemblyPath, probingRoots, registerCharacterComponentRuntimes: false, staged: true);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Trusted Mod assemblies are loaded and discovered at runtime.")]
	private LoadedModAssembly LoadModAssemblyCore(string modId, string assemblyPath, IEnumerable<string> probingRoots, bool registerCharacterComponentRuntimes, bool staged)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			throw new InvalidOperationException("Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。");
		}
		string text = NormalizeExternalModIdOrThrow(modId);
		if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
		{
			throw new FileNotFoundException("Mod assembly was not found.", assemblyPath);
		}
		if (_loaded.ContainsKey(text) && (staged || !UnloadMod(text)))
		{
			throw new InvalidOperationException("Mod '" + text + "' cannot reload while a state-machine callback runtime is active.");
		}
		if (RestartRequired.Contains(text))
		{
			throw new InvalidOperationException("Mod '" + text + "' was unloaded from a non-collectible runtime and can only reload after restart.");
		}
		string fullPath = Path.GetFullPath(assemblyPath);
		bool flag = !OperatingSystem.IsAndroid();
		ModLoadContext modLoadContext = null;
		Assembly assembly;
		if (OperatingSystem.IsAndroid())
		{
			assembly = LoadAndroidAssembly(text, fullPath, probingRoots);
		}
		else
		{
			modLoadContext = new ModLoadContext(fullPath, probingRoots, flag);
			try
			{
				assembly = modLoadContext.LoadAssemblyWithoutLock(fullPath);
			}
			catch
			{
				modLoadContext.Unload();
				throw;
			}
		}
		StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = (staged ? new StateMachineCallbackRegistrationResult(success: true) : StateMachineCallbackRegistry.Shared.RegisterAssembly(text, assembly));
		if (!stateMachineCallbackRegistrationResult.Success)
		{
			ReleaseFailedContext(text, modLoadContext, flag);
			throw new InvalidOperationException("Mod '" + text + "' state-machine callbacks are invalid: " + stateMachineCallbackRegistrationResult.Error);
		}
		LoadedModAssembly loadedModAssembly = new LoadedModAssembly
		{
			ModId = text,
			AssemblyPath = fullPath,
			Assembly = assembly,
			LoadContext = modLoadContext,
			StateMachineCallbackCount = stateMachineCallbackRegistrationResult.CallbackCount,
			CallbacksRegistered = !staged,
			IsStaged = staged,
			SupportsCollectibleUnload = flag
		};
		_loaded[text] = loadedModAssembly;
		try
		{
			if (registerCharacterComponentRuntimes)
			{
				loadedModAssembly.ComponentTypesRegistered = true;
				loadedModAssembly.CharacterComponentRuntimeCount = CharacterComponentRuntimeTypeRegistry.RegisterAssembly(text, loadedModAssembly.Assembly);
			}
		}
		catch
		{
			CharacterComponentRuntimeTypeRegistry.UnregisterOwner(text);
			_loaded.Remove(text);
			StateMachineCallbackRegistry.Shared.TryUnloadOwner(text, out var _);
			ReleaseFailedContext(text, modLoadContext, flag);
			throw;
		}
		XWModEnvironmentService.AssemblyChanged(text, loaded: true);
		return loadedModAssembly;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Trusted Android Mod assemblies and their declared private dependencies are loaded at runtime by design.")]
	private static Assembly LoadAndroidAssembly(string modId, string assemblyPath, IEnumerable<string> probingRoots)
	{
		lock (AndroidAssemblyLoadLock)
		{
			foreach (string item in EnumerateAndroidDependencies(assemblyPath, probingRoots))
			{
				LoadAndroidAssemblyFile(modId, item, isMainAssembly: false);
			}
			return LoadAndroidAssemblyFile(modId, assemblyPath, isMainAssembly: true);
		}
	}

	private static IEnumerable<string> EnumerateAndroidDependencies(string assemblyPath, IEnumerable<string> probingRoots)
	{
		if (probingRoots == null)
		{
			yield break;
		}
		string fullPath = Path.GetFullPath(assemblyPath);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string>();
		foreach (string probingRoot in probingRoots)
		{
			if (string.IsNullOrWhiteSpace(probingRoot))
			{
				continue;
			}
			string fullPath2 = Path.GetFullPath(probingRoot);
			if (!Directory.Exists(fullPath2) || !hashSet.Add(fullPath2))
			{
				continue;
			}
			foreach (string item in Directory.EnumerateFiles(fullPath2, "*.dll", SearchOption.TopDirectoryOnly))
			{
				string fullPath3 = Path.GetFullPath(item);
				if (!string.Equals(fullPath3, fullPath, StringComparison.Ordinal))
				{
					list.Add(fullPath3);
				}
			}
		}
		list.Sort(StringComparer.Ordinal);
		foreach (string item2 in list)
		{
			yield return item2;
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Trusted Android Mod assemblies are loaded into the default runtime context by design.")]
	private static Assembly LoadAndroidAssemblyFile(string modId, string assemblyPath, bool isMainAssembly)
	{
		byte[] array = File.ReadAllBytes(assemblyPath);
		AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
		string name = assemblyName.Name;
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new InvalidOperationException("Android Mod assembly has no simple name: " + assemblyPath);
		}
		string text = Convert.ToHexString(SHA256.HashData(array));
		if (AndroidLoadedAssemblies.TryGetValue(name, out var value))
		{
			bool flag = AssemblyName.ReferenceMatchesDefinition(value.Identity, assemblyName);
			bool flag2 = string.Equals(value.Sha256, text, StringComparison.Ordinal);
			if (!isMainAssembly && !value.IsMainAssembly && flag && flag2)
			{
				return value.Assembly;
			}
			throw BuildAndroidAssemblyConflict(modId, assemblyName, value.OwnerId, value.Identity);
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			AssemblyName name2 = assemblies[i].GetName();
			if (string.Equals(name2.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				throw BuildAndroidAssemblyConflict(modId, assemblyName, "game or another runtime component", name2);
			}
		}
		using MemoryStream assembly = new MemoryStream(array, writable: false);
		Assembly assembly2 = AssemblyLoadContext.Default.LoadFromStream(assembly);
		AndroidLoadedAssemblies.Add(name, new AndroidLoadedAssembly
		{
			Assembly = assembly2,
			Identity = assemblyName,
			Sha256 = text,
			OwnerId = modId,
			IsMainAssembly = isMainAssembly
		});
		return assembly2;
	}

	private static InvalidOperationException BuildAndroidAssemblyConflict(string modId, AssemblyName requested, string existingOwner, AssemblyName existing)
	{
		return new InvalidOperationException($"Android Mod '{modId}' cannot load assembly '{requested.FullName}' because '{existing.FullName}' is already loaded by {existingOwner}. Android Mod " + "assemblies share one non-collectible context, so main assembly names must be unique. Private dependencies may share a name across Mods only when their identity and bytes are identical, and may not shadow game assemblies.");
	}

	public int RegisterCharacterComponentRuntimeTypes(string modId)
	{
		if (!TryNormalizeExternalModId(modId, out var normalizedModId) || !_loaded.TryGetValue(normalizedModId, out var value) || value.RegistrationsDetached)
		{
			return 0;
		}
		value.ComponentTypesRegistered = true;
		return value.CharacterComponentRuntimeCount = CharacterComponentRuntimeTypeRegistry.RegisterAssembly(normalizedModId, value.Assembly);
	}

	internal StateMachineCallbackRegistrationResult RegisterStateMachineCallbacks(string modId)
	{
		LoadedModAssembly loadedModAssembly = _loaded[modId];
		if (loadedModAssembly.RegistrationsDetached)
		{
			return new StateMachineCallbackRegistrationResult(success: false, "程序集已注销，不能重新发布回调。");
		}
		StateMachineCallbackRegistrationResult result = StateMachineCallbackRegistry.Shared.RegisterAssembly(modId, loadedModAssembly.Assembly);
		if (result.Success)
		{
			loadedModAssembly.CallbacksRegistered = true;
			loadedModAssembly.SetCallbackCount(result.CallbackCount);
		}
		return result;
	}

	public bool UnloadMod(string modId)
	{
		StateMachineUnloadBlockers blockers;
		return UnloadMod(modId, out blockers);
	}

	public bool CanUnloadMod(string modId, out StateMachineUnloadBlockers blockers)
	{
		blockers = new StateMachineUnloadBlockers(0, Array.Empty<StateMachineUnloadBlocker>());
		if (!TryNormalizeExternalModId(modId, out var normalizedModId) || !_loaded.ContainsKey(normalizedModId))
		{
			return true;
		}
		StateMachineCallbackRegistry.Shared.CanUnloadOwner(normalizedModId, out blockers);
		StateMachineUnloadBlockers unloadBlockers = CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers(normalizedModId);
		blockers = new StateMachineUnloadBlockers(blockers.LeaseCount + unloadBlockers.LeaseCount, blockers.Entries.Concat(unloadBlockers.Entries).ToArray());
		return blockers.LeaseCount == 0;
	}

	public bool RequiresRestart(string modId)
	{
		if (TryNormalizeExternalModId(modId, out var normalizedModId))
		{
			return RestartRequired.Contains(normalizedModId);
		}
		return false;
	}

	public bool UnloadMod(string modId, out StateMachineUnloadBlockers blockers)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			blockers = new StateMachineUnloadBlockers(0, Array.Empty<StateMachineUnloadBlocker>());
			return false;
		}
		LoadedModAssembly detached;
		string diagnostic;
		return TryDetachMod(modId, out detached, out blockers) && TryRequestUnload(detached, out diagnostic);
	}

	internal bool TryDetachMod(string modId, out LoadedModAssembly detached, out StateMachineUnloadBlockers blockers)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		detached = null;
		blockers = new StateMachineUnloadBlockers(0, Array.Empty<StateMachineUnloadBlocker>());
		if (environmentMutation == null)
		{
			return false;
		}
		if (!TryNormalizeExternalModId(modId, out var normalizedModId) || !_loaded.TryGetValue(normalizedModId, out var value))
		{
			return false;
		}
		if (value.RegistrationsDetached)
		{
			detached = value;
			return true;
		}
		if (!CharacterComponentRuntimeTypeRegistry.TryReserveUnload(normalizedModId, out var reservation))
		{
			CanUnloadMod(normalizedModId, out blockers);
			return false;
		}
		using (reservation)
		{
			if (value.CallbacksRegistered && !StateMachineCallbackRegistry.Shared.TryUnloadOwner(normalizedModId, out blockers))
			{
				StateMachineUnloadBlockers obj = blockers;
				if (obj != null && obj.LeaseCount > 0)
				{
					return false;
				}
			}
			if (!value.IsStaged)
			{
				XWModRuntimeRegistry.UnregisterOwner(normalizedModId);
			}
			if (value.ComponentTypesRegistered)
			{
				CharacterComponentRuntimeTypeRegistry.UnregisterOwner(normalizedModId);
			}
			value.RegistrationsDetached = true;
			detached = value;
			XWModEnvironmentService.AssemblyChanged(normalizedModId, loaded: false);
			return true;
		}
	}

	internal bool TryRequestUnload(LoadedModAssembly detached, out string diagnostic)
	{
		diagnostic = "";
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		if (environmentMutation == null)
		{
			diagnostic = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。";
			return false;
		}
		if (detached == null || !detached.RegistrationsDetached || detached.UnloadRequestInProgress)
		{
			diagnostic = "程序集尚未注销或正在卸载，不能重复请求。";
			return false;
		}
		try
		{
			detached.UnloadRequestInProgress = true;
			if (detached.SupportsCollectibleUnload)
			{
				detached.LoadContext.Unload();
			}
			else
			{
				RestartRequired.Add(detached.ModId);
			}
			_loaded.Remove(detached.ModId);
			detached.LastUnloadDiagnostic = "";
			return true;
		}
		catch (Exception ex)
		{
			string text = (detached.LastUnloadDiagnostic = ex.GetBaseException().Message);
			diagnostic = text;
			return false;
		}
		finally
		{
			detached.UnloadRequestInProgress = false;
		}
	}

	public void UnloadAll()
	{
		foreach (string item in new List<string>(_loaded.Keys))
		{
			UnloadMod(item);
		}
	}

	private static void ReleaseFailedContext(string modId, AssemblyLoadContext context, bool supportsCollectibleUnload)
	{
		if (supportsCollectibleUnload)
		{
			context?.Unload();
		}
		else
		{
			RestartRequired.Add(modId);
		}
	}

	public List<Type> FindTypesAssignableTo(string modId, Type baseType)
	{
		List<Type> list = new List<Type>();
		if (baseType == null || !TryNormalizeExternalModId(modId, out var normalizedModId) || !_loaded.TryGetValue(normalizedModId, out var value))
		{
			return list;
		}
		foreach (Type item in GetTypesSafe(value.Assembly))
		{
			if (!(item == null) && !item.IsAbstract && baseType.IsAssignableFrom(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private static string NormalizeExternalModIdOrThrow(string modId)
	{
		if (!StateMachineCallbackKey.TryNormalizeOwnerId(modId, out var normalized))
		{
			throw new ArgumentException("Mod id is invalid. Use a canonical owner id without path separators or control characters.", "modId");
		}
		if (string.Equals(normalized, "builtin", StringComparison.Ordinal))
		{
			throw new ArgumentException("Mod id 'builtin' is reserved for game-owned callbacks.", "modId");
		}
		return normalized;
	}

	private static bool TryNormalizeExternalModId(string modId, out string normalizedModId)
	{
		if (StateMachineCallbackKey.TryNormalizeOwnerId(modId, out normalizedModId))
		{
			return !string.Equals(normalizedModId, "builtin", StringComparison.Ordinal);
		}
		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Mod type discovery must inspect runtime-loaded assemblies.")]
	private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			List<Type> list = new List<Type>();
			Type[] types = ex.Types;
			foreach (Type type in types)
			{
				if (type != null)
				{
					list.Add(type);
				}
			}
			return list;
		}
	}
}
