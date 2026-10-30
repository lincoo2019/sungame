using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Godot;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModRuntimeCompatibilityTest : Node
{
	[Export(PropertyHint.None, "")]
	public string AssemblyPath { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public string RuntimeProfile { get; set; } = "unknown";

	[Export(PropertyHint.None, "")]
	public int Iterations { get; set; } = 30;

	public override void _Ready()
	{
		ApplyCommandLineArguments();
		Callable.From(RunProbe).CallDeferred();
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This benchmark intentionally probes runtime-loaded Mod reflection.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The dynamically loaded fixture has no statically analyzable members.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The dynamically loaded fixture has no statically analyzable members.")]
	private void RunProbe()
	{
		if (string.IsNullOrWhiteSpace(AssemblyPath))
		{
			GD.PrintErr("[ModRuntimeCompatibilityTest] Fixture assembly was not found: " + AssemblyPath);
			GetTree().Quit(2);
			return;
		}
		string fullPath = Path.GetFullPath(AssemblyPath);
		if (!File.Exists(fullPath))
		{
			GD.PrintErr("[ModRuntimeCompatibilityTest] Fixture assembly was not found: " + fullPath);
			GetTree().Quit(2);
			return;
		}
		bool flag = !string.Equals(RuntimeProfile, "aot", StringComparison.OrdinalIgnoreCase);
		XWModAssemblyLoader xWModAssemblyLoader = new XWModAssemblyLoader();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		long timestamp = Stopwatch.GetTimestamp();
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		Exception ex = null;
		bool flag2 = false;
		try
		{
			for (int i = 0; i < Iterations; i++)
			{
				string modId = $"dynamic-mod-probe-{i}";
				xWModAssemblyLoader.LoadModAssembly(modId, fullPath, new string[1] { fullPath.GetBaseDir() });
				num4++;
				Type type = xWModAssemblyLoader.FindTypesAssignableTo(modId, typeof(object)).Find((Type type2) => type2.FullName == "DynamicModProbe.EntryPoint");
				if (type == null)
				{
					throw new TypeLoadException("DynamicModProbe.EntryPoint was not discovered.");
				}
				object obj = Activator.CreateInstance(type) ?? throw new MissingMethodException(type.FullName, ".ctor()");
				object obj2 = (type.GetMethod("Run", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null) ?? throw new MissingMethodException(type.FullName, "Run()")).Invoke(obj, null);
				if (!(obj2 is int num8) || num8 != 42)
				{
					throw new InvalidOperationException($"Unexpected Mod probe result: {obj2 ?? "<null>"}.");
				}
				num7 = num8;
				num5++;
				if (!xWModAssemblyLoader.UnloadMod(modId))
				{
					throw new InvalidOperationException($"Mod probe unload failed for iteration {i}.");
				}
				num6++;
			}
			flag2 = true;
		}
		catch (Exception exception)
		{
			ex = UnwrapException(exception);
			GD.PrintErr($"[ModRuntimeCompatibilityTest] profile={RuntimeProfile} failure={ex.GetType().Name}: {ex.Message}");
		}
		finally
		{
			xWModAssemblyLoader.UnloadAll();
		}
		double value = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		long value2 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		long value3 = GC.GetTotalAllocatedBytes() - totalAllocatedBytes;
		int value4 = GC.CollectionCount(0) - num;
		int value5 = GC.CollectionCount(1) - num2;
		int value6 = GC.CollectionCount(2) - num3;
		bool flag3 = !flag && !flag2 && IsDynamicLoadingUnsupported(ex);
		bool flag4 = ((!flag) ? flag3 : (flag2 && num4 == Iterations && num5 == Iterations && num6 == Iterations && num7 == 42 && xWModAssemblyLoader.LoadedAssemblies.Count == 0));
		string value7 = ex?.GetType().Name ?? "none";
		GD.Print($"[ModRuntimeCompatibilityResult] runtimeProfile={RuntimeProfile} expectedSupported={FormatBool(flag)} supported={FormatBool(flag2)} iterations={Iterations} loads={num4} invokes={num5} unloads={num6} result={num7} failureType={value7} elapsedMs={value:F3} threadAllocatedBytes={value2} totalAllocatedBytes={value3} gen0={value4} gen1={value5} gen2={value6} passed={FormatBool(flag4)}");
		GetTree().Quit((!flag4) ? 1 : 0);
	}

	private static Exception UnwrapException(Exception exception)
	{
		while (exception is TargetInvocationException && exception.InnerException != null)
		{
			exception = exception.InnerException;
		}
		return exception;
	}

	private static bool IsDynamicLoadingUnsupported(Exception exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex is PlatformNotSupportedException || ex is NotSupportedException)
			{
				return true;
			}
		}
		return false;
	}

	private static string FormatBool(bool value)
	{
		if (!value)
		{
			return "false";
		}
		return "true";
	}

	private void ApplyCommandLineArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			int value;
			if (text.StartsWith("--mod-probe-assembly=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--mod-probe-assembly=".Length;
				AssemblyPath = text2.Substring(length, text2.Length - length).Trim();
			}
			else if (text.StartsWith("--runtime-profile=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--runtime-profile=".Length;
				RuntimeProfile = text2.Substring(length, text2.Length - length).Trim();
			}
			else if (TryReadInt(text, "--mod-probe-iterations=", out value))
			{
				Iterations = Math.Max(1, value);
			}
		}
	}

	private static bool TryReadInt(string arg, string prefix, out int value)
	{
		value = 0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return int.TryParse(arg.Substring(length, arg.Length - length), out value);
		}
		return false;
	}
}
