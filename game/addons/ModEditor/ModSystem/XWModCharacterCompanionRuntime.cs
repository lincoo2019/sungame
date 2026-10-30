using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModCharacterCompanionRuntime : IDisposable
{
	private sealed class RuntimeInstanceLease
	{
		public WeakReference<Node> Node { get; init; }

		public string HostType { get; init; } = "";
	}

	private const string BindingMetadata = "mod_character_script_binding";

	private const string ScriptPathMetadata = "mod_character_script_path";

	private const string CompanionOnly = "CompanionOnly";

	private readonly XWModAssemblyLoader _assemblyLoader = new XWModAssemblyLoader();

	private string _ownerId = "";

	private XWModAssemblyLoader.LoadedModAssembly _loadedAssembly;

	private IXWModRuntimeEntry _runtimeEntry;

	private bool _requiresRestart;

	private readonly System.Collections.Generic.Dictionary<ulong, RuntimeInstanceLease> _runtimeInstances = new System.Collections.Generic.Dictionary<ulong, RuntimeInstanceLease>();

	public bool IsLoaded => _loadedAssembly?.Assembly != null;

	public string OwnerId => _ownerId;

	public int StateMachineCallbackCount => _loadedAssembly?.StateMachineCallbackCount ?? 0;

	public bool RuntimeEntryInitialized => _runtimeEntry != null;

	public int CharacterComponentRuntimeCount => _loadedAssembly?.CharacterComponentRuntimeCount ?? 0;

	public bool RequiresRestart => _requiresRestart;

	public bool UnloadRequested { get; private set; }

	public WeakReference LastUnloadContext { get; private set; }

	public string LastLifecycleDiagnostic { get; private set; } = "";

	public StateMachineUnloadBlockers LastUnloadBlockers { get; private set; } = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());

	public void Load(string ownerId, string assemblyPath)
	{
		if (string.IsNullOrWhiteSpace(ownerId))
		{
			throw new ArgumentException("必须提供 Mod 所有者 ID。", "ownerId");
		}
		if (!TryUnload(out var blockers))
		{
			throw new InvalidOperationException(FormatUnloadBlockedDiagnostic(_ownerId, blockers));
		}
		string text = ownerId.Trim();
		XWModAssemblyLoader.LoadedModAssembly loadedAssembly = _assemblyLoader.LoadModAssembly(text, assemblyPath, new string[2]
		{
			Path.GetDirectoryName(assemblyPath) ?? "",
			Path.Combine(Path.GetDirectoryName(assemblyPath) ?? "", "Dependencies")
		});
		_ownerId = text;
		_loadedAssembly = loadedAssembly;
		_runtimeEntry = null;
		_requiresRestart = false;
		UnloadRequested = false;
		LastUnloadContext = null;
		LastLifecycleDiagnostic = "";
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The manifest explicitly selects a trusted managed Mod entry type.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The manifest-selected IXWModRuntimeEntry must expose its public parameterless constructor.")]
	public bool TryInitializeRuntimeEntry(XWModManifest manifest, string packageRoot, out string diagnostic)
	{
		diagnostic = "";
		if (manifest == null || string.IsNullOrWhiteSpace(manifest.RuntimeEntryType))
		{
			return true;
		}
		if (!IsLoaded)
		{
			diagnostic = "运行入口已声明，但 Mod 程序集没有加载。";
			return false;
		}
		if (manifest.RuntimeApiVersion != 1)
		{
			diagnostic = $"运行 API 版本不兼容：Mod={manifest.RuntimeApiVersion}，Host={1}。";
			return false;
		}
		Type type = GetTypesSafe(_loadedAssembly.Assembly).SingleOrDefault((Type type2) => type2 != null && !type2.IsAbstract && typeof(IXWModRuntimeEntry).IsAssignableFrom(type2) && string.Equals(type2.FullName, manifest.RuntimeEntryType.Trim(), StringComparison.Ordinal));
		if (type == null)
		{
			diagnostic = "找不到运行入口类型，或类型未实现 IXWModRuntimeEntry：" + manifest.RuntimeEntryType + "。";
			return false;
		}
		IXWModRuntimeEntry iXWModRuntimeEntry = null;
		try
		{
			iXWModRuntimeEntry = Activator.CreateInstance(type) as IXWModRuntimeEntry;
			if (iXWModRuntimeEntry == null)
			{
				throw new InvalidOperationException("运行入口必须提供公开无参构造函数。");
			}
			iXWModRuntimeEntry.Initialize(new XWModRuntimeContext(manifest, packageRoot));
			_runtimeEntry = iXWModRuntimeEntry;
			return true;
		}
		catch (Exception ex)
		{
			try
			{
				iXWModRuntimeEntry?.Shutdown();
			}
			catch
			{
			}
			diagnostic = ex.GetBaseException().Message;
			_runtimeEntry = null;
			return false;
		}
	}

	public bool NotifyAllModsLoaded(out string diagnostic)
	{
		diagnostic = "";
		if (_runtimeEntry == null)
		{
			return true;
		}
		try
		{
			_runtimeEntry.OnAllModsLoaded();
			return true;
		}
		catch (Exception ex)
		{
			diagnostic = ex.GetBaseException().Message;
			LastLifecycleDiagnostic = diagnostic;
			return false;
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Player-authored Mod types are selected from an explicitly declared collectible assembly.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The selected non-abstract Godot Node type is required to expose its generated public constructor.")]
	public bool TryCreateInstance(PackedScene authoredScene, out Node runtimeRoot, out string diagnostic)
	{
		runtimeRoot = null;
		diagnostic = "";
		if (!IsLoaded || !GodotObject.IsInstanceValid(authoredScene))
		{
			diagnostic = "伴随脚本运行时或角色场景不可用。";
			return false;
		}
		Node authoredRoot = null;
		try
		{
			authoredRoot = authoredScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(authoredRoot))
			{
				diagnostic = "无法实例化角色场景。";
				return false;
			}
			string a = (authoredRoot.HasMeta("mod_character_script_binding") ? authoredRoot.GetMeta("mod_character_script_binding").AsString() : "");
			string text = (authoredRoot.HasMeta("mod_character_script_path") ? authoredRoot.GetMeta("mod_character_script_path").AsString() : "");
			if (!string.Equals(a, "CompanionOnly", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(text))
			{
				diagnostic = "角色场景缺少 CompanionOnly 伴随脚本元数据。";
				return false;
			}
			string expectedTypeName = Path.GetFileNameWithoutExtension(text.Replace('\\', '/'));
			Type type = GetTypesSafe(_loadedAssembly.Assembly).SingleOrDefault((Type type2) => type2 != null && !type2.IsAbstract && string.Equals(type2.Name, expectedTypeName, StringComparison.Ordinal) && authoredRoot.GetType().IsAssignableFrom(type2));
			if (type == null)
			{
				diagnostic = "找不到声明的伴随脚本类型，或其基类不兼容：" + expectedTypeName + "。";
				return false;
			}
			if (!(Activator.CreateInstance(type) is Node node))
			{
				diagnostic = "声明的伴随脚本类型没有创建 Godot 节点：" + expectedTypeName + "。";
				return false;
			}
			runtimeRoot = node;
			runtimeRoot.Name = authoredRoot.Name;
			CopyStoredProperties(authoredRoot, runtimeRoot);
			CopyMetadata(authoredRoot, runtimeRoot);
			Node[] array = authoredRoot.GetChildren().Cast<Node>().ToArray();
			foreach (Node node2 in array)
			{
				Node[] array2 = (from node3 in EnumerateSubtree(node2)
					where node3.Owner == authoredRoot
					select node3).ToArray();
				Node[] array3 = array2;
				for (int num2 = 0; num2 < array3.Length; num2++)
				{
					array3[num2].Owner = null;
				}
				node2.Reparent(runtimeRoot, keepGlobalTransform: false);
				array3 = array2;
				for (int num2 = 0; num2 < array3.Length; num2++)
				{
					array3[num2].Owner = runtimeRoot;
				}
			}
			authoredRoot.Free();
			authoredRoot = null;
			_runtimeInstances[runtimeRoot.GetInstanceId()] = new RuntimeInstanceLease
			{
				Node = new WeakReference<Node>(runtimeRoot),
				HostType = (type.FullName ?? type.Name)
			};
			return true;
		}
		catch (Exception ex)
		{
			diagnostic = ex.GetBaseException().Message;
			if (GodotObject.IsInstanceValid(runtimeRoot))
			{
				runtimeRoot.Free();
			}
			runtimeRoot = null;
			return false;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(authoredRoot))
			{
				authoredRoot.Free();
			}
		}
	}

	public bool Unload()
	{
		StateMachineUnloadBlockers blockers;
		return TryUnload(out blockers);
	}

	internal void PrepareLifecycleRollback()
	{
		ShutdownRuntimeEntry();
	}

	public bool CanUnload(out StateMachineUnloadBlockers blockers)
	{
		blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
		bool flag = string.IsNullOrWhiteSpace(_ownerId) || _assemblyLoader.CanUnloadMod(_ownerId, out blockers);
		int leaseCount = blockers.LeaseCount;
		List<StateMachineUnloadBlocker> list = new List<StateMachineUnloadBlocker>(blockers.Entries);
		KeyValuePair<ulong, RuntimeInstanceLease>[] array = _runtimeInstances.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<ulong, RuntimeInstanceLease> keyValuePair = array[i];
			var (num2, runtimeInstanceLease2) = keyValuePair;
			if (!runtimeInstanceLease2.Node.TryGetTarget(out var target) || !GodotObject.IsInstanceValid(target))
			{
				_runtimeInstances.Remove(num2);
				continue;
			}
			list.Add(new StateMachineUnloadBlocker($"companion-instance:{num2}", runtimeInstanceLease2.HostType));
		}
		blockers = new StateMachineUnloadBlockers(leaseCount + _runtimeInstances.Count, list.ToArray());
		if (flag)
		{
			return _runtimeInstances.Count == 0;
		}
		return false;
	}

	public bool TryUnload(out StateMachineUnloadBlockers blockers)
	{
		using XWModEnvironmentService.EnvironmentMutation environmentMutation = XWModEnvironmentService.TryBeginEnvironmentMutation();
		blockers = new StateMachineUnloadBlockers(0, System.Array.Empty<StateMachineUnloadBlocker>());
		if (environmentMutation == null)
		{
			LastLifecycleDiagnostic = "Mod 环境正在确认或用于联机战斗，请取消确认或退出战斗后重试。";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_ownerId))
		{
			_loadedAssembly = null;
			LastUnloadBlockers = blockers;
			return true;
		}
		if (!CanUnload(out blockers))
		{
			LastUnloadBlockers = blockers;
			return false;
		}
		ShutdownRuntimeEntry();
		AssemblyLoadContext assemblyLoadContext = _loadedAssembly?.LoadContext;
		if (!_assemblyLoader.UnloadMod(_ownerId, out blockers))
		{
			LastUnloadBlockers = blockers;
			return false;
		}
		_requiresRestart = _assemblyLoader.RequiresRestart(_ownerId);
		UnloadRequested = true;
		LastUnloadContext = ((assemblyLoadContext == null) ? null : new WeakReference(assemblyLoadContext));
		_loadedAssembly = null;
		_ownerId = "";
		_runtimeInstances.Clear();
		LastUnloadBlockers = blockers;
		return true;
	}

	public void Dispose()
	{
		Unload();
	}

	private void ShutdownRuntimeEntry()
	{
		if (_runtimeEntry != null)
		{
			XWModEnvironmentService.Invalidate("RuntimeEntryShutdown");
			try
			{
				_runtimeEntry.Shutdown();
			}
			catch (Exception ex)
			{
				LastLifecycleDiagnostic = ex.GetBaseException().Message;
			}
			_runtimeEntry = null;
		}
	}

	internal static string FormatUnloadBlockedDiagnostic(string ownerId, StateMachineUnloadBlockers blockers)
	{
		int value = blockers?.LeaseCount ?? 0;
		string value2 = ((blockers?.Entries != null && blockers.Entries.Length != 0) ? string.Join(", ", blockers.Entries.Select((StateMachineUnloadBlocker entry) => entry.DefinitionId + " (" + entry.HostType + ")")) : "unknown active runtime");
		return $"Mod '{ownerId}' runtime assembly cannot unload while {value} runtime binding(s) or instance(s) are active: {value2}.";
	}

	private static void CopyStoredProperties(GodotObject source, GodotObject target)
	{
		HashSet<string> hashSet = (from entry in target.GetPropertyList()
			where entry.ContainsKey("name")
			select entry["name"].AsString()).ToHashSet<string>(StringComparer.Ordinal);
		foreach (Dictionary property in source.GetPropertyList())
		{
			if (!property.ContainsKey("name") || !property.ContainsKey("usage"))
			{
				continue;
			}
			string text = property["name"].AsString();
			bool flag = (property["usage"].AsInt64() & 2) == 0;
			if (!flag)
			{
				bool flag2 = ((text == "script" || text == "owner") ? true : false);
				flag = flag2;
			}
			if (!flag && hashSet.Contains(text))
			{
				try
				{
					target.Set(text, source.Get(text));
				}
				catch
				{
				}
			}
		}
	}

	private static void CopyMetadata(Node source, Node target)
	{
		foreach (StringName meta in source.GetMetaList())
		{
			target.SetMeta(meta, source.GetMeta(meta));
		}
	}

	private static IEnumerable<Node> EnumerateSubtree(Node root)
	{
		yield return root;
		foreach (Node child in root.GetChildren())
		{
			foreach (Node item in EnumerateSubtree(child))
			{
				yield return item;
			}
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Explicitly declared Mod assemblies are inspected at runtime by design.")]
	private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
	{
		try
		{
			return assembly?.GetTypes() ?? System.Array.Empty<Type>();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type type) => type != null);
		}
	}
}
