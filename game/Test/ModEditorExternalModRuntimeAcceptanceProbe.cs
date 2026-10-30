using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorExternalModRuntimeAcceptanceProbe : Node
{
	private readonly record struct RuntimeAcceptanceLoadResult(bool BuiltinRejected, bool OwnerNormalized, bool Load, bool Instantiate, bool Invoke, bool UnloadRequested, WeakReference ContextReference);

	private const string EntryAssemblyName = "ExternalModRuntimeAcceptance";

	private const string DependencyAssemblyName = "ExternalModRuntimeDependency";

	private const string EntryTypeName = "ExternalModRuntimeAcceptance.EntryPoint";

	private const string ContractMethodName = "Invoke";

	private const string ExpectedContractResult = "依赖已解析：外部 Mod 入口已执行";

	private static readonly System.Threading.Mutex BuildMutex = new System.Threading.Mutex(initiallyOwned: false, "PVZHE_ModEditor_ExternalModRuntimeAcceptance_Build");

	private readonly List<string> _failures = new List<string>();

	private string _tempRoot = "";

	public override async void _Ready()
	{
		bool f3 = false;
		bool builtinRejected = false;
		bool ownerNormalized = false;
		bool load = false;
		bool instantiate = false;
		bool invoke = false;
		bool unload = false;
		try
		{
			f3 = await OpenRealEditorAsync();
			Require(f3, "F3 did not open the ModEditor main surface.");
			if (!f3)
			{
				return;
			}
			_tempRoot = Path.Combine(Path.GetTempPath(), "pvzhe-external-mod-runtime-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempRoot);
			if (!BuildMutex.WaitOne(TimeSpan.FromSeconds(120L)))
			{
				Require(condition: false, "External Mod acceptance build lock timed out.");
				return;
			}
			try
			{
				string text = await BuildExternalModAsync(_tempRoot);
				Require(File.Exists(text), "Temporary external Mod entry assembly was not produced.");
				Require(File.Exists(Path.Combine(Path.GetDirectoryName(text) ?? "", "ExternalModRuntimeDependency.dll")), "Temporary external Mod private dependency was not produced.");
				if (File.Exists(text))
				{
					RuntimeAcceptanceLoadResult runtimeAcceptanceLoadResult = LoadInvokeAndRequestUnload(text);
					builtinRejected = runtimeAcceptanceLoadResult.BuiltinRejected;
					ownerNormalized = runtimeAcceptanceLoadResult.OwnerNormalized;
					load = runtimeAcceptanceLoadResult.Load;
					instantiate = runtimeAcceptanceLoadResult.Instantiate;
					invoke = runtimeAcceptanceLoadResult.Invoke;
					unload = runtimeAcceptanceLoadResult.UnloadRequested && CanRenameAssembly(text) && WaitForCollection(runtimeAcceptanceLoadResult.ContextReference) && !IsAssemblyStillLoaded("ExternalModRuntimeAcceptance") && !IsAssemblyStillLoaded("ExternalModRuntimeDependency");
					Require(builtinRejected, "The external Mod loader did not reject the reserved builtin owner before loading.");
					Require(ownerNormalized, "The external Mod loader did not use one normalized Unicode owner for lookup, registration, and unload.");
					Require(load, "External Mod assembly did not load in a collectible context.");
					Require(instantiate, "External Mod contract entry point was not instantiated.");
					Require(invoke, "External Mod contract entry point did not return its dependency result.");
					Require(unload, "External Mod context did not unload, still locked files, or leaked into the host AppDomain.");
				}
			}
			finally
			{
				BuildMutex.ReleaseMutex();
			}
		}
		catch (Exception ex)
		{
			Require(condition: false, ex.ToString());
		}
		finally
		{
			bool flag = CleanupTempRoot();
			Require(flag, "External Mod acceptance temporary directory was not cleaned.");
			foreach (string failure in _failures)
			{
				GD.PrintErr("[EXTERNAL_MOD_RUNTIME_ACCEPTANCE_FAILURE] " + failure);
			}
			GD.Print($"[EXTERNAL_MOD_RUNTIME_ACCEPTANCE] f3={f3} builtinRejected={builtinRejected} ownerNormalized={ownerNormalized} load={load} instantiate={instantiate} invoke={invoke} unload={unload} cleanup={flag} failures={_failures.Count}");
			GetTree().Quit((_failures.Count != 0) ? 1 : 0);
		}
	}

	private async Task<string> BuildExternalModAsync(string root)
	{
		XWDotNetToolchainLocator.ToolchainInfo toolchainInfo = XWDotNetToolchainLocator.Locate();
		if (!toolchainInfo.Exists)
		{
			throw new FileNotFoundException("No .NET toolchain is available for external Mod acceptance.", toolchainInfo.DotnetPath);
		}
		string text = Path.Combine(root, "PrivateDependency");
		string text2 = Path.Combine(root, "ExternalMod");
		string outputRoot = Path.Combine(root, "publish");
		Directory.CreateDirectory(text);
		Directory.CreateDirectory(text2);
		File.WriteAllText(Path.Combine(text, "ExternalModRuntimeDependency.csproj"), BuildProjectFile("ExternalModRuntimeDependency"));
		File.WriteAllText(Path.Combine(text, "ContractText.cs"), "namespace ExternalModRuntimeDependency;\npublic static class ContractText\n{\n    public static string Value => \"\\u4F9D\\u8D56\\u5DF2\\u89E3\\u6790\\uFF1A\\u5916\\u90E8 Mod \\u5165\\u53E3\\u5DF2\\u6267\\u884C\";\n}\n");
		File.WriteAllText(Path.Combine(text2, "ExternalModRuntimeAcceptance.csproj"), BuildProjectFile("ExternalModRuntimeAcceptance", "..\\PrivateDependency\\ExternalModRuntimeDependency.csproj"));
		File.WriteAllText(Path.Combine(text2, "EntryPoint.cs"), "using ExternalModRuntimeDependency;\nnamespace ExternalModRuntimeAcceptance;\npublic sealed class EntryPoint\n{\n    public string Invoke() => ContractText.Value;\n}\n");
		XWBuildProcessRunner.ProcessRunResult processRunResult = await XWBuildProcessRunner.RunAsync(new ProcessStartInfo
		{
			FileName = toolchainInfo.DotnetPath,
			WorkingDirectory = text2,
			ArgumentList = 
			{
				"build",
				Path.Combine(text2, "ExternalModRuntimeAcceptance.csproj"),
				"--nologo",
				"--configuration",
				"Release",
				"--output",
				outputRoot
			}
		}, TimeSpan.FromMinutes(2L), CancellationToken.None);
		if (processRunResult.ExitCode != 0 || processRunResult.Cancelled || processRunResult.TimedOut || !processRunResult.ProcessTreeTerminated)
		{
			throw new InvalidOperationException("External Mod temporary project build failed: " + processRunResult.StandardOutput + System.Environment.NewLine + processRunResult.StandardError);
		}
		return Path.Combine(outputRoot, "ExternalModRuntimeAcceptance.dll");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The acceptance probe intentionally resolves a player-style external Mod entry type by name.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The acceptance probe intentionally instantiates the external Mod contract's public parameterless entry point.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The acceptance probe intentionally invokes the external Mod contract's public method by name.")]
	private static RuntimeAcceptanceLoadResult LoadInvokeAndRequestUnload(string entryAssemblyPath)
	{
		XWModAssemblyLoader xWModAssemblyLoader = new XWModAssemblyLoader();
		bool builtinRejected = RejectBuiltinOwner(xWModAssemblyLoader, entryAssemblyPath);
		string text = "  测试-外部运行-AbC-" + Guid.NewGuid().ToString("N").ToUpperInvariant() + "  ";
		string text2 = text.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
		string modId = "  " + text2.ToUpperInvariant() + "  ";
		XWModAssemblyLoader.LoadedModAssembly loadedModAssembly = null;
		object obj = null;
		Type type = null;
		WeakReference contextReference = null;
		bool load = false;
		bool instantiate = false;
		bool invoke = false;
		bool flag = false;
		bool ownerNormalized = false;
		try
		{
			loadedModAssembly = xWModAssemblyLoader.LoadModAssembly(text, entryAssemblyPath, new string[1] { Path.GetDirectoryName(entryAssemblyPath) ?? "" }, registerCharacterComponentRuntimes: false);
			contextReference = new WeakReference(loadedModAssembly.LoadContext);
			ownerNormalized = string.Equals(loadedModAssembly.ModId, text2, StringComparison.Ordinal) && xWModAssemblyLoader.LoadedAssemblies.ContainsKey(text2) && xWModAssemblyLoader.FindTypesAssignableTo(modId, typeof(object)).Count > 0;
			load = loadedModAssembly.Assembly != null && (object)loadedModAssembly.Assembly != typeof(ModEditorExternalModRuntimeAcceptanceProbe).Assembly;
			type = loadedModAssembly.Assembly.GetType("ExternalModRuntimeAcceptance.EntryPoint", throwOnError: false);
			obj = ((type == null) ? null : Activator.CreateInstance(type));
			instantiate = obj != null;
			invoke = string.Equals((type?.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null))?.Invoke(obj, null) as string, "依赖已解析：外部 Mod 入口已执行", StringComparison.Ordinal);
		}
		finally
		{
			obj = null;
			type = null;
			loadedModAssembly = null;
			flag = xWModAssemblyLoader.UnloadMod(modId);
			xWModAssemblyLoader = null;
		}
		return new RuntimeAcceptanceLoadResult(builtinRejected, ownerNormalized, load, instantiate, invoke, flag, contextReference);
	}

	private static bool RejectBuiltinOwner(XWModAssemblyLoader loader, string entryAssemblyPath)
	{
		try
		{
			loader.LoadModAssembly("  BUILTIN  ", entryAssemblyPath, new string[1] { Path.GetDirectoryName(entryAssemblyPath) ?? "" }, registerCharacterComponentRuntimes: false);
			return false;
		}
		catch (ArgumentException ex)
		{
			return ex.ParamName == "modId" && ex.Message.Contains("reserved", StringComparison.OrdinalIgnoreCase) && loader.LoadedAssemblies.Count == 0;
		}
	}

	private static bool CanRenameAssembly(string assemblyPath)
	{
		string text = assemblyPath + ".unlock-check";
		try
		{
			File.Move(assemblyPath, text, overwrite: true);
			File.Move(text, assemblyPath, overwrite: true);
			return true;
		}
		catch
		{
			try
			{
				if (File.Exists(text) && !File.Exists(assemblyPath))
				{
					File.Move(text, assemblyPath, overwrite: true);
				}
			}
			catch
			{
			}
			return false;
		}
	}

	private static bool WaitForCollection(WeakReference contextReference)
	{
		if (contextReference == null)
		{
			return false;
		}
		for (int i = 0; i < 12; i++)
		{
			if (!contextReference.IsAlive)
			{
				break;
			}
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
		return !contextReference.IsAlive;
	}

	private static bool IsAssemblyStillLoaded(string simpleName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			if (string.Equals(assemblies[i].GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<bool> OpenRealEditorAsync()
	{
		ModEditorManager modEditorManager = ModEditorManager.Instance;
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(modEditorManager))
			{
				AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			return false;
		}
		await WaitFrames(2);
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = Key.F3,
			PhysicalKeycode = Key.F3,
			Pressed = true
		});
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = Key.F3,
			PhysicalKeycode = Key.F3,
			Pressed = false
		});
		for (int frame = 0; frame < 300; frame++)
		{
			Control control = XWEditorInterface.Instance?.GetEditorPanel();
			if (GodotObject.IsInstanceValid(control) && control.IsVisibleInTree())
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private static string BuildProjectFile(string assemblyName, string projectReference = "")
	{
		string text = (string.IsNullOrWhiteSpace(projectReference) ? "" : ("  <ItemGroup>\n    <ProjectReference Include=\"" + projectReference + "\" />\n  </ItemGroup>\n"));
		return "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n    <AssemblyName>" + assemblyName + "</AssemblyName>\n    <ImplicitUsings>disable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n" + text + "</Project>\n";
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private bool CleanupTempRoot()
	{
		if (string.IsNullOrWhiteSpace(_tempRoot))
		{
			return true;
		}
		try
		{
			string fullPath = Path.GetFullPath(_tempRoot);
			string fullPath2 = Path.GetFullPath(Path.GetTempPath());
			if (!fullPath.StartsWith(fullPath2, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (Directory.Exists(fullPath))
			{
				Directory.Delete(fullPath, recursive: true);
			}
			return !Directory.Exists(fullPath);
		}
		catch
		{
			return false;
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
