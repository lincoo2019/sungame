using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

public static class StartupLoadDiagnostics
{
	private static readonly string Mode = System.Environment.GetEnvironmentVariable("PVZ_STARTUP_DETAIL") ?? string.Empty;

	private static readonly List<(string Name, long Ticks)> Marks = new List<(string, long)>();

	private static readonly List<Resource> RetainedDependencies = new List<Resource>();

	private static readonly List<(string Path, string Type, double Milliseconds)> DependencyLoads = new List<(string, string, double)>();

	private static SceneTree _observedTree;

	private static double _dependencyDiscoveryMilliseconds;

	private static int _cachedDependencyCount;

	[ModuleInitializer]
	internal static void RecordAssemblyEntry()
	{
		Mark("managed.assembly_entry");
	}

	public static void Mark(string name)
	{
		if (Mode.Length > 0)
		{
			Marks.Add((name, Stopwatch.GetTimestamp()));
		}
	}

	public static void ObserveAutoloadTree()
	{
		if (Mode.Length != 0 && _observedTree == null)
		{
			_observedTree = (Engine.GetMainLoop() as SceneTree) ?? throw new InvalidOperationException("启动诊断没有找到正式场景树。");
			_observedTree.NodeAdded += OnNodeAdded;
			RenderingServer.FramePostDraw += OnFirstFramePostDraw;
		}
	}

	private static void OnFirstFramePostDraw()
	{
		Mark("render.first_frame_post_draw");
		RenderingServer.FramePostDraw -= OnFirstFramePostDraw;
	}

	private static void OnNodeAdded(Node node)
	{
		if (node.GetParent() == _observedTree.Root)
		{
			string name = node.Name.ToString();
			Mark("tree.entered/" + name);
			node.Ready += delegate
			{
				Mark("tree.ready/" + name);
			};
		}
	}

	public static void StopObservingTree()
	{
		if (_observedTree != null)
		{
			_observedTree.NodeAdded -= OnNodeAdded;
			_observedTree = null;
		}
	}

	public static void MeasureCharacterDependencies(IReadOnlyList<string> roots)
	{
		if (Mode != "dependencies")
		{
			return;
		}
		Mark("dependencies.discovery.begin");
		long timestamp = Stopwatch.GetTimestamp();
		HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		foreach (string root in roots)
		{
			CollectDependencies(root, visited, active, list);
		}
		_dependencyDiscoveryMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
		Mark("dependencies.discovery.end");
		Mark("dependencies.loading.begin");
		foreach (string item in list)
		{
			long timestamp2 = Stopwatch.GetTimestamp();
			Resource resource = ResourceLoader.Load(item, "", ResourceLoader.CacheMode.Reuse);
			double totalMilliseconds = Stopwatch.GetElapsedTime(timestamp2).TotalMilliseconds;
			if (!GodotObject.IsInstanceValid(resource))
			{
				throw new InvalidOperationException("依赖归因试验无法读取资源：" + item);
			}
			RetainedDependencies.Add(resource);
			DependencyLoads.Add((item, resource.GetType().Name, totalMilliseconds));
		}
		Mark("dependencies.loading.end");
	}

	private static void CollectDependencies(string path, HashSet<string> visited, HashSet<string> active, List<string> ordered)
	{
		if (active.Contains(path))
		{
			throw new InvalidOperationException("依赖归因试验遇到循环：" + path);
		}
		if (!visited.Add(path))
		{
			return;
		}
		if (ResourceLoader.HasCached(path))
		{
			_cachedDependencyCount++;
			return;
		}
		active.Add(path);
		string[] dependencies = ResourceLoader.GetDependencies(path);
		foreach (string text in dependencies)
		{
			int num = text.LastIndexOf("::", StringComparison.Ordinal);
			string path2;
			if (num >= 0)
			{
				string text2 = text;
				int num2 = num + 2;
				path2 = text2.Substring(num2, text2.Length - num2);
			}
			else
			{
				path2 = text;
			}
			string text3 = ProjectResourceUidCache.ResolveResourcePath(path2).Replace('\\', '/');
			if (!text3.StartsWith("res://", StringComparison.Ordinal))
			{
				throw new InvalidOperationException("依赖归因试验遇到非项目路径：" + text);
			}
			CollectDependencies(text3, visited, active, ordered);
		}
		active.Remove(path);
		ordered.Add(path);
	}

	public static Dictionary CreateReport(double processClockOffsetMs)
	{
		ulong ticksUsec = Time.GetTicksUsec();
		long timestamp = Stopwatch.GetTimestamp();
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (var (text, num) in Marks)
		{
			array.Add(new Dictionary
			{
				["name"] = text,
				["processMilliseconds"] = processClockOffsetMs + (double)ticksUsec / 1000.0 + (double)(num - timestamp) * 1000.0 / (double)Stopwatch.Frequency
			});
		}
		Array<Dictionary> array2 = new Array<Dictionary>();
		foreach (var (text2, text3, num2) in DependencyLoads)
		{
			array2.Add(new Dictionary
			{
				["path"] = text2,
				["type"] = text3,
				["milliseconds"] = num2
			});
		}
		return new Dictionary
		{
			["mode"] = Mode,
			["changesResourceLoadOrder"] = Mode == "dependencies",
			["marks"] = array,
			["dependencyDiscoveryMilliseconds"] = _dependencyDiscoveryMilliseconds,
			["cachedDependencyCount"] = _cachedDependencyCount,
			["dependencyLoads"] = array2
		};
	}
}
