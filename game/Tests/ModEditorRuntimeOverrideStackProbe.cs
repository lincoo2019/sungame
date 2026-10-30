using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;

public partial class ModEditorRuntimeOverrideStackProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		string suffix = Guid.NewGuid().ToString("N");
		string directKey = "probe_stack_direct_" + suffix;
		string missingKey = "probe_stack_missing_" + suffix;
		string previewKey = "probe_stack_preview_" + suffix;
		string loaderKey = "probe_stack_loader_" + suffix;
		string provider = "probe.provider." + suffix;
		string firstOverride = "probe.override.first." + suffix;
		string secondOverride = "probe.override.second." + suffix;
		string rejectedProvider = "probe.provider.rejected." + suffix;
		string missingOverride = "probe.override.missing." + suffix;
		string previewOwner = "ModEditorPreview." + suffix;
		string loaderA = "probe.loader.a." + suffix;
		string loaderB = "probe.loader.b." + suffix;
		string workRoot = ProjectSettings.GlobalizePath("user://RuntimeOverrideStackProbe/");
		string[] directOwners = new string[6] { provider, firstOverride, secondOverride, rejectedProvider, missingOverride, previewOwner };
		try
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
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
			bool flag = await WaitForEditorWindow(900);
			Require(flag, "F3 did not open the real ModEditor window.");
			TowerDefenseBattleFeature from = new TowerDefenseBattleFeature
			{
				ResourceName = "provided"
			};
			TowerDefenseBattleFeature from2 = new TowerDefenseBattleFeature
			{
				ResourceName = "override-first"
			};
			TowerDefenseBattleFeature from3 = new TowerDefenseBattleFeature
			{
				ResourceName = "override-second"
			};
			string diagnostic;
			TowerDefenseBattleFeature value;
			bool flag2 = XWModRuntimeRegistry.Register(provider, "Features", directKey, Variant.From(in from), allowOverride: false, out diagnostic) && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(directKey), out value) && value == from;
			string diagnostic2;
			bool flag3 = !XWModRuntimeRegistry.Register(rejectedProvider, "Feature", directKey, Variant.From<TowerDefenseBattleFeature>(new TowerDefenseBattleFeature()), allowOverride: false, out diagnostic2) && diagnostic2.Contains("provides collision", StringComparison.Ordinal) && diagnostic2.Contains("declare it under overrides", StringComparison.Ordinal);
			string diagnostic3;
			bool flag4 = !XWModRuntimeRegistry.Register(missingOverride, "Feature", missingKey, Variant.From<TowerDefenseBattleFeature>(new TowerDefenseBattleFeature()), allowOverride: true, out diagnostic3) && diagnostic3.Contains("overrides target is missing", StringComparison.Ordinal) && diagnostic3.Contains("declare it under provides", StringComparison.Ordinal);
			TowerDefenseBattleFeature from4 = new TowerDefenseBattleFeature
			{
				ResourceName = "preview-upsert"
			};
			TowerDefenseBattleFeature value2;
			bool flag5 = XWModRuntimeRegistry.Register(previewOwner, "Feature", previewKey, Variant.From(in from4), allowOverride: true) && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(previewKey), out value2) && value2 == from4 && XWModRuntimeRegistry.UnregisterOwner(previewOwner) == 1 && !TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey(new StringName(previewKey));
			string diagnostic4;
			bool num = XWModRuntimeRegistry.Register(firstOverride, "Feature", directKey, Variant.From(in from2), allowOverride: true, out diagnostic4);
			string diagnostic5;
			bool flag6 = XWModRuntimeRegistry.Register(secondOverride, "Feature", directKey, Variant.From(in from3), allowOverride: true, out diagnostic5);
			IReadOnlyList<XWModRuntimeRegistry.Registration> registrationStack = XWModRuntimeRegistry.GetRegistrationStack("Features", directKey);
			XWModRuntimeRegistry.Registration registration;
			TowerDefenseBattleFeature value3;
			bool flag7 = num && flag6 && registrationStack.Count == 3 && registrationStack[0].OwnerMod == provider && registrationStack[1].OwnerMod == firstOverride && registrationStack[2].OwnerMod == secondOverride && !registrationStack[0].IsOverride && registrationStack[1].IsOverride && registrationStack[2].IsOverride && registrationStack[0].LoadOrder < registrationStack[1].LoadOrder && registrationStack[1].LoadOrder < registrationStack[2].LoadOrder && !registrationStack[0].IsEffective && !registrationStack[1].IsEffective && registrationStack[2].IsEffective && XWModRuntimeRegistry.TryGetEffectiveRegistration("Feature", directKey, out registration) && registration.OwnerMod == secondOverride && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(directKey), out value3) && value3 == from3;
			int num2 = XWModRuntimeRegistry.UnregisterOwner(firstOverride);
			TowerDefenseBattleFeature value4;
			bool flag8 = TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(directKey), out value4) && value4 == from3 && XWModRuntimeRegistry.GetRegistrationStack("Feature", directKey).Count == 2;
			int num3 = XWModRuntimeRegistry.UnregisterOwner(provider);
			TowerDefenseBattleFeature value5;
			bool flag9 = TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(directKey), out value5) && value5 == from3 && XWModRuntimeRegistry.GetRegistrationStack("Feature", directKey).Count == 1;
			int num4 = XWModRuntimeRegistry.UnregisterOwner(secondOverride);
			int num5 = XWModRuntimeRegistry.UnregisterOwner(secondOverride);
			bool flag10 = num2 == 1 && num3 == 1 && num4 == 1 && num5 == 0 && flag8 && flag9 && !TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey(new StringName(directKey));
			bool flag11 = diagnostic.Contains("provided new runtime value", StringComparison.Ordinal) && diagnostic4.Contains("stacked override", StringComparison.Ordinal) && diagnostic5.Contains(firstOverride, StringComparison.Ordinal) && !diagnostic2.Contains(rejectedProvider, StringComparison.Ordinal);
			var (flag12, flag13, flag14) = CheckBuiltInFactoryOverrides(suffix);
			Require(flag12, "A Feature override did not replace the built-in construction factory.");
			Require(flag13, "A Process override did not replace the built-in construction factory.");
			Require(flag14, "Unloading overrides did not restore the original types and metadata.");
			if (Directory.Exists(workRoot))
			{
				Directory.Delete(workRoot, recursive: true);
			}
			Directory.CreateDirectory(workRoot);
			string featurePath = SaveFeature(workRoot, "feature_a.tres", "loader-a");
			string featurePath2 = SaveFeature(workRoot, "feature_b.tres", "loader-b");
			string text = Path.Combine(workRoot, "unsafe.cs");
			File.WriteAllText(text, "using Godot; public partial class UntrustedProbe : Node { }");
			TowerDefenseBattleFeature towerDefenseBattleFeature = new TowerDefenseBattleFeature
			{
				ResourceName = "loader-built-in"
			};
			TowerDefenseBattleRegistry.BattleFeatureDictionary[new StringName(loaderKey)] = towerDefenseBattleFeature;
			ModLoader.LoadedMod loadedMod = CreateLoadedMod(loaderA, loaderKey, featurePath, text);
			ModLoader.LoadedMod mod = CreateLoadedMod(loaderB, loaderKey, featurePath2, null);
			bool flag15 = ModLoader.ApplyMod(loadedMod);
			bool flag16 = ModLoader.ApplyMod(loadedMod);
			TowerDefenseBattleFeature value6;
			bool flag17 = XWModRuntimeRegistry.GetRegistrationStack("Feature", loaderKey).Count == 1 && ModLoader.GetLoadedMods().Count == 1 && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(loaderKey), out value6) && value6.ResourceName == "loader-a";
			bool flag18 = ModLoader.ApplyMod(mod);
			TowerDefenseBattleFeature value7;
			bool flag19 = XWModRuntimeRegistry.GetRegistrationStack("Feature", loaderKey).Count == 2 && ModLoader.GetLoadedMods().Count == 2 && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(loaderKey), out value7) && value7.ResourceName == "loader-b";
			bool flag20 = flag15 && flag16 && flag18 && flag17 && flag19;
			bool flag21 = loadedMod.Diagnostics.FindAll((string item) => item.Contains("blocked executable package file: Scripts/unsafe.cs", StringComparison.Ordinal)).Count == 1;
			bool num6 = ModLoader.UnloadMod(loaderA);
			bool flag22 = ModLoader.UnloadMod(loaderA);
			TowerDefenseBattleFeature value8;
			bool flag23 = num6 && !flag22 && XWModRuntimeRegistry.GetRegistrationStack("Feature", loaderKey).Count == 1 && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(loaderKey), out value8) && value8.ResourceName == "loader-b";
			ModLoader.UnloadAll();
			ModLoader.UnloadAll();
			TowerDefenseBattleFeature value9;
			bool flag24 = ModLoader.GetLoadedMods().Count == 0 && XWModRuntimeRegistry.GetRegistrationStack("Feature", loaderKey).Count == 0 && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(loaderKey), out value9) && value9 == towerDefenseBattleFeature;
			Require(flag2, "A provides declaration did not add an absent runtime key.");
			Require(flag3, "A second provides declaration did not report a truthful collision.");
			Require(flag4, "An overrides declaration was allowed without a lower target.");
			Require(flag5, "The legacy ModEditor preview upsert path was broken by strict manifest semantics.");
			Require(flag7, "Later overrides did not form an ordered effective layer stack.");
			Require(flag10, "Unloading a middle/lower owner did not preserve the correct effective layer.");
			Require(flag20, "Applying one Mod twice duplicated tracking or stack layers.");
			Require(flag23, "ModLoader could not unload an arbitrary lower owner idempotently.");
			Require(flag24, "Repeated UnloadAll did not restore the built-in runtime value.");
			Require(flag11, "Provide/override registration diagnostics do not match the real result.");
			Require(flag21, "The override-stack path attempted or ignored an untrusted script instead of blocking it.");
			GD.Print($"[MOD_RUNTIME_OVERRIDE_STACK_PROBE] f3={flag} provide={flag2} provideConflict={flag3} overrideMissing={flag4} previewUpsert={flag5} stack={flag7} middleUnload={flag10} loaderIdempotent={flag20} arbitraryUnload={flag23} unloadAll={flag24} diagnostics={flag11} scriptsBlocked={flag21} featureFactory={flag12} processFactory={flag13} restoredFactory={flag14} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		finally
		{
			string[] array = directOwners;
			for (int num7 = 0; num7 < array.Length; num7++)
			{
				XWModRuntimeRegistry.UnregisterOwner(array[num7]);
			}
			ModLoader.UnloadMod(loaderA);
			ModLoader.UnloadMod(loaderB);
			TowerDefenseBattleRegistry.BattleFeatureDictionary.Remove(new StringName(directKey));
			TowerDefenseBattleRegistry.BattleFeatureDictionary.Remove(new StringName(missingKey));
			TowerDefenseBattleRegistry.BattleFeatureDictionary.Remove(new StringName(previewKey));
			TowerDefenseBattleRegistry.BattleFeatureDictionary.Remove(new StringName(loaderKey));
			if (Directory.Exists(workRoot))
			{
				Directory.Delete(workRoot, recursive: true);
			}
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_RUNTIME_OVERRIDE_STACK_PROBE_FAILURE] " + failure);
		}
		await WaitFrames(1);
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static (bool Feature, bool Process, bool Restored) CheckBuiltInFactoryOverrides(string suffix)
	{
		TowerDefenseBattleRegistry.Init();
		StringName stringName = new StringName("Wave");
		TowerDefenseBattleFeature towerDefenseBattleFeature = TowerDefenseBattleRegistry.BattleFeatureDictionary[stringName];
		TowerDefenseBattleProcess towerDefenseBattleProcess = TowerDefenseBattleRegistry.BattleProcessDictionary[stringName];
		string ownerMod = "probe.factory.first." + suffix;
		string ownerMod2 = "probe.factory.second." + suffix;
		string text = "probe.factory.new." + suffix;
		using TowerDefenseBattleFeatureCamera from = new TowerDefenseBattleFeatureCamera
		{
			gameStartPriority = 731
		};
		using TowerDefenseBattleFeatureSun from2 = new TowerDefenseBattleFeatureSun
		{
			gameStartPriority = 732
		};
		using TowerDefenseBattleProcessEmpty from3 = new TowerDefenseBattleProcessEmpty
		{
			gameStartPriority = 831
		};
		using TowerDefenseBattleProcessQuiz from4 = new TowerDefenseBattleProcessQuiz
		{
			gameStartPriority = 832
		};
		from.dependenceData = new TowerDefenseBattleDependenceData
		{
			FeatureNames = new Array<StringName> { "Map" }
		};
		from3.dependenceData = new TowerDefenseBattleDependenceData
		{
			FeatureNames = new Array<StringName> { "Map", "Sun" }
		};
		try
		{
			string diagnostic;
			bool flag = XWModRuntimeRegistry.Register(ownerMod, "Feature", "Wave", Variant.From(in from), allowOverride: true, out diagnostic) && XWModRuntimeRegistry.Register(ownerMod, "Process", "Wave", Variant.From(in from3), allowOverride: true, out diagnostic) && XWModRuntimeRegistry.Register(ownerMod, "Feature", text, Variant.From(in from), allowOverride: false, out diagnostic) && XWModRuntimeRegistry.Register(ownerMod, "Process", text, Variant.From(in from3), allowOverride: false, out diagnostic);
			using TowerDefenseBattleFeature towerDefenseBattleFeature2 = TowerDefenseBattleRegistry.GetFeature(stringName);
			using TowerDefenseBattleProcess towerDefenseBattleProcess2 = TowerDefenseBattleRegistry.GetProcess(stringName);
			using TowerDefenseBattleFeature towerDefenseBattleFeature3 = TowerDefenseBattleRegistry.GetFeature(text);
			using TowerDefenseBattleProcess towerDefenseBattleProcess3 = TowerDefenseBattleRegistry.GetProcess(text);
			bool flag2 = flag && towerDefenseBattleFeature2?.GetType() == from.GetType() && towerDefenseBattleFeature3?.GetType() == from.GetType() && towerDefenseBattleFeature2.gameStartPriority == 731 && towerDefenseBattleFeature2.dependenceData == from.dependenceData;
			bool flag3 = flag && towerDefenseBattleProcess2?.GetType() == from3.GetType() && towerDefenseBattleProcess3?.GetType() == from3.GetType() && towerDefenseBattleProcess2.gameStartPriority == 831 && towerDefenseBattleProcess2.dependenceData == from3.dependenceData;
			bool flag4 = XWModRuntimeRegistry.Register(ownerMod2, "Feature", "Wave", Variant.From(in from2), allowOverride: true, out diagnostic) && XWModRuntimeRegistry.Register(ownerMod2, "Process", "Wave", Variant.From(in from4), allowOverride: true, out diagnostic);
			bool flag5 = XWModRuntimeRegistry.UnregisterOwner(ownerMod) == 4 && !TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey(text) && !TowerDefenseBattleRegistry.BattleProcessDictionary.ContainsKey(text);
			using TowerDefenseBattleFeature towerDefenseBattleFeature4 = TowerDefenseBattleRegistry.GetFeature(stringName);
			using TowerDefenseBattleProcess towerDefenseBattleProcess4 = TowerDefenseBattleRegistry.GetProcess(stringName);
			flag2 &= flag4 && flag5 && towerDefenseBattleFeature4?.GetType() == from2.GetType() && towerDefenseBattleFeature4.gameStartPriority == 732;
			flag3 &= flag4 && flag5 && towerDefenseBattleProcess4?.GetType() == from4.GetType() && towerDefenseBattleProcess4.gameStartPriority == 832;
			bool flag6 = XWModRuntimeRegistry.UnregisterOwner(ownerMod2) == 2;
			using TowerDefenseBattleFeature towerDefenseBattleFeature5 = TowerDefenseBattleRegistry.GetFeature(stringName);
			using TowerDefenseBattleProcess towerDefenseBattleProcess5 = TowerDefenseBattleRegistry.GetProcess(stringName);
			bool item = flag6 && towerDefenseBattleFeature5?.GetType() == towerDefenseBattleFeature.GetType() && towerDefenseBattleProcess5?.GetType() == towerDefenseBattleProcess.GetType() && towerDefenseBattleFeature5.gameStartPriority == towerDefenseBattleFeature.gameStartPriority && towerDefenseBattleProcess5.gameStartPriority == towerDefenseBattleProcess.gameStartPriority && towerDefenseBattleFeature5.dependenceData == towerDefenseBattleFeature.dependenceData && towerDefenseBattleProcess5.dependenceData == towerDefenseBattleProcess.dependenceData && TowerDefenseBattleRegistry.BattleFeatureDictionary[stringName] == towerDefenseBattleFeature && TowerDefenseBattleRegistry.BattleProcessDictionary[stringName] == towerDefenseBattleProcess;
			return (Feature: flag2, Process: flag3, Restored: item);
		}
		finally
		{
			XWModRuntimeRegistry.UnregisterOwner(ownerMod);
			XWModRuntimeRegistry.UnregisterOwner(ownerMod2);
		}
	}

	private static ModLoader.LoadedMod CreateLoadedMod(string owner, string runtimeKey, string featurePath, string scriptPath)
	{
		XWModManifest xWModManifest = new XWModManifest
		{
			Id = owner,
			Name = owner
		};
		xWModManifest.Overrides["Feature"] = new List<string> { runtimeKey };
		if (!string.IsNullOrWhiteSpace(scriptPath))
		{
			xWModManifest.Scripts.Add("Scripts/unsafe.cs");
		}
		ModLoader.LoadedMod loadedMod = new ModLoader.LoadedMod
		{
			Manifest = xWModManifest,
			FilePath = owner + ".pmod"
		};
		loadedMod.RelativeFiles.Add("Battle/Features/" + Path.GetFileName(featurePath));
		loadedMod.ExtractedFiles.Add(featurePath);
		if (!string.IsNullOrWhiteSpace(scriptPath))
		{
			loadedMod.RelativeFiles.Add("Scripts/unsafe.cs");
			loadedMod.ExtractedFiles.Add(scriptPath);
		}
		return loadedMod;
	}

	private static string SaveFeature(string root, string fileName, string resourceName)
	{
		string text = Path.Combine(root, fileName);
		Error error = ResourceSaver.Save(new TowerDefenseBattleFeature
		{
			ResourceName = resourceName
		}, ProjectSettings.LocalizePath(text), ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Could not save {fileName}: {error}");
		}
		return text;
	}

	private async Task<bool> WaitForEditorWindow(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Window window = FindEditorWindow(GetTree().Root);
			if (GodotObject.IsInstanceValid(window) && window.Visible)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static Window FindEditorWindow(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is Window window && window.Title.StartsWith("PVZ Mod", StringComparison.Ordinal))
		{
			return window;
		}
		foreach (Node child in root.GetChildren())
		{
			Window window2 = FindEditorWindow(child);
			if (GodotObject.IsInstanceValid(window2))
			{
				return window2;
			}
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_RUNTIME_OVERRIDE_STACK_PROBE_FAILURE] " + message);
		}
	}
}
