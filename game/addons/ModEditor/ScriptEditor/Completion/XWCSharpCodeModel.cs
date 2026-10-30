using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PVZHE.ModEditor.ScriptEditor;

internal static class XWCSharpCodeModel
{
	private sealed class AssemblyIndexData
	{
		public readonly HashSet<string> Namespaces = new HashSet<string>(StringComparer.Ordinal);

		public readonly Dictionary<string, SortedSet<string>> TypeNamespaces = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

		public readonly Dictionary<string, List<Type>> TypeLookup = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
	}

	private static readonly object CacheLock = new object();

	private static readonly HashSet<string> NamespaceCache = new HashSet<string>(StringComparer.Ordinal);

	private static readonly Dictionary<string, SortedSet<string>> TypeNamespaces = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

	private static readonly Dictionary<string, List<Type>> TypeLookup = new Dictionary<string, List<Type>>(StringComparer.Ordinal);

	private static bool _baselineInitialized;

	private static bool _assemblyIndexStarted;

	private static readonly string[] CommonNamespaces = new string[24]
	{
		"System", "System.Buffers", "System.Collections", "System.Collections.Concurrent", "System.Collections.Generic", "System.Collections.ObjectModel", "System.ComponentModel", "System.Diagnostics", "System.Globalization", "System.IO",
		"System.Linq", "System.Numerics", "System.Reflection", "System.Runtime.CompilerServices", "System.Runtime.InteropServices", "System.Text", "System.Text.Json", "System.Text.RegularExpressions", "System.Threading", "System.Threading.Tasks",
		"Godot", "Godot.Collections", "PVZHE", "PVZHE.ModEditor"
	};

	private static readonly (string Name, string Namespace)[] CommonTypes = new(string, string)[39]
	{
		("Action", "System"),
		("Func", "System"),
		("Math", "System"),
		("Random", "System"),
		("String", "System"),
		("DateTime", "System"),
		("TimeSpan", "System"),
		("List", "System.Collections.Generic"),
		("Dictionary", "System.Collections.Generic"),
		("HashSet", "System.Collections.Generic"),
		("Queue", "System.Collections.Generic"),
		("Stack", "System.Collections.Generic"),
		("IEnumerable", "System.Collections.Generic"),
		("IReadOnlyList", "System.Collections.Generic"),
		("IReadOnlyDictionary", "System.Collections.Generic"),
		("File", "System.IO"),
		("Directory", "System.IO"),
		("Path", "System.IO"),
		("Regex", "System.Text.RegularExpressions"),
		("StringBuilder", "System.Text"),
		("JsonSerializer", "System.Text.Json"),
		("Task", "System.Threading.Tasks"),
		("CancellationToken", "System.Threading"),
		("Node", "Godot"),
		("Node2D", "Godot"),
		("Control", "Godot"),
		("Resource", "Godot"),
		("Texture2D", "Godot"),
		("PackedScene", "Godot"),
		("Vector2", "Godot"),
		("Vector2I", "Godot"),
		("Vector3", "Godot"),
		("Color", "Godot"),
		("StringName", "Godot"),
		("NodePath", "Godot"),
		("Callable", "Godot"),
		("Variant", "Godot"),
		("Array", "Godot.Collections"),
		("Dictionary", "Godot.Collections")
	};

	public static List<string> GetNamespaces(string source = "")
	{
		EnsureCache();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		lock (CacheLock)
		{
			foreach (string item in NamespaceCache)
			{
				hashSet.Add(item);
			}
		}
		foreach (string @namespace in XWCSharpProjectIndex.GetSnapshot().Namespaces)
		{
			hashSet.Add(@namespace);
		}
		AddSourceSymbols(source ?? "", hashSet, null);
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	public static List<string> FindNamespacesByType(string typeName, string source = "")
	{
		string text = NormalizeSimpleTypeName(typeName);
		if (string.IsNullOrEmpty(text))
		{
			return new List<string>();
		}
		EnsureCache();
		SortedSet<string> sortedSet = new SortedSet<string>(StringComparer.Ordinal);
		lock (CacheLock)
		{
			if (TypeNamespaces.TryGetValue(text, out var value))
			{
				foreach (string item2 in value)
				{
					sortedSet.Add(item2);
				}
			}
		}
		if (XWCSharpProjectIndex.GetSnapshot().TypeNamespaces.TryGetValue(text, out var value2))
		{
			string[] array = value2;
			foreach (string item in array)
			{
				sortedSet.Add(item);
			}
		}
		Dictionary<string, SortedSet<string>> dictionary = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
		AddSourceSymbols(source ?? "", null, dictionary);
		if (dictionary.TryGetValue(text, out var value3))
		{
			foreach (string item3 in value3)
			{
				sortedSet.Add(item3);
			}
		}
		return new List<string>(sortedSet);
	}

	public static List<string> GetKnownTypeNames(string source = "")
	{
		EnsureCache();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		lock (CacheLock)
		{
			foreach (string key in TypeNamespaces.Keys)
			{
				hashSet.Add(key);
			}
		}
		foreach (string key2 in XWCSharpProjectIndex.GetSnapshot().TypeNamespaces.Keys)
		{
			hashSet.Add(key2);
		}
		Dictionary<string, SortedSet<string>> dictionary = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
		AddSourceSymbols(source ?? "", null, dictionary);
		foreach (string key3 in dictionary.Keys)
		{
			hashSet.Add(key3);
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.Ordinal);
		return list;
	}

	public static List<Type> FindTypes(string typeName, HashSet<string> usingNamespaces = null, string currentNamespace = "")
	{
		string text = NormalizeSimpleTypeName(typeName);
		if (string.IsNullOrEmpty(text))
		{
			return new List<Type>();
		}
		EnsureCache();
		List<Type> list = new List<Type>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		lock (CacheLock)
		{
			if (TypeLookup.TryGetValue(typeName ?? "", out var value))
			{
				AddTypes(list, seen, value);
			}
			if (TypeLookup.TryGetValue(text, out var value2))
			{
				AddTypes(list, seen, value2);
			}
		}
		list.Sort(delegate(Type a, Type b)
		{
			int num = GetTypeRank(a, usingNamespaces, currentNamespace).CompareTo(GetTypeRank(b, usingNamespaces, currentNamespace));
			return (num == 0) ? string.Compare(a.FullName, b.FullName, StringComparison.Ordinal) : num;
		});
		return list;
	}

	public static HashSet<string> ExtractUsingNamespaces(string source)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (Match item in Regex.Matches(source ?? "", "(?m)^\\s*using\\s+(?!static\\b)(?:[A-Za-z_][A-Za-z0-9_]*\\s*=\\s*)?(?<ns>[A-Za-z_][A-Za-z0-9_.]*)\\s*;"))
		{
			string value = item.Groups["ns"].Value;
			if (!string.IsNullOrEmpty(value))
			{
				hashSet.Add(value);
			}
		}
		return hashSet;
	}

	public static string ExtractCurrentNamespace(string source)
	{
		string result = "";
		foreach (Match item in Regex.Matches(source ?? "", "(?m)^\\s*namespace\\s+(?<ns>[A-Za-z_][A-Za-z0-9_.]*)\\s*(?:[;{]|$)"))
		{
			result = item.Groups["ns"].Value;
		}
		return result;
	}

	public static bool IsBuiltInTypeAlias(string typeName)
	{
		switch (NormalizeSimpleTypeName(typeName))
		{
		case "bool":
		case "byte":
		case "char":
		case "uint":
		case "long":
		case "void":
		case "short":
		case "sbyte":
		case "float":
		case "ulong":
		case "decimal":
		case "dynamic":
		case "string":
		case "ushort":
		case "double":
		case "object":
		case "int":
		case "var":
			return true;
		default:
			return false;
		}
	}

	public static string NormalizeSimpleTypeName(string typeName)
	{
		string text = (typeName ?? "").Trim();
		if (text.Length == 0)
		{
			return "";
		}
		text = text.Replace("global::", "");
		int num = text.IndexOf('<');
		if (num >= 0)
		{
			text = text.Substring(0, num);
		}
		text = text.TrimEnd('?', '[', ']');
		int num2 = text.LastIndexOf('.');
		if (num2 >= 0)
		{
			text = text.Substring(num2 + 1);
		}
		return text;
	}

	public static bool IsNamespaceAlreadyVisible(string namespaceName, HashSet<string> usingNamespaces, string currentNamespace)
	{
		if (string.IsNullOrEmpty(namespaceName))
		{
			return true;
		}
		if (usingNamespaces != null && usingNamespaces.Contains(namespaceName))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(currentNamespace))
		{
			return string.Equals(namespaceName, currentNamespace, StringComparison.Ordinal);
		}
		return false;
	}

	private static void EnsureCache()
	{
		lock (CacheLock)
		{
			if (!_baselineInitialized)
			{
				string[] commonNamespaces = CommonNamespaces;
				for (int i = 0; i < commonNamespaces.Length; i++)
				{
					RegisterNamespace(commonNamespaces[i]);
				}
				(string, string)[] commonTypes = CommonTypes;
				for (int i = 0; i < commonTypes.Length; i++)
				{
					(string, string) tuple = commonTypes[i];
					RegisterType(tuple.Item1, tuple.Item2);
				}
				_baselineInitialized = true;
			}
			if (_assemblyIndexStarted)
			{
				return;
			}
			_assemblyIndexStarted = true;
			Task.Run((Func<AssemblyIndexData>)BuildAssemblySymbols).ContinueWith(delegate(Task<AssemblyIndexData> task)
			{
				if (task.Status == TaskStatus.RanToCompletion)
				{
					MergeAssemblySymbols(task.Result);
				}
			}, TaskScheduler.Default);
		}
	}

	public static Task RequestBackgroundWarmup(string projectRoot)
	{
		EnsureCache();
		return XWCSharpProjectIndex.RequestFullIndexAsync(projectRoot);
	}

	public static Task RequestSourceUpdate(string filePath, string source)
	{
		EnsureCache();
		return XWCSharpProjectIndex.RequestSourceUpdate(filePath, source);
	}

	internal static XWCSharpProjectIndex.IndexMetrics GetIndexMetrics()
	{
		return XWCSharpProjectIndex.GetMetrics();
	}

	internal static void ResetIndexForProbe()
	{
		XWCSharpProjectIndex.ResetForProbe();
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The in-game script editor intentionally indexes loaded editor/game assemblies for best-effort C# completion.")]
	private static AssemblyIndexData BuildAssemblySymbols()
	{
		AssemblyIndexData assemblyIndexData = new AssemblyIndexData();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.IsDynamic)
			{
				continue;
			}
			foreach (Type item in SafeGetTypes(assembly))
			{
				if (item == null || item.IsNested)
				{
					continue;
				}
				string text = StripGenericArity(item.Name);
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = item.Namespace ?? "";
					if (!string.IsNullOrWhiteSpace(text2))
					{
						assemblyIndexData.Namespaces.Add(text2);
					}
					AddType(assemblyIndexData.TypeNamespaces, text, text2);
					AddTypeLookup(assemblyIndexData.TypeLookup, text, item);
					if (!string.IsNullOrEmpty(item.FullName))
					{
						AddTypeLookup(assemblyIndexData.TypeLookup, StripGenericArity(item.FullName), item);
					}
				}
			}
		}
		return assemblyIndexData;
	}

	private static void MergeAssemblySymbols(AssemblyIndexData index)
	{
		lock (CacheLock)
		{
			foreach (string @namespace in index.Namespaces)
			{
				NamespaceCache.Add(@namespace);
			}
			foreach (KeyValuePair<string, SortedSet<string>> typeNamespace in index.TypeNamespaces)
			{
				foreach (string item in typeNamespace.Value)
				{
					AddType(TypeNamespaces, typeNamespace.Key, item);
				}
			}
			foreach (KeyValuePair<string, List<Type>> item2 in index.TypeLookup)
			{
				foreach (Type item3 in item2.Value)
				{
					AddTypeLookup(item2.Key, item3);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The in-game script editor intentionally indexes loaded editor/game assemblies for best-effort C# completion.")]
	private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
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
		catch
		{
			return Array.Empty<Type>();
		}
	}

	private static void AddSourceSymbols(string source, HashSet<string> namespaces, Dictionary<string, SortedSet<string>> typeNamespaces)
	{
		if (string.IsNullOrEmpty(source))
		{
			return;
		}
		string namespaceName = "";
		MatchCollection matchCollection = Regex.Matches(source, "(?m)^\\s*namespace\\s+(?<ns>[A-Za-z_][A-Za-z0-9_.]*)\\s*(?:[;{]|$)");
		foreach (Match item in matchCollection)
		{
			string value = item.Groups["ns"].Value;
			if (!string.IsNullOrEmpty(value))
			{
				namespaces?.Add(value);
			}
		}
		int i = 0;
		foreach (Match item2 in Regex.Matches(source, "(?m)^\\s*(?:\\[[^\\r\\n]+\\]\\s*)*(?:(?:public|private|protected|internal|static|abstract|sealed|partial|unsafe|new)\\s+)*(?:class|struct|interface|enum|record(?:\\s+(?:class|struct))?)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)"))
		{
			for (; i < matchCollection.Count && matchCollection[i].Index < item2.Index; i++)
			{
				namespaceName = matchCollection[i].Groups["ns"].Value;
			}
			string value2 = item2.Groups["name"].Value;
			if (!string.IsNullOrEmpty(value2))
			{
				AddType(typeNamespaces, value2, namespaceName);
			}
		}
	}

	private static void RegisterNamespace(string namespaceName)
	{
		if (!string.IsNullOrWhiteSpace(namespaceName))
		{
			NamespaceCache.Add(namespaceName.Trim());
		}
	}

	private static void RegisterType(string name, string namespaceName)
	{
		RegisterNamespace(namespaceName);
		AddType(TypeNamespaces, name, namespaceName);
	}

	private static void AddType(Dictionary<string, SortedSet<string>> target, string name, string namespaceName)
	{
		if (target != null && !string.IsNullOrWhiteSpace(name))
		{
			if (!target.TryGetValue(name, out var value))
			{
				value = (target[name] = new SortedSet<string>(StringComparer.Ordinal));
			}
			value.Add(namespaceName ?? "");
		}
	}

	private static void AddTypeLookup(string key, Type type)
	{
		AddTypeLookup(TypeLookup, key, type);
	}

	private static void AddTypeLookup(Dictionary<string, List<Type>> target, string key, Type type)
	{
		if (!string.IsNullOrWhiteSpace(key) && !(type == null))
		{
			if (!target.TryGetValue(key, out var value))
			{
				value = (target[key] = new List<Type>());
			}
			value.Add(type);
		}
	}

	private static void AddTypes(List<Type> target, HashSet<string> seen, IEnumerable<Type> source)
	{
		foreach (Type item2 in source)
		{
			string item = item2.AssemblyQualifiedName ?? item2.FullName ?? item2.Name;
			if (seen.Add(item))
			{
				target.Add(item2);
			}
		}
	}

	private static int GetTypeRank(Type type, HashSet<string> usingNamespaces, string currentNamespace)
	{
		string text = type.Namespace ?? "";
		if (!string.IsNullOrEmpty(currentNamespace) && text == currentNamespace)
		{
			return 0;
		}
		if (usingNamespaces != null && usingNamespaces.Contains(text))
		{
			return 1;
		}
		if (text == "Godot" || text.StartsWith("Godot.", StringComparison.Ordinal))
		{
			return 2;
		}
		if (text == "System" || text.StartsWith("System.", StringComparison.Ordinal))
		{
			return 3;
		}
		return 4;
	}

	private static string StripGenericArity(string typeName)
	{
		string text = typeName ?? "";
		int num = text.IndexOf('`');
		if (num < 0)
		{
			return text;
		}
		return text.Substring(0, num);
	}
}
