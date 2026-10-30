using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ModSystem.Validation;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorRuntimeModPackageProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		string key = "probe_feature_" + Guid.NewGuid().ToString("N");
		string owner = "runtime.package.probe." + key;
		string text = "probe_audio_file_" + Guid.NewGuid().ToString("N");
		string audioRuntimeKey = "probe.audio.runtime." + Guid.NewGuid().ToString("N");
		string text2 = "ProbeCharacterFolder" + Guid.NewGuid().ToString("N");
		string characterRuntimeKey = "probe.character.runtime." + Guid.NewGuid().ToString("N");
		string workRoot = ProjectSettings.GlobalizePath("user://RuntimeModPackageProbe/");
		try
		{
			_ = 11;
			try
			{
				if (Directory.Exists(workRoot))
				{
					Directory.Delete(workRoot, recursive: true);
				}
				Directory.CreateDirectory(workRoot);
				string sourceResource = Path.Combine(workRoot, key + ".tres");
				TowerDefenseBattleFeature resource = new TowerDefenseBattleFeature
				{
					ResourceName = "modded-feature"
				};
				Require(ResourceSaver.Save(resource, ProjectSettings.LocalizePath(sourceResource), ResourceSaver.SaverFlags.None) == Error.Ok, "Could not save the package probe resource.");
				string audioSource = Path.Combine(workRoot, text + ".wav");
				WriteProbeWav(audioSource);
				string characterConfig = WriteTextFile(workRoot, text2 + "Config.tres", "[gd_resource type=\"Resource\" format=3]\n\n[resource]\nresource_name = \"ProbeCharacterConfig\"\nmetadata/probe_value = \"config-loaded\"\n");
				string value = WriteTextFile(workRoot, text2 + ".cs", "using Godot; public partial class UnsafePackageScript : Node2D { }\n");
				string value2 = WriteTextFile(workRoot, text2 + ".tscn", "[gd_scene load_steps=2 format=3]\n\n[ext_resource type=\"Resource\" path=\"../Config/" + text2 + "Config.tres\" id=\"1_config\"]\n[node name=\"ProbeCharacter\" type=\"Node2D\"]\nmetadata/character_config = ExtResource(\"1_config\")\n");
				string value3 = WriteTextFile(workRoot, "unsafe_blueprint.json", "{\"nodes\":[]}");
				string validPackage = Path.Combine(workRoot, "valid.pmod");
				string manifest = "{\"schemaVersion\":1,\"id\":\"" + owner + "\",\"name\":\"Runtime Package Probe\",\"version\":\"1.0.0\",\"provides\":{\"Character\":[\"" + characterRuntimeKey + "\"]},\"overrides\":{\"Feature\":[\"" + key + "\"],\"Audio\":[\"" + audioRuntimeKey + "\"]},\"blueprints\":[\"Blueprints/unsafe.json\"],\"resources\":[\"Battle/Features/" + key + ".tres\",\"Assets/Audio/Sfx/" + text + ".wav\",\"Resources/Characters/Plants/" + text2 + "/Scene/" + text2 + ".tscn\"]}";
				CreatePackage(validPackage, manifest, new Dictionary<string, string>
				{
					["Battle/Features/" + key + ".tres"] = sourceResource,
					["Assets/Audio/Sfx/" + text + ".wav"] = audioSource,
					[$"Resources/Characters/Plants/{text2}/Config/{text2}Config.tres"] = characterConfig,
					[$"Resources/Characters/Plants/{text2}/Scene/{text2}.tscn"] = value2,
					["Blueprints/unsafe.json"] = value3,
					["Assets/notes.txt"] = WriteTextFile(workRoot, "notes.txt", "unsupported but preserved")
				});
				string text3 = Path.Combine(workRoot, "unsafe-script.pmod");
				CreatePackage(text3, manifest, new Dictionary<string, string> { [$"Resources/Characters/Plants/{text2}/Script/{text2}.cs"] = value });
				XWModManifest manifest2;
				ModExporter.ModInfo info;
				string diagnostic;
				bool scriptsBlocked = !ModLoader.ValidatePackageArchive(text3, out manifest2, out info, out diagnostic) && diagnostic.Contains("undeclared executable package file", StringComparison.Ordinal);
				Require(scriptsBlocked, "Installed package validation accepted an undeclared executable.");
				TowerDefenseBattleFeature originalFeature = new TowerDefenseBattleFeature
				{
					ResourceName = "original-feature"
				};
				TowerDefenseBattleRegistry.BattleFeatureDictionary[new StringName(key)] = originalFeature;
				AudioStreamWav originalAudio = new AudioStreamWav();
				ResourceManager.Instance.AUDIOS[audioRuntimeKey] = originalAudio;
				ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(validPackage);
				bool relative = loadedMod != null && loadedMod.RelativeFiles.Contains("Battle/Features/" + key + ".tres") && loadedMod.RelativeFiles.Contains("Assets/notes.txt") && loadedMod.ExtractedFiles.Exists((string text8) => text8.EndsWith(Path.Combine("Battle", "Features", key + ".tres"), StringComparison.OrdinalIgnoreCase));
				Require(relative, "Package-relative paths were not preserved.");
				string modsDirectory = ProjectSettings.GlobalizePath("user://Mods/");
				if (Directory.Exists(modsDirectory))
				{
					Directory.Delete(modsDirectory, recursive: true);
				}
				Directory.CreateDirectory(modsDirectory);
				File.Copy(validPackage, Path.Combine(modsDirectory, "valid.pmod"), overwrite: true);
				new XWModManager(modsDirectory).SaveEnabledIds(new string[1] { owner });
				ModEditorManager instance = ModEditorManager.Instance;
				if (!GodotObject.IsInstanceValid(instance))
				{
					instance = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
					if (GodotObject.IsInstanceValid(instance))
					{
						AddChild(instance, forceReadableName: false, InternalMode.Disabled);
					}
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
				PopupMenu popupMenu = await WaitForModMenu(900);
				bool f3 = GodotObject.IsInstanceValid(popupMenu);
				bool menu = f3 && popupMenu.GetItemText(popupMenu.GetItemIndex(20)).Contains("应用已安装 Mod", StringComparison.Ordinal) && popupMenu.GetItemText(popupMenu.GetItemIndex(21)).Contains("打开 Mod 安装目录", StringComparison.Ordinal);
				Require(f3 && menu, "F3 ModEditor did not expose the corrected runtime package menu semantics.");
				popupMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 20L);
				await WaitFrames(8);
				ModLoader.LoadedMod appliedMod = ((ModLoader.GetLoadedMods().Count == 1) ? ModLoader.GetLoadedMods()[0] : null);
				XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.CaptureAsync();
				Require(xWModEnvironmentStatus.Ready && xWModEnvironmentStatus.Snapshot.Mods.Count == 1 && xWModEnvironmentStatus.Snapshot.Mods[0].EffectiveMode == "ResourceOnly" && xWModEnvironmentStatus.Snapshot.Mods[0].PackageSha256 == appliedMod?.PackageSha256, "Formal resource package did not produce its actual environment snapshot.");
				if (xWModEnvironmentStatus.Ready)
				{
					XWModEnvironmentSnapshot snapshot = xWModEnvironmentStatus.Snapshot;
					bool ready = (await XWModEnvironmentService.CaptureAsync(new CancellationToken(canceled: true))).Ready;
					if (ready)
					{
						ready = (await XWModEnvironmentService.CaptureAsync()).Ready;
					}
					Require(ready, "Cancelled capture discarded an unchanged formal environment.");
					XWModSnapshotEntry xWModSnapshotEntry = snapshot.Mods[0];
					XWModSnapshotEntry[] array = new XWModSnapshotEntry[5]
					{
						xWModSnapshotEntry with
						{
							Version = "different"
						},
						xWModSnapshotEntry with
						{
							PackageSha256 = new string('0', 64)
						},
						xWModSnapshotEntry with
						{
							RuntimeApiVersion = 99
						},
						xWModSnapshotEntry with
						{
							EffectiveMode = "Managed"
						},
						xWModSnapshotEntry with
						{
							LoadOrderIndex = 7
						}
					};
					foreach (XWModSnapshotEntry xWModSnapshotEntry2 in array)
					{
						Require(XWModEnvironmentService.CompareSnapshots(snapshot, snapshot with
						{
							Mods = new XWModSnapshotEntry[1] { xWModSnapshotEntry2 }
						}).Count == 1, "Snapshot comparison missed a single-field environment mismatch.");
					}
					Require(XWModEnvironmentService.CompareSnapshots(snapshot, snapshot).Count == 0, "Snapshot self comparison produced a difference.");
					string installed = Path.Combine(modsDirectory, "valid.pmod");
					Require(XWModRuntimeRegistry.Register("ModEditorPreview.M2", "Feature", "probe.m2.preview", Variant.From<TowerDefenseBattleFeatureCamera>(new TowerDefenseBattleFeatureCamera())), "Preview fixture failed to register.");
					ready = !XWModEnvironmentService.GetStatus().Ready;
					if (ready)
					{
						ready = !(await XWModEnvironmentService.CaptureAsync()).Ready;
					}
					Require(ready, "Extra preview registration was accepted as part of the formal environment.");
					XWModRuntimeRegistry.UnregisterOwner("ModEditorPreview.M2");
					Require(!(await XWModEnvironmentService.CaptureAsync()).Ready, "Closing preview reconstructed a formal snapshot without a complete reapply.");
					ready = new XWModManager(modsDirectory).LoadEnabledModsWithResult() == 1;
					if (ready)
					{
						ready = (await XWModEnvironmentService.CaptureAsync()).Ready;
					}
					Require(ready, "Formal reapply did not restore Ready after preview.");
					using (FileStream fileStream = new FileStream(installed, FileMode.Append))
					{
						fileStream.WriteByte(0);
					}
					Require(!(await XWModEnvironmentService.CaptureAsync()).Ready, "Changed installed package retained a Ready snapshot.");
					File.Copy(validPackage, installed, overwrite: true);
				}
				TowerDefenseBattleFeature value4;
				bool registered = appliedMod != null && appliedMod.AppliedResourceCount == 3 && TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(key), out value4) && value4.ResourceName == "modded-feature";
				Resource value5;
				bool audio = ResourceManager.Instance.AUDIOS.TryGetValue(audioRuntimeKey, out value5) && value5 is AudioStreamWav && value5 != originalAudio;
				Resource value6;
				bool character = ResourceManager.Instance.TOWERDEFENSE_CHARCATERS.TryGetValue(characterRuntimeKey, out value6) && value6 is PackedScene;
				bool characterConfigLoaded = false;
				if (character && value6 is PackedScene packedScene)
				{
					Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
					characterConfigLoaded = node.GetMeta("character_config").AsGodotObject() is Resource { ResourceName: "ProbeCharacterConfig" } resource2 && resource2.GetMeta("probe_value").AsString() == "config-loaded";
					node.Free();
				}
				bool diagnostics = appliedMod?.Diagnostics.Exists((string item) => item.Contains("unsupported package file", StringComparison.Ordinal)) ?? false;
				Require(registered, "Supported resource was not registered into the real runtime registry.");
				Require(audio, "Manifest-keyed WAV audio was not registered into ResourceManager.AUDIOS.");
				Require(character && characterConfigLoaded, "Standard character scene/config package was not loaded under its explicit manifest key.");
				Require(diagnostics, "Unsupported package files did not produce explicit diagnostics.");
				string text4 = Path.Combine(workRoot, "conflict.pmod");
				CreatePackage(text4, "{\"schemaVersion\":1,\"id\":\"" + owner + ".conflict\",\"name\":\"Conflict Probe\",\"version\":\"1.0.0\",\"provides\":{\"Audio\":[\"" + audioRuntimeKey + "\"]},\"overrides\":{}}", new Dictionary<string, string> { ["Assets/Audio/Sfx/conflict_file.wav"] = audioSource });
				ModLoader.LoadedMod loadedMod2 = ModLoader.LoadMod(text4);
				bool conflictRejected = loadedMod2 != null && !ModLoader.ApplyMod(loadedMod2) && loadedMod2.Diagnostics.Exists((string item) => item.Contains("runtime registry rejected resource: Audio/" + audioRuntimeKey, StringComparison.Ordinal));
				Require(conflictRejected, "A second Mod owner was allowed to claim an occupied audio key.");
				string text5 = Path.Combine(workRoot, "ambiguous.pmod");
				string ambiguousKey = "duplicate_audio_" + Guid.NewGuid().ToString("N");
				CreatePackage(text5, "{\"schemaVersion\":1,\"id\":\"" + owner + ".ambiguous\",\"name\":\"Ambiguous Probe\",\"version\":\"1.0.0\",\"provides\":{},\"overrides\":{}}", new Dictionary<string, string>
				{
					["Assets/Audio/Sfx/" + ambiguousKey + ".wav"] = audioSource,
					["Assets/Audio/BGM/" + ambiguousKey + ".wav"] = audioSource
				});
				ModLoader.LoadedMod loadedMod3 = ModLoader.LoadMod(text5);
				bool ambiguityRejected = loadedMod3 != null && !ModLoader.ApplyMod(loadedMod3) && loadedMod3.Diagnostics.FindAll((string item) => item.Contains("ambiguous automatic runtime key: Audio/" + ambiguousKey, StringComparison.Ordinal)).Count == 2 && !ResourceManager.Instance.AUDIOS.ContainsKey(ambiguousKey);
				Require(ambiguityRejected, "Ambiguous automatic audio keys were not rejected as a group.");
				ModLoader.UnloadAll();
				TowerDefenseBattleFeature value7;
				Resource value8;
				bool unload = TowerDefenseBattleRegistry.BattleFeatureDictionary.TryGetValue(new StringName(key), out value7) && value7 == originalFeature && ResourceManager.Instance.AUDIOS.TryGetValue(audioRuntimeKey, out value8) && value8 == originalAudio && !ResourceManager.Instance.TOWERDEFENSE_CHARCATERS.ContainsKey(characterRuntimeKey);
				Require(unload, "Unloading the package did not restore the overridden runtime resource.");
				string text6 = Path.Combine(workRoot, "malicious.pmod");
				CreatePackage(text6, "{\"name\":\"malicious\",\"version\":\"1.0.0\",\"files\":[]}", new Dictionary<string, string> { ["../escaped.txt"] = WriteTextFile(workRoot, "escape-source.txt", "escape") });
				string path = ProjectSettings.GlobalizePath("user://ModsCache/escaped.txt");
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				bool zipSlip = ModLoader.LoadMod(text6) == null && !File.Exists(path);
				Require(zipSlip, "Zip Slip entry escaped the package cache root.");
				string text7 = Path.Combine(workRoot, "unsupported.pmod");
				CreatePackage(text7, "{\"name\":\"unsupported\",\"version\":\"1.0.0\",\"files\":[\"Assets/only.txt\"]}", new Dictionary<string, string> { ["Assets/only.txt"] = WriteTextFile(workRoot, "only.txt", "only") });
				ModLoader.LoadedMod loadedMod4 = ModLoader.LoadMod(text7);
				bool unsupported = loadedMod4 != null && !ModLoader.ApplyMod(loadedMod4) && loadedMod4.AppliedResourceCount == 0 && loadedMod4.Diagnostics.Count > 0;
				Require(unsupported, "Unsupported-only package was falsely reported as applied.");
				bool requiredRollback = CheckRequiredResourceRollback(workRoot, key, sourceResource, characterConfig);
				bool levelCatalog = CheckLevelCatalogDependency(workRoot);
				bool optionalFallback = CheckOptionalResourceFallback(workRoot, sourceResource);
				Require(requiredRollback, "A failed required declaration left a partial registration or applied owner.");
				Require(levelCatalog, "A level dependency was registered as a catalog or its packaged reference failed to resolve.");
				Require(optionalFallback, "Optional assembly fallback, case-insensitive declarations or unrelated resource ambiguity regressed.");
				bool installationTransactions = await RunM5InstallationTransactionProbe(workRoot);
				Require(installationTransactions, "M5 package installation, stale confirmation or deletion transaction regressed.");
				var (flag, flag2, flag3, flag4, flag5, flag6, flag7) = await RunManagedLifecycleProbe(workRoot);
				Require(flag, "Installed packages were not disabled when enabled_mods.json was absent.");
				Require(flag2, "Enabled package dependency order or cycle validation was incorrect.");
				Require(flag3, "Code-only package lifecycle did not run Initialize/OnAllModsLoaded/Shutdown.");
				Require(flag4, "A failing managed entry was not rolled back completely.");
				Require(flag5, "A packaged private managed dependency was not resolved.");
				Require(flag6, "A callback-only managed package was not retained as applied.");
				Require(flag7, "Managed package export allowed an unsafe or duplicate runtime path.");
				GD.Print($"[MOD_RUNTIME_PACKAGE_PROBE] f3={f3} menu={menu} relative={relative} zipSlip={zipSlip} register={registered} override={registered} unload={unload} audio={audio} character={character} config={characterConfigLoaded} scriptsBlocked={scriptsBlocked} conflict={conflictRejected} ambiguity={ambiguityRejected} unsupported={unsupported} diagnostics={diagnostics} defaultDisabled={flag} topology={flag2} lifecycle={flag3} rollback={flag4} dependency={flag5} callbackOnly={flag6} exportSafety={flag7} requiredRollback={requiredRollback} levelCatalog={levelCatalog} optionalFallback={optionalFallback} m5Install={installationTransactions} failures={_failures.Count}");
			}
			catch (Exception ex)
			{
				_failures.Add(ex.ToString());
			}
		}
		finally
		{
			XWModRuntimeRegistry.UnregisterOwner(owner);
			TowerDefenseBattleRegistry.BattleFeatureDictionary.Remove(new StringName(key));
			ResourceManager.Instance?.AUDIOS.Remove(audioRuntimeKey);
			ResourceManager.Instance?.TOWERDEFENSE_CHARCATERS.Remove(characterRuntimeKey);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			try
			{
				if (Directory.Exists(workRoot))
				{
					Directory.Delete(workRoot, recursive: true);
				}
			}
			catch (Exception ex2)
			{
				GD.PushWarning("[MOD_RUNTIME_PACKAGE_PROBE] temporary cleanup deferred: " + ex2.GetBaseException().Message);
			}
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_RUNTIME_PACKAGE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private bool CheckRequiredResourceRollback(string workRoot, string featureKey, string validFeature, string wrongType)
	{
		TowerDefenseBattleRegistry.Init();
		TowerDefenseBattleFeature towerDefenseBattleFeature = TowerDefenseBattleRegistry.BattleFeatureDictionary[featureKey];
		bool flag = true;
		string[] array = new string[5] { "type", "missing", "collision", "ambiguous", "load" };
		foreach (string text in array)
		{
			string owner = "probe.required." + text + "." + Guid.NewGuid().ToString("N");
			string text2 = ((text == "collision") ? "Wave" : owner);
			string text3 = ((text == "load") ? "Character" : "Process");
			string text4 = Path.Combine(workRoot, text + ".pmod");
			string manifest = $"{{\"schemaVersion\":2,\"id\":\"{owner}\",\"name\":\"{owner}\",\"version\":\"1.0.0\",\n \"provides\":{{\"{text3}\":[\"{text2}\"]}},\"overrides\":{{\"Feature\":[\"{featureKey}\"]}}}}";
			Dictionary<string, string> dictionary = new Dictionary<string, string> { ["Battle/Features/" + featureKey + ".tres"] = validFeature };
			if (text == "load")
			{
				dictionary[$"Resources/Characters/Plants/{owner}/Scene/{owner}.tscn"] = WriteTextFile(workRoot, "unsafe-dependency.tscn", "[gd_scene format=3]\n[node name=\"Probe\" type=\"Node2D\"]\n");
				dictionary["Resources/Characters/Plants/" + owner + "/Config/unsafe.res"] = wrongType;
			}
			else if (text != "missing")
			{
				string text5 = wrongType;
				if (text != "type")
				{
					text5 = Path.Combine(workRoot, "valid-process.tres");
					using TowerDefenseBattleProcessEmpty resource = new TowerDefenseBattleProcessEmpty();
					Require(ResourceSaver.Save(resource, ProjectSettings.LocalizePath(text5), ResourceSaver.SaverFlags.None) == Error.Ok, "Could not save Process fixture.");
				}
				dictionary["Battle/Processes/" + text2 + ".tres"] = text5;
				if (text == "ambiguous")
				{
					dictionary["Battle/Processes/Duplicate/" + text2 + ".tres"] = text5;
				}
			}
			CreatePackage(text4, manifest, dictionary);
			ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(text4);
			try
			{
				bool flag2 = loadedMod != null && !ModLoader.ApplyMod(loadedMod) && !loadedMod.Applied && loadedMod.AppliedResourceCount == 0 && !ModLoader.GetLoadedMods().Contains(loadedMod) && !XWModRuntimeRegistry.GetRegistrationStack("Feature", featureKey).Any((XWModRuntimeRegistry.Registration item) => item.OwnerMod == owner) && !XWModRuntimeRegistry.GetRegistrationStack(text3, text2).Any((XWModRuntimeRegistry.Registration item) => item.OwnerMod == owner) && TowerDefenseBattleRegistry.BattleFeatureDictionary[featureKey] == towerDefenseBattleFeature && loadedMod.Diagnostics.Count > 0;
				Require(flag2, "Required resource rollback failed for " + text);
				flag = flag && flag2;
			}
			finally
			{
				ModLoader.UnloadMod(owner);
				XWModRuntimeRegistry.UnregisterOwner(owner);
			}
		}
		return flag;
	}

	private bool CheckLevelCatalogDependency(string workRoot)
	{
		string text = "probe.catalog." + Guid.NewGuid().ToString("N");
		string path = Path.Combine(workRoot, "catalog-source");
		string text2 = Path.Combine(path, "Resources", "Levels", "level.tres");
		string text3 = Path.Combine(path, "Resources", "LevelCatalogs", text + ".tres");
		Directory.CreateDirectory(Path.GetDirectoryName(text2));
		Directory.CreateDirectory(Path.GetDirectoryName(text3));
		using TowerDefenseLevelNewConfig resource = new TowerDefenseLevelNewConfig
		{
			ResourceName = "packaged-new-level",
			name = "packaged-new-level"
		};
		Require(ResourceSaver.Save(resource, ProjectSettings.LocalizePath(text2), ResourceSaver.SaverFlags.ChangePath) == Error.Ok, "Could not save level dependency.");
		File.WriteAllText(text3, "[gd_resource type=\"Resource\" load_steps=7 format=3]\n[ext_resource type=\"Script\" path=\"res://Resource/Level/LevelCatalogConfig.cs\" id=\"1_catalog\"]\n[ext_resource type=\"Script\" path=\"res://Resource/Level/LevelChapterConfig.cs\" id=\"2_chapter\"]\n[ext_resource type=\"Script\" path=\"res://Resource/Level/LevelChooseConfig.cs\" id=\"3_choose\"]\n[ext_resource type=\"Resource\" path=\"../Levels/level.tres\" id=\"4_level\"]\n[sub_resource type=\"Resource\" id=\"Choice\"]\nscript = ExtResource(\"3_choose\")\nsaveKey = \"packaged-new-level\"\nnormalLevel = ExtResource(\"4_level\")\n[sub_resource type=\"Resource\" id=\"Chapter\"]\nscript = ExtResource(\"2_chapter\")\nlevelList = Array[ExtResource(\"3_choose\")]([SubResource(\"Choice\")])\n[resource]\nscript = ExtResource(\"1_catalog\")\ncatalogKey = \"" + text + "\"\nchapterList = Array[ExtResource(\"2_chapter\")]([SubResource(\"Chapter\")])");
		string text4 = Path.Combine(workRoot, "catalog.pmod");
		CreatePackage(text4, $"{{\"schemaVersion\":2,\"id\":\"{text}\",\"name\":\"{text}\",\"version\":\"1.0.0\",\n \"provides\":{{\"Level\":[\"{text}\"]}}}}", new Dictionary<string, string>
		{
			["Resources/LevelCatalogs/" + text + ".tres"] = text3,
			["Resources/Levels/level.tres"] = text2
		});
		ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(text4);
		try
		{
			if (loadedMod == null || !ModLoader.ApplyMod(loadedMod) || loadedMod.AppliedResourceCount != 1 || !ResourceManager.Instance.LEVELS.TryGetValue(text, out var value))
			{
				return false;
			}
			string text5 = value["Chapter"].AsGodotArray()[0].AsGodotDictionary()["Level"].AsGodotArray()[0].AsGodotDictionary()["Level"].AsGodotDictionary()["Normal"].AsString();
			TowerDefenseLevelNewConfig towerDefenseLevelNewConfig = ResourceLoader.Load<TowerDefenseLevelNewConfig>(text5, null, ResourceLoader.CacheMode.Reuse);
			bool flag = towerDefenseLevelNewConfig?.ResourceName == "packaged-new-level" && Path.GetFullPath(ProjectSettings.GlobalizePath(text5)).Equals(Path.GetFullPath(Path.Combine(loadedMod.ExtractRoot, "Resources", "Levels", "level.tres")), StringComparison.OrdinalIgnoreCase) && !loadedMod.Diagnostics.Any((string item) => item.Contains("Resources/Levels/level.tres", StringComparison.Ordinal));
			Require(flag, $"Catalog resolved an unexpected dependency: path={text5}; resource={towerDefenseLevelNewConfig?.ResourceName}; root={loadedMod.ExtractRoot}");
			return ModLoader.UnloadMod(text) && flag && !ResourceManager.Instance.LEVELS.ContainsKey(text);
		}
		finally
		{
			ModLoader.UnloadMod(text);
		}
	}

	private static bool CheckOptionalResourceFallback(string workRoot, string featureSource)
	{
		bool flag = true;
		bool[] array = new bool[2] { false, true };
		foreach (bool corruptAssembly in array)
		{
			string text = "probe.optional." + Guid.NewGuid().ToString("N");
			string text2 = text.ToUpperInvariant();
			string text3 = Path.Combine(workRoot, text + ".pmod");
			string manifest = $"{{\"schemaVersion\":2,\"id\":\"{text}\",\"name\":\"{text}\",\"version\":\"1.0.0\",\n \"runtimeAssembly\":\"Runtime/ModAssembly.dll\",\"runtimeEntryType\":\"Missing.Entry\",\n \"runtimeApiVersion\":1,\"runtimeAssemblyPolicy\":\"optional\",\"provides\":{{\"Feature\":[\"{text}\"]}}}}";
			Dictionary<string, string> dictionary = new Dictionary<string, string>
			{
				["Battle/Features/" + text2 + ".tres"] = featureSource,
				["Battle/Features/A/Unused.tres"] = featureSource,
				["Battle/Features/B/Unused.tres"] = featureSource
			};
			if (corruptAssembly)
			{
				dictionary["Runtime/ModAssembly.dll"] = WriteTextFile(workRoot, "corrupt.dll", "invalid assembly bytes");
			}
			CreatePackage(text3, manifest, dictionary);
			ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(text3);
			try
			{
				flag &= loadedMod != null && ModLoader.ApplyMod(loadedMod) && loadedMod.AppliedResourceCount == 1 && !loadedMod.RuntimeEntryInitialized && TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey(text2) && loadedMod.Diagnostics.Any((string item) => item.Contains(corruptAssembly ? "程序集加载失败" : "程序集不存在", StringComparison.Ordinal));
				flag &= ModLoader.UnloadMod(text) && !TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey(text2);
			}
			finally
			{
				ModLoader.UnloadMod(text);
			}
		}
		return flag;
	}

	private static string WriteTextFile(string root, string name, string content)
	{
		string text = Path.Combine(root, name);
		File.WriteAllText(text, content);
		return text;
	}

	private static void WriteProbeWav(string path)
	{
		byte[] array = new byte[64];
		using FileStream output = new FileStream(path, FileMode.Create, System.IO.FileAccess.Write);
		using BinaryWriter binaryWriter = new BinaryWriter(output);
		binaryWriter.Write(Encoding.ASCII.GetBytes("RIFF"));
		binaryWriter.Write(36 + array.Length);
		binaryWriter.Write(Encoding.ASCII.GetBytes("WAVE"));
		binaryWriter.Write(Encoding.ASCII.GetBytes("fmt "));
		binaryWriter.Write(16);
		binaryWriter.Write((short)1);
		binaryWriter.Write((short)1);
		binaryWriter.Write(8000);
		binaryWriter.Write(16000);
		binaryWriter.Write((short)2);
		binaryWriter.Write((short)16);
		binaryWriter.Write(Encoding.ASCII.GetBytes("data"));
		binaryWriter.Write(array.Length);
		binaryWriter.Write(array);
	}

	private static void CreatePackage(string packagePath, string manifest, Dictionary<string, string> entries)
	{
		using FileStream stream = new FileStream(packagePath, FileMode.Create);
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
		using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mod.json").Open()))
		{
			streamWriter.Write(manifest);
		}
		foreach (KeyValuePair<string, string> entry in entries)
		{
			using Stream destination = zipArchive.CreateEntry(entry.Key).Open();
			using FileStream fileStream = File.OpenRead(entry.Value);
			fileStream.CopyTo(destination);
		}
	}

	private async Task<(bool DefaultDisabled, bool Topology, bool Lifecycle, bool Rollback, bool Dependency, bool CallbackOnly, bool ExportSafety)> RunManagedLifecycleProbe(string workRoot)
	{
		string catalogRoot = Path.Combine(workRoot, "ManagedLifecycleMods");
		Directory.CreateDirectory(catalogRoot);
		CreatePackage(Path.Combine(catalogRoot, "base.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.base\",\"name\":\"Base\",\"version\":\"1.0.0\"}", new Dictionary<string, string>());
		CreatePackage(Path.Combine(catalogRoot, "child.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.child\",\"name\":\"Child\",\"version\":\"1.0.0\",\"dependencies\":[{\"id\":\"probe.base\",\"version\":\"1.0.0\"}]}", new Dictionary<string, string>());
		CreatePackage(Path.Combine(catalogRoot, "disabled-duplicate-a.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.disabled.duplicate\",\"name\":\"Duplicate A\",\"version\":\"1.0.0\"}", new Dictionary<string, string>());
		CreatePackage(Path.Combine(catalogRoot, "disabled-duplicate-b.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.disabled.duplicate\",\"name\":\"Duplicate B\",\"version\":\"1.0.0\"}", new Dictionary<string, string>());
		XWModManager catalog = new XWModManager(catalogRoot);
		bool defaultDisabled = catalog.LoadEnabledIds().Count == 0 && catalog.ScanMods().All((XWModManager.ModEntry entry) => !entry.Enabled) && catalog.ResolveEnabledMods(catalog.LoadEnabledIds()).LoadOrder.Count == 0;
		catalog.SaveEnabledIds(new string[2] { "probe.child", "probe.base" });
		XWModManager.ResolveEnabledResult resolveEnabledResult = catalog.ResolveEnabledMods(catalog.LoadEnabledIds());
		bool flag = resolveEnabledResult.Issues.Count == 0 && resolveEnabledResult.LoadOrder.Select((XWModManager.ModEntry entry) => entry.Id).SequenceEqual<string>(new string[2] { "probe.base", "probe.child" }, StringComparer.OrdinalIgnoreCase);
		XWModManager.SetEnabledResult setEnabledResult = catalog.TrySetEnabled("probe.base", enabled: false);
		flag &= !setEnabledResult.Success && setEnabledResult.Blocked && catalog.LoadEnabledIds().Contains<string>("probe.base", StringComparer.OrdinalIgnoreCase);
		CreatePackage(Path.Combine(catalogRoot, "cycle-a.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.cycle.a\",\"name\":\"Cycle A\",\"version\":\"1.0.0\",\"dependencies\":[{\"id\":\"probe.cycle.b\"}]}", new Dictionary<string, string>());
		CreatePackage(Path.Combine(catalogRoot, "cycle-b.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.cycle.b\",\"name\":\"Cycle B\",\"version\":\"1.0.0\",\"dependencies\":[{\"id\":\"probe.cycle.a\"}]}", new Dictionary<string, string>());
		catalog.SaveEnabledIds(new string[2] { "probe.cycle.a", "probe.cycle.b" });
		XWModManager.ResolveEnabledResult resolveEnabledResult2 = catalog.ResolveEnabledMods(catalog.LoadEnabledIds());
		bool topology = flag && resolveEnabledResult2.LoadOrder.Count == 0 && resolveEnabledResult2.Issues.Count((XWValidationIssue issue) => issue.Code == XWValidationIssue.IssueCode.DependencyCycle) == 2;
		(string, string, string) tuple = await BuildManagedLifecycleFixture(workRoot);
		string entryAssembly = tuple.Item1;
		string dependencyAssembly = tuple.Item2;
		string componentAssembly = tuple.Item3;
		string text = Path.Combine(workRoot, "ManagedExportProject");
		string outputDir = Path.Combine(workRoot, "ManagedExportOutput");
		Directory.CreateDirectory(text);
		Directory.CreateDirectory(Path.Combine(text, "Runtime"));
		Directory.CreateDirectory(Path.Combine(text, "Runtime", "Dependencies"));
		File.Copy(entryAssembly, Path.Combine(text, "Runtime", "ModAssembly.dll"), overwrite: true);
		File.Copy(dependencyAssembly, Path.Combine(text, "Runtime", "Dependencies", "ManagedLifecycleDependency.dll"), overwrite: true);
		File.WriteAllText(Path.Combine(text, "mod.json"), "{\"schemaVersion\":2,\"id\":\"probe.export.dependencies\",\"name\":\"Export Dependencies\",\"version\":\"1.0.0\"}");
		bool flag2;
		using (ZipArchive zipArchive = ZipFile.OpenRead(ModExporter.ExportFromDirectory("managed-export-probe", new ModExporter.ModInfo
		{
			Name = "Managed Export Probe",
			Version = "1.0.0"
		}, text, new List<string> { "Runtime/ModAssembly.dll", "Runtime/Dependencies/ManagedLifecycleDependency.dll" }, outputDir, entryAssembly)))
		{
			flag2 = zipArchive.Entries.Count((ZipArchiveEntry entry) => string.Equals(entry.FullName, "Runtime/ModAssembly.dll", StringComparison.OrdinalIgnoreCase)) == 1 && zipArchive.Entries.Count((ZipArchiveEntry entry) => string.Equals(entry.FullName, "Runtime/Dependencies/ManagedLifecycleDependency.dll", StringComparison.OrdinalIgnoreCase)) == 1;
		}
		bool flag3 = false;
		try
		{
			ModExporter.ExportFromDirectory("managed-export-traversal-probe", new ModExporter.ModInfo
			{
				Version = "1.0.0"
			}, text, new List<string> { "../outside.txt" }, outputDir);
		}
		catch (InvalidDataException)
		{
			flag3 = true;
		}
		bool flag4 = false;
		try
		{
			ModExporter.ExportFromDirectory("../escaped-package", new ModExporter.ModInfo
			{
				Version = "1.0.0"
			}, text, new List<string>(), outputDir);
		}
		catch (InvalidDataException)
		{
			flag4 = true;
		}
		bool exportSafety = flag2 && flag3 && flag4;
		foreach (string item in Directory.EnumerateFiles(catalogRoot))
		{
			File.Delete(item);
		}
		string workingManifest = "{\"schemaVersion\":2,\"id\":\"probe.managed.lifecycle\",\"name\":\"Managed Lifecycle\",\"version\":\"1.0.0\",\"runtimeAssembly\":\"Runtime/ModAssembly.dll\",\"runtimeEntryType\":\"ManagedLifecycleFixture.WorkingEntry\",\"provides\":{\"Feature\":[\"probe.managed.lifecycle.FEATURE\"]},\"runtimeApiVersion\":1,\"runtimeAssemblyPolicy\":\"required\"}";
		string[] array = new string[4]
		{
			workingManifest.Replace("\"runtimeApiVersion\":1,", ""),
			workingManifest.Replace("\"runtimeApiVersion\":1", "\"runtimeApiVersion\":99"),
			workingManifest.Replace("\"schemaVersion\":2", "\"schemaVersion\":1"),
			workingManifest.Replace("\"runtimeAssemblyPolicy\":\"required\"", "\"runtimeAssemblyPolicy\":\"\"")
		};
		foreach (string text2 in array)
		{
			bool[] array2 = new bool[2] { false, true };
			foreach (bool flag5 in array2)
			{
				string text3 = Path.Combine(workRoot, "incompatible.pmod");
				CreatePackage(text3, flag5 ? text2.Replace("\"runtimeEntryType\":\"ManagedLifecycleFixture.WorkingEntry\",", "") : text2, new Dictionary<string, string> { ["Runtime/ModAssembly.dll"] = entryAssembly });
				ModRuntimeLifecycleProbeRecorder.Reset();
				Require(!ModLoader.ValidatePackageArchive(text3, out var _, out var _, out var diagnostic) && diagnostic.Contains("重新", StringComparison.Ordinal) && ModLoader.LoadMod(text3) == null && ModRuntimeLifecycleProbeRecorder.Snapshot().Length == 0, "An incompatible managed package reached assembly execution or lacked a rebuild diagnostic.");
			}
		}
		CreatePackage(Path.Combine(catalogRoot, "managed.pmod"), workingManifest, new Dictionary<string, string>
		{
			["Runtime/ModAssembly.dll"] = entryAssembly,
			["Runtime/Dependencies/ManagedLifecycleDependency.dll"] = dependencyAssembly
		});
		catalog = new XWModManager(catalogRoot);
		catalog.SaveEnabledIds(new string[1] { "probe.managed.lifecycle" });
		ModRuntimeLifecycleProbeRecorder.Reset();
		int num3 = catalog.LoadEnabledModsWithResult();
		ModLoader.LoadedMod loadedMod = ModLoader.GetLoadedMods().SingleOrDefault();
		if (loadedMod == null)
		{
			throw new InvalidOperationException("Managed lifecycle package did not load; see ModManager diagnostics.");
		}
		string[] array3 = ModRuntimeLifecycleProbeRecorder.Snapshot();
		bool dependency = flag2 && array3.Contains("initialize:probe.managed.lifecycle:private-dependency");
		bool lifecycle = num3 == 1 && loadedMod != null && loadedMod.AppliedResourceCount == 0 && loadedMod.RuntimeEntryInitialized && XWModRuntimeRegistry.GetRegistrationStack("Feature", "probe.managed.lifecycle.feature").Any((XWModRuntimeRegistry.Registration item) => item.OwnerMod == "probe.managed.lifecycle" && !item.IsOverride) && array3.SequenceEqual(new string[2] { "initialize:probe.managed.lifecycle:private-dependency", "all" });
		Node node = new Node
		{
			Name = "ManagedCompanionProbe"
		};
		node.SetMeta("mod_character_script_binding", "CompanionOnly");
		node.SetMeta("mod_character_script_path", "WorkingNode.cs");
		PackedScene packedScene = new PackedScene();
		Error error = packedScene.Pack(node);
		node.Free();
		Node liveCompanion = null;
		string diagnostic2;
		bool companionCreated = error == Error.Ok && loadedMod.CharacterCompanionRuntime.TryCreateInstance(packedScene, out liveCompanion, out diagnostic2);
		StateMachineUnloadBlockers blockers;
		bool liveInstanceBlocked = companionCreated && !ModLoader.UnloadMod("probe.managed.lifecycle", out blockers) && blockers.LeaseCount > 0;
		XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.CaptureAsync();
		Require(xWModEnvironmentStatus.Ready && xWModEnvironmentStatus.Snapshot.Mods[0].EffectiveMode == "Managed", "Normal companion leases or a blocked unchanged unload invalidated the formal snapshot.");
		long generation = xWModEnvironmentStatus.Generation;
		Require(!catalog.TryUnloadAll() && XWModEnvironmentService.GetStatus().Generation == generation, "A blocked no-change batch unload changed the environment generation.");
		File.WriteAllText(catalog.EnabledStatePath, "[\"probe.managed.lifecycle\",\"new.desired.mod\"]");
		Require(catalog.LoadEnabledModsWithResult() == -1 && !XWModEnvironmentService.GetStatus().Ready, "A blocked reload kept Ready despite an externally changed enabled set.");
		catalog.SaveEnabledIds(new string[1] { "probe.managed.lifecycle" });
		liveCompanion?.Free();
		lifecycle = lifecycle && companionCreated && liveInstanceBlocked;
		catalog.UnloadAll();
		lifecycle &= ModRuntimeLifecycleProbeRecorder.Snapshot().SequenceEqual(new string[3] { "initialize:probe.managed.lifecycle:private-dependency", "all", "shutdown" });
		lifecycle &= !TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey("probe.managed.lifecycle.feature");
		string text4 = Path.Combine(workRoot, "managed-wrong-declaration.pmod");
		CreatePackage(text4, workingManifest.Replace("\"provides\"", "\"overrides\""), new Dictionary<string, string>
		{
			["Runtime/ModAssembly.dll"] = entryAssembly,
			["Runtime/Dependencies/ManagedLifecycleDependency.dll"] = dependencyAssembly
		});
		ModRuntimeLifecycleProbeRecorder.Reset();
		ModLoader.LoadedMod loadedMod2 = ModLoader.LoadMod(text4);
		lifecycle &= loadedMod2 != null && !ModLoader.ApplyMod(loadedMod2) && !loadedMod2.Applied && !ModLoader.GetLoadedMods().Contains(loadedMod2) && !TowerDefenseBattleRegistry.BattleFeatureDictionary.ContainsKey("probe.managed.lifecycle.feature") && loadedMod2.Diagnostics.Any((string item) => item.Contains("required manifest runtime entry", StringComparison.Ordinal)) && ModRuntimeLifecycleProbeRecorder.Snapshot().SequenceEqual(new string[2] { "initialize:probe.managed.lifecycle:private-dependency", "shutdown" });
		ModLoader.UnloadMod("probe.managed.lifecycle");
		CreatePackage(Path.Combine(catalogRoot, "required-parent.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.required.parent\",\"name\":\"Parent\",\"version\":\"1.0.0\",\"provides\":{\"Feature\":[\"missing-parent-feature\"]}}", new Dictionary<string, string>());
		CreatePackage(Path.Combine(catalogRoot, "required-child.pmod"), workingManifest.Replace("probe.managed.lifecycle", "probe.required.child").Replace("\"version\":\"1.0.0\",", "\"version\":\"1.0.0\",\"dependencies\":[{\"id\":\"probe.required.parent\",\"version\":\"1.0.0\"}],"), new Dictionary<string, string>
		{
			["Runtime/ModAssembly.dll"] = entryAssembly,
			["Runtime/Dependencies/ManagedLifecycleDependency.dll"] = dependencyAssembly
		});
		catalog = new XWModManager(catalogRoot);
		catalog.SaveEnabledIds(new string[2] { "probe.required.child", "probe.required.parent" });
		ModRuntimeLifecycleProbeRecorder.Reset();
		dependency &= catalog.LoadEnabledModsWithResult() == 0 && catalog.LoadedManifests.Count == 0 && ModLoader.GetLoadedMods().Count == 0 && ModRuntimeLifecycleProbeRecorder.Snapshot().Length == 0;
		catalog.UnloadAll();
		foreach (string item2 in Directory.EnumerateFiles(catalogRoot))
		{
			File.Delete(item2);
		}
		CreatePackage(Path.Combine(catalogRoot, "callbacks.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.managed.callbacks\",\"name\":\"Managed Callbacks\",\"version\":\"1.0.0\",\"runtimeAssembly\":\"Runtime/ModAssembly.dll\",\"runtimeApiVersion\":1,\"runtimeAssemblyPolicy\":\"required\"}", new Dictionary<string, string>
		{
			["Runtime/ModAssembly.dll"] = entryAssembly,
			["Runtime/Dependencies/ManagedLifecycleDependency.dll"] = dependencyAssembly
		});
		catalog = new XWModManager(catalogRoot);
		catalog.SaveEnabledIds(new string[1] { "probe.managed.callbacks" });
		int num4 = catalog.LoadEnabledModsWithResult();
		ModLoader.LoadedMod loadedMod3 = ModLoader.GetLoadedMods().SingleOrDefault();
		bool callbackOnly = num4 == 1 && loadedMod3 != null && !loadedMod3.RuntimeEntryInitialized && loadedMod3 != null && loadedMod3.AppliedResourceCount == 0 && loadedMod3 != null && loadedMod3.StateMachineCallbackCount == 1;
		catalog.UnloadAll();
		CreatePackage(Path.Combine(catalogRoot, "components.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.managed.components\",\"version\":\"1.0.0\",\"runtimeAssembly\":\"Runtime/ModAssembly.dll\",\"runtimeApiVersion\":1,\"runtimeAssemblyPolicy\":\"required\"}", new Dictionary<string, string> { ["Runtime/ModAssembly.dll"] = componentAssembly });
		catalog.SaveEnabledIds(new string[1] { "probe.managed.components" });
		Require(catalog.LoadEnabledModsWithResult() == 1, "Component-only package was not applied.");
		ModLoader.LoadedMod loadedMod4 = ModLoader.GetLoadedMods().Single();
		Require(loadedMod4.Applied && loadedMod4.CharacterComponentRuntimeCount == 4 && loadedMod4.StateMachineCallbackCount == 0 && !loadedMod4.RuntimeEntryInitialized, "Ordinary components did not count as managed applied content.");
		CharacterComponentRuntime characterComponentRuntime = CharacterComponentRuntimeTypeRegistry.Create("PackageOnlyComponent");
		Require(characterComponentRuntime != null && !catalog.TryUnloadAll(), "Constructed component did not block package unload.");
		Require(CharacterComponentRuntimeTypeRegistry.Create("PackageThrowCtor") == null && CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers("probe.managed.components").LeaseCount == 1, "Constructor failure leaked the creation lease.");
		TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter();
		ComponentManager componentManager = new ComponentManager
		{
			parent = towerDefenseCharacter
		};
		ModCharacterComponentDefinition modCharacterComponentDefinition = new ModCharacterComponentDefinition
		{
			ComponentTypeId = "PackageComponent",
			InstanceId = "failure",
			RuntimeTypeName = "PackageThrowBind"
		};
		Require(componentManager.AddRuntimeComponent(modCharacterComponentDefinition) == null && CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers("probe.managed.components").LeaseCount == 1, "Bound hook failure retained a manager component or lease.");
		modCharacterComponentDefinition.RuntimeTypeName = "PackageThrowRelease";
		CharacterComponentRuntime characterComponentRuntime2 = componentManager.AddRuntimeComponent(modCharacterComponentDefinition);
		Require(characterComponentRuntime2 != null && componentManager.RemoveRuntimeComponent(characterComponentRuntime2) && characterComponentRuntime2.IsReleased && CharacterComponentRuntimeTypeRegistry.GetUnloadBlockers("probe.managed.components").LeaseCount == 1, "Release hook failure prevented cleanup or stable instance ID reuse.");
		characterComponentRuntime.Bind(componentManager, towerDefenseCharacter, modCharacterComponentDefinition);
		characterComponentRuntime.Detach(ComponentDetachReason.TemporaryTreeExit);
		Require(!catalog.TryUnloadAll(), "Detached ordinary component allowed assembly unload.");
		characterComponentRuntime.Bind(componentManager, towerDefenseCharacter, modCharacterComponentDefinition);
		characterComponentRuntime.Release();
		componentManager.Dispose();
		towerDefenseCharacter.Dispose();
		Require(catalog.TryUnloadAll() && !CharacterComponentRuntimeTypeRegistry.Contains("PackageOnlyComponent"), "Released component did not allow package registration cleanup.");
		foreach (string item3 in Directory.EnumerateFiles(catalogRoot))
		{
			File.Delete(item3);
		}
		CreatePackage(Path.Combine(catalogRoot, "failing.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.managed.failure\",\"name\":\"Managed Failure\",\"version\":\"1.0.0\",\"runtimeAssembly\":\"Runtime/ModAssembly.dll\",\"runtimeEntryType\":\"ManagedLifecycleFixture.FailingEntry\",\"runtimeApiVersion\":1,\"runtimeAssemblyPolicy\":\"required\"}", new Dictionary<string, string>
		{
			["Runtime/ModAssembly.dll"] = entryAssembly,
			["Runtime/Dependencies/ManagedLifecycleDependency.dll"] = dependencyAssembly
		});
		catalog = new XWModManager(catalogRoot);
		catalog.SaveEnabledIds(new string[1] { "probe.managed.failure" });
		ModRuntimeLifecycleProbeRecorder.Reset();
		bool rollback = catalog.LoadEnabledModsWithResult() == 0 && ModLoader.GetLoadedMods().Count == 0 && !XWModRuntimeRegistry.GetRegistrations().Values.SelectMany((Dictionary<string, XWModRuntimeRegistry.Registration> registrations) => registrations.Values).Any((XWModRuntimeRegistry.Registration registration) => string.Equals(registration.OwnerMod, "probe.managed.failure", StringComparison.OrdinalIgnoreCase)) && ModRuntimeLifecycleProbeRecorder.Snapshot().SequenceEqual(new string[2] { "failing-initialize", "failing-shutdown" });
		XWModManager.ModEntry modEntry = new XWModManager(catalogRoot).GetManagementSnapshot().Single((XWModManager.ModEntry entry) => entry.Id == "probe.managed.failure");
		Require(!modEntry.Loaded && modEntry.EffectiveState == "加载失败" && !string.IsNullOrWhiteSpace(modEntry.LastApplyFailure) && new XWModManager(catalogRoot).GetRecentApplyFailures().Any((XWModManager.ApplyFailureRecord record) => record.ModId == "probe.managed.failure"), "Failed Apply disappeared from management after runtime rollback removed its owner.");
		catalog.UnloadAll();
		foreach (string item4 in Directory.EnumerateFiles(catalogRoot))
		{
			File.Delete(item4);
		}
		CreatePackage(Path.Combine(catalogRoot, "blocked-failure.pmod"), "{\"schemaVersion\":2,\"id\":\"probe.managed.blocked-failure\",\"name\":\"Managed Blocked Failure\",\"version\":\"1.0.0\",\"runtimeAssembly\":\"Runtime/ModAssembly.dll\",\"runtimeEntryType\":\"ManagedLifecycleFixture.BlockingFailEntry\",\"runtimeApiVersion\":1,\"runtimeAssemblyPolicy\":\"required\"}", new Dictionary<string, string>
		{
			["Runtime/ModAssembly.dll"] = entryAssembly,
			["Runtime/Dependencies/ManagedLifecycleDependency.dll"] = dependencyAssembly
		});
		catalog = new XWModManager(catalogRoot);
		catalog.SaveEnabledIds(new string[1] { "probe.managed.blocked-failure" });
		ModRuntimeLifecycleProbeRecorder.Reset();
		bool flag6 = catalog.LoadEnabledModsWithResult() == -1 && ModLoader.GetLoadedMods().Count == 1 && ModLoader.GetLoadedMods()[0].State == ModLoader.ApplicationState.PendingRollback && !ModLoader.GetLoadedMods()[0].Applied;
		if (flag6)
		{
			flag6 = !(await XWModEnvironmentService.CaptureAsync()).Ready;
		}
		bool flag7 = flag6 && catalog.LoadedManifests.ContainsKey("probe.managed.blocked-failure") && ModRuntimeLifecycleProbeRecorder.Snapshot().SequenceEqual(new string[2] { "blocking-failing-initialize", "blocking-failing-shutdown" });
		ModRuntimeLifecycleProbeRecorder.ReleaseHeld();
		bool flag8 = catalog.TryUnloadAll() && ModLoader.GetLoadedMods().Count == 0;
		rollback = rollback && flag7 && flag8;
		catalog.SaveEnabledIds(Array.Empty<string>());
		flag6 = catalog.LoadEnabledModsWithResult() == 0;
		if (flag6)
		{
			flag6 = (await XWModEnvironmentService.CaptureAsync()).Ready;
		}
		Require(flag6 && XWModEnvironmentService.GetStatus().Snapshot.Mods.Count == 0, "Formal empty environment was not explicitly Ready.");
		File.WriteAllText(catalog.EnabledStatePath, "{broken");
		flag6 = catalog.LoadEnabledModsWithResult() == -1;
		if (flag6)
		{
			flag6 = !(await XWModEnvironmentService.CaptureAsync()).Ready;
		}
		Require(flag6, "Malformed enabled JSON was treated as a valid empty environment.");
		return (DefaultDisabled: defaultDisabled, Topology: topology, Lifecycle: lifecycle, Rollback: rollback, Dependency: dependency, CallbackOnly: callbackOnly, ExportSafety: exportSafety);
	}

	private static async Task<(string EntryAssembly, string DependencyAssembly, string ComponentAssembly)> BuildManagedLifecycleFixture(string workRoot)
	{
		string fixtureRoot = Path.Combine(workRoot, "ManagedLifecycleFixture");
		string text = Path.Combine(fixtureRoot, "Dependency");
		string entryRoot = Path.Combine(fixtureRoot, "Entry");
		string outputRoot = Path.Combine(fixtureRoot, "Output");
		Directory.CreateDirectory(text);
		Directory.CreateDirectory(entryRoot);
		string text2 = ResolveHostAssemblyPath();
		string text3 = Path.Combine(Path.GetDirectoryName(text2) ?? "", "GodotSharp.dll");
		if (!File.Exists(text3))
		{
			throw new FileNotFoundException("Managed lifecycle probe could not locate GodotSharp.dll.", text3);
		}
		string text4 = typeof(ModRuntimeLifecycleProbeRecorder).Assembly.Location;
		if (!File.Exists(text4))
		{
			text4 = text2;
		}
		File.WriteAllText(Path.Combine(text, "ManagedLifecycleDependency.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework><AssemblyName>ManagedLifecycleDependency</AssemblyName></PropertyGroup></Project>");
		File.WriteAllText(Path.Combine(text, "DependencyValue.cs"), "namespace ManagedLifecycleDependency; public static class DependencyValue { public const string Value = \"private-dependency\"; }");
		File.WriteAllText(Path.Combine(entryRoot, "ManagedLifecycleFixture.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net9.0</TargetFramework><EnableDynamicLoading>true</EnableDynamicLoading></PropertyGroup><ItemGroup><ProjectReference Include=\"..\\Dependency\\ManagedLifecycleDependency.csproj\" /><Reference Include=\"PlantsVsZombies\"><HintPath>" + text2 + "</HintPath><Private>false</Private></Reference><Reference Include=\"GodotSharp\"><HintPath>" + text3 + "</HintPath><Private>false</Private></Reference>" + (string.Equals(text2, text4, StringComparison.OrdinalIgnoreCase) ? "" : ("<Reference Include=\"ProbeRecorder\"><HintPath>" + text4 + "</HintPath><Private>false</Private></Reference>")) + "</ItemGroup></Project>");
		File.WriteAllText(Path.Combine(entryRoot, "Entries.cs"), "using PVZHE.ModEditor.ModSystem; using ManagedLifecycleDependency; namespace ManagedLifecycleFixture; public sealed class WorkingEntry : IXWModRuntimeEntry { public void Initialize(XWModRuntimeContext context) { if (!context.TryRegister(\"Feature\", context.ModId + \".feature\", Godot.Variant.From(new TowerDefenseBattleFeatureCamera()), false, out string error)) throw new System.InvalidOperationException(error); ModRuntimeLifecycleProbeRecorder.Record(\"initialize:\" + context.ModId + \":\" + DependencyValue.Value); } public void OnAllModsLoaded() => ModRuntimeLifecycleProbeRecorder.Record(\"all\"); public void Shutdown() => ModRuntimeLifecycleProbeRecorder.Record(\"shutdown\"); } public sealed class FailingEntry : IXWModRuntimeEntry { public void Initialize(XWModRuntimeContext context) { ModRuntimeLifecycleProbeRecorder.Record(\"failing-initialize\"); if (!context.TryRegister(\"Feature\", context.ModId + \".feature\", Godot.Variant.From(new TowerDefenseBattleFeatureCamera()), false, out string error)) throw new System.InvalidOperationException(error); throw new System.InvalidOperationException(\"expected-initialize-failure\"); } public void OnAllModsLoaded() { } public void Shutdown() => ModRuntimeLifecycleProbeRecorder.Record(\"failing-shutdown\"); } public sealed class BlockingFailEntry : IXWModRuntimeEntry { public void Initialize(XWModRuntimeContext context) { ModRuntimeLifecycleProbeRecorder.Record(\"blocking-failing-initialize\"); if (!StateMachineCallbackKey.TryBuild(context.ModId, \"probe\", out string callbackKey, out string keyError)) throw new System.InvalidOperationException(keyError); var definition = new StateMachineDefinition { DefinitionId = \"blocking-probe\", RootStateId = \"root\" }; definition.States.Add(new StateMachineStateDefinition { StableId = \"root\", EnterCallbackKey = new Godot.StringName(callbackKey) }); if (!StateMachineCompiler.TryCompile(definition, out StateMachineProgram program, out _)) throw new System.InvalidOperationException(\"compile-failed\"); if (!StateMachineCallbackRegistry.Shared.TryAcquireBinding(context.ModId, program, new object(), out StateMachineCallbackBinding binding, out StateMachineBindingDiagnostic diagnostic)) throw new System.InvalidOperationException(diagnostic.Message); ModRuntimeLifecycleProbeRecorder.Hold(binding); throw new System.InvalidOperationException(\"expected-blocking-initialize-failure\"); } public void OnAllModsLoaded() { } public void Shutdown() => ModRuntimeLifecycleProbeRecorder.Record(\"blocking-failing-shutdown\"); } public static class CallbackOnly { [StateMachineCallback(\"probe\", StateMachineCallbackPhase.Enter)] public static void Enter(in StateMachineCallbackContext context) { } } public sealed class WorkingNode : Godot.Node { }");
		XWDotNetToolchainLocator.ToolchainInfo toolchainInfo = XWDotNetToolchainLocator.Locate();
		if (!toolchainInfo.Exists)
		{
			throw new FileNotFoundException("No .NET toolchain is available for managed lifecycle probe.", toolchainInfo.DotnetPath);
		}
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = toolchainInfo.DotnetPath,
			WorkingDirectory = entryRoot
		};
		startInfo.ArgumentList.Add("build");
		startInfo.ArgumentList.Add(Path.Combine(entryRoot, "ManagedLifecycleFixture.csproj"));
		startInfo.ArgumentList.Add("--nologo");
		startInfo.ArgumentList.Add("--configuration");
		startInfo.ArgumentList.Add("Release");
		startInfo.ArgumentList.Add("--output");
		startInfo.ArgumentList.Add(outputRoot);
		XWBuildProcessRunner.ProcessRunResult processRunResult = await XWBuildProcessRunner.RunAsync(startInfo, TimeSpan.FromMinutes(2L), CancellationToken.None);
		if (processRunResult.ExitCode != 0 || processRunResult.Cancelled || processRunResult.TimedOut || !processRunResult.ProcessTreeTerminated)
		{
			throw new InvalidOperationException("Managed lifecycle fixture build failed: " + processRunResult.StandardOutput + System.Environment.NewLine + processRunResult.StandardError);
		}
		string preservedAssembly = Path.Combine(fixtureRoot, "ManagedLifecycleFixture.dll");
		File.Copy(Path.Combine(outputRoot, "ManagedLifecycleFixture.dll"), preservedAssembly, overwrite: true);
		File.WriteAllText(Path.Combine(entryRoot, "Entries.cs"), "public sealed class PackageOnlyComponent : CharacterComponentRuntime { } public sealed class PackageThrowCtor : CharacterComponentRuntime { public PackageThrowCtor() { throw new System.Exception(\"ctor failure\"); } } public sealed class PackageThrowBind : CharacterComponentRuntime { protected override void OnBound() { throw new System.Exception(\"bind failure\"); } } public sealed class PackageThrowRelease : CharacterComponentRuntime { protected override void OnReleased() { throw new System.Exception(\"release failure\"); } }");
		processRunResult = await XWBuildProcessRunner.RunAsync(startInfo, TimeSpan.FromMinutes(2L), CancellationToken.None);
		if (processRunResult.ExitCode != 0 || processRunResult.Cancelled || processRunResult.TimedOut || !processRunResult.ProcessTreeTerminated)
		{
			throw new InvalidOperationException("Component-only fixture build failed: " + processRunResult.StandardOutput + processRunResult.StandardError);
		}
		return (EntryAssembly: preservedAssembly, DependencyAssembly: Path.Combine(outputRoot, "ManagedLifecycleDependency.dll"), ComponentAssembly: Path.Combine(outputRoot, "ManagedLifecycleFixture.dll"));
	}

	private static string ResolveHostAssemblyPath()
	{
		string[] array = new string[4]
		{
			typeof(IXWModRuntimeEntry).Assembly.Location,
			ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/PlantsVsZombies.dll"),
			ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Release/PlantsVsZombies.dll"),
			Path.Combine(AppContext.BaseDirectory, "PlantsVsZombies.dll")
		};
		foreach (string text in array)
		{
			if (!string.IsNullOrWhiteSpace(text) && File.Exists(text))
			{
				return Path.GetFullPath(text);
			}
		}
		throw new FileNotFoundException("Managed lifecycle probe could not locate PlantsVsZombies.dll.");
	}

	private async Task<PopupMenu> WaitForModMenu(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			PopupMenu popupMenu = FindNodeOfType<PopupMenu>(GetTree().Root, "模组");
			if (GodotObject.IsInstanceValid(popupMenu))
			{
				return popupMenu;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static T FindNodeOfType<T>(Node root, string name) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result && root.Name == (StringName)name)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child, name);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
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
			GD.PrintErr("[MOD_RUNTIME_PACKAGE_PROBE_FAILURE] " + message);
		}
	}

	private static async Task<bool> RunM5InstallationTransactionProbe(string workRoot)
	{
		string owner = "probe.m5.install";
		XWModManager manager = new XWModManager();
		string text = Path.Combine(workRoot, "m5-source.pmod");
		string source2 = Path.Combine(workRoot, "m5-source2.pmod");
		string text2 = Path.Combine(workRoot, "m5-dependency.pmod");
		string manifest = "{\"schemaVersion\":1,\"id\":\"" + owner + "\",\"name\":\"M5 Probe\",\"version\":\"1.0.0\"}";
		CreatePackage(text, manifest, new Dictionary<string, string>());
		CreatePackage(source2, manifest.Replace("M5 Probe", "M5 Changed"), new Dictionary<string, string>());
		CreatePackage(text2, manifest.Replace("\"version\":", "\"dependencies\":[{\"id\":\"missing.m5\"}],\"version\":"), new Dictionary<string, string>());
		try
		{
			string[] array = new string[2] { "uid://m5tx7r8k2c9v", "uid://m5prb7k4t2x9" };
			foreach (string text3 in array)
			{
				Check(ResourceUid.TextToId(text3) != -1, "invalid new UID: " + text3);
			}
			string text4 = Path.Combine(manager.ModsDirectory, ".incoming");
			Directory.CreateDirectory(text4);
			string text5 = Path.Combine(text4, Guid.NewGuid().ToString("N") + ".pmod.part");
			string path = Path.Combine(text4, "keep.pmod.part");
			File.Copy(text, text5);
			File.WriteAllText(path, "keep");
			string path2 = XWModInstallTransaction.JournalPath(manager.ModsDirectory);
			File.WriteAllText(path2, "{broken");
			XWModPackageInstaller.CleanupInstallArtifacts();
			Check(File.Exists(text5) && File.ReadAllText(path2) == "{broken", "failed recovery discarded staged evidence");
			File.Delete(path2);
			XWModPackageInstaller.CleanupInstallArtifacts();
			Check(!File.Exists(text5) && File.ReadAllText(path) == "keep", "cold startup leaked orphan stage or deleted unrelated file");
			XWModPackageInstaller.CleanupInstallArtifacts();
			File.Delete(path);
			using (XWModPackageInstaller.PreparedImport preparedImport = Prepare(text))
			{
				string stagedPath = preparedImport.StagedPath;
				preparedImport.Dispose();
				Check(!File.Exists(stagedPath) && manager.ScanMods().All((XWModManager.ModEntry e) => e.Id != owner), "cancel changed installation or leaked stage");
			}
			using (XWModPackageInstaller.PreparedImport prepared = Prepare(text))
			{
				Check(XWModPackageInstaller.Install(prepared, enableAfterInstall: false).Success, "first install");
			}
			XWModManager.ModEntry modEntry = manager.ScanMods().Single((XWModManager.ModEntry e) => e.Id == owner);
			string target = modEntry.PackagePath;
			string oldHash = Hash(target);
			string newHash = Hash(source2);
			Check(!manager.LoadEnabledIds().Contains(owner), "manual import silently enabled");
			array = new string[3] { "{broken", "null", "[null]" };
			foreach (string text6 in array)
			{
				File.WriteAllText(manager.EnabledStatePath, text6);
				bool[] array2 = new bool[2] { true, false };
				foreach (bool enabled in array2)
				{
					XWModManager.SetEnabledResult setEnabledResult = manager.TrySetEnabled(owner, enabled);
					Check(!setEnabledResult.Success && setEnabledResult.Blocked && setEnabledResult.Issues.Count > 0 && File.ReadAllText(manager.EnabledStatePath) == text6 && Hash(target) == oldHash, "enable mutation overwrote corrupt state or package");
				}
			}
			manager.SaveEnabledIds(Array.Empty<string>());
			using (XWModPackageInstaller.PreparedImport prepared2 = Prepare(source2))
			{
				File.Copy(source2, target, overwrite: true);
				Check(!XWModPackageInstaller.Install(prepared2, enableAfterInstall: false).Success && Hash(target) == newHash, "same-version stale confirmation overwrote package");
			}
			File.Copy(text, target, overwrite: true);
			manager.SaveEnabledIds(new string[1] { owner });
			using (XWModPackageInstaller.PreparedImport prepared3 = Prepare(text2))
			{
				Check(!XWModPackageInstaller.Install(prepared3, enableAfterInstall: true).Success && Hash(target) == oldHash && manager.LoadEnabledIds().Contains(owner), "new dependency rejection damaged old package/enable state");
			}
			File.Delete(target);
			Check(manager.GetManagementSnapshot().Any((XWModManager.ModEntry e) => e.Id == owner && e.PackagePath == ""), "missing package not visible");
			using (XWModPackageInstaller.PreparedImport prepared4 = Prepare(text))
			{
				Check(XWModPackageInstaller.Install(prepared4, enableAfterInstall: false).Success, "missing package reinstall");
			}
			target = manager.ScanMods().Single((XWModManager.ModEntry e) => e.Id == owner).PackagePath;
			string text7 = "probe.m5.unrelated";
			string text8 = Path.Combine(manager.ModsDirectory, "m5-unrelated.pmod");
			CreatePackage(text8, manifest.Replace(owner, text7).Replace("\"version\":", "\"dependencies\":[{\"id\":\"missing.m5.dependency\"}],\"version\":"), new Dictionary<string, string>());
			string[] array3 = new string[2] { text7, "missing.m5.package" };
			manager.SaveEnabledIds(array3);
			Check(manager.ValidateEnableSet(array3).Any((XWValidationIssue issue) => issue.Level == XWValidationIssue.Severity.Error), "unrelated broken enabled fixture was valid");
			Check(manager.TryDeleteMod(owner, Hash(target)).Success && !File.Exists(target), "unrelated error blocked inactive package deletion");
			using (XWModPackageInstaller.PreparedImport prepared5 = Prepare(text))
			{
				Check(XWModPackageInstaller.Install(prepared5, enableAfterInstall: false).Success, "unrelated error blocked disabled fresh install");
			}
			using (XWModPackageInstaller.PreparedImport prepared6 = Prepare(source2))
			{
				Check(XWModPackageInstaller.Install(prepared6, enableAfterInstall: false).Success && Hash(target) == newHash, "unrelated error blocked disabled replacement");
			}
			Check((from id in manager.LoadEnabledIds()
				orderby id
				select id).SequenceEqual(array3.OrderBy((string id) => id)), "inactive maintenance changed unrelated enabled IDs");
			manager.SaveEnabledIds(array3.Append(owner));
			using (XWModPackageInstaller.PreparedImport prepared7 = Prepare(text))
			{
				Check(XWModPackageInstaller.Install(prepared7, enableAfterInstall: false).Success && Hash(target) == oldHash && (from id in manager.LoadEnabledIds()
					orderby id
					select id).SequenceEqual(array3.OrderBy((string id) => id)), "unrelated error blocked replacement with safe disable");
			}
			CreatePackage(text8, manifest.Replace(owner, text7).Replace("\"version\":", "\"dependencies\":[{\"id\":\"" + owner + "\"}],\"version\":"), new Dictionary<string, string>());
			manager.SaveEnabledIds(new string[2] { owner, text7 });
			using (XWModPackageInstaller.PreparedImport prepared8 = Prepare(source2))
			{
				Check(!XWModPackageInstaller.Install(prepared8, enableAfterInstall: false).Success && Hash(target) == oldHash && manager.LoadEnabledIds().Contains(owner), "inactive dependency allowed replacement to disable its required package");
			}
			Check(!manager.TryDeleteMod(owner, oldHash).Success && Hash(target) == oldHash, "inactive dependency allowed deletion of its required package");
			manager.SaveEnabledIds(Array.Empty<string>());
			File.Delete(text8);
			string path3 = XWModInstallTransaction.JournalPath(manager.ModsDirectory);
			array = new string[4] { "prepared", "package-replaced", "enabled-written", "committed" };
			foreach (string text9 in array)
			{
				File.Copy(text, target, overwrite: true);
				manager.SaveEnabledIds(new string[1] { owner });
				XWModInstallTransaction.Journal journal = Journal();
				XWModInstallTransaction.BeginInstall(manager.ModsDirectory, journal);
				if (text9 != "prepared")
				{
					File.Copy(target, journal.BackupPath);
					File.Copy(source2, target, overwrite: true);
					XWModInstallTransaction.SetPhase(manager.ModsDirectory, journal, "package-replaced");
				}
				if ((text9 == "enabled-written" || text9 == "committed") ? true : false)
				{
					manager.SaveEnabledIds(Array.Empty<string>());
				}
				if (text9 == "committed")
				{
					XWModInstallTransaction.SetPhase(manager.ModsDirectory, journal, "committed");
				}
				Check(manager.RecoverPendingStorage(out var diagnostic), "recover " + text9 + ": " + diagnostic);
				Check(manager.RecoverPendingStorage(out diagnostic), "repeat recovery " + text9);
				Check(Hash(target) == ((text9 == "committed") ? newHash : oldHash) && manager.LoadEnabledIds().Contains(owner) == (text9 != "committed") && !File.Exists(path3) && !File.Exists(journal.BackupPath), "recovery outcome " + text9);
			}
			File.Copy(text, target, overwrite: true);
			XWModInstallTransaction.Journal journal2 = Journal();
			XWModInstallTransaction.BeginInstall(manager.ModsDirectory, journal2);
			File.Copy(target, journal2.BackupPath);
			File.Copy(source2, target, overwrite: true);
			XWModInstallTransaction.SetPhase(manager.ModsDirectory, journal2, "committed");
			string diagnostic2;
			using (new FileStream(journal2.BackupPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.None))
			{
				Check(!manager.RecoverPendingStorage(out diagnostic2) && File.Exists(path3) && Hash(target) == newHash, "cleanup failure lost committed journal or rolled back new package");
			}
			Check(manager.RecoverPendingStorage(out diagnostic2) && !File.Exists(path3), "committed cleanup retry");
			File.Copy(text, target, overwrite: true);
			manager.SaveEnabledIds(new string[1] { owner });
			XWModInstallTransaction.Journal journal3 = Journal();
			journal3.BackupPath = target + ".delete-backup-" + Guid.NewGuid().ToString("N");
			XWModInstallTransaction.BeginDelete(manager.ModsDirectory, journal3);
			File.Move(target, journal3.BackupPath);
			manager.SaveEnabledIds(Array.Empty<string>());
			XWModInstallTransaction.SetPhase(manager.ModsDirectory, journal3, "package-moved");
			Check(manager.RecoverPendingStorage(out diagnostic2) && Hash(target) == oldHash && manager.LoadEnabledIds().Contains(owner), "interrupted deletion did not restore package and enable state");
			File.Copy(text, target, overwrite: true);
			array = new string[4] { "missing-both", "backup-hash", "null-bool", "unknown-phase" };
			foreach (string text10 in array)
			{
				File.Copy(text, target, overwrite: true);
				manager.SaveEnabledIds(new string[1] { owner });
				XWModInstallTransaction.Journal journal4 = Journal();
				XWModInstallTransaction.BeginInstall(manager.ModsDirectory, journal4);
				if (text10 == "missing-both")
				{
					File.Delete(target);
				}
				if (text10 == "backup-hash")
				{
					File.Copy(source2, journal4.BackupPath);
				}
				if ((text10 == "null-bool" || text10 == "unknown-phase") ? true : false)
				{
					JsonNode jsonNode = JsonNode.Parse(File.ReadAllText(path3));
					if (text10 == "null-bool")
					{
						jsonNode["WasEnabled"] = null;
					}
					else
					{
						jsonNode["Phase"] = "unknown";
					}
					File.WriteAllText(path3, jsonNode.ToJsonString());
				}
				Check(!manager.RecoverPendingStorage(out diagnostic2) && File.Exists(path3), "unsafe recovery accepted " + text10);
				File.Delete(path3);
				if (File.Exists(journal4.BackupPath))
				{
					File.Delete(journal4.BackupPath);
				}
			}
			File.Copy(text, target, overwrite: true);
			XWModInstallTransaction.Journal journal5 = Journal();
			XWModInstallTransaction.BeginInstall(manager.ModsDirectory, journal5);
			XWModInstallTransaction.SetPhase(manager.ModsDirectory, journal5, "package-replaced");
			manager.SaveEnabledIds(Array.Empty<string>());
			Check(manager.RecoverPendingStorage(out diagnostic2) && manager.LoadEnabledIds().Contains(owner), "resume after old package restored");
			string path4 = XWModInstallTransaction.PendingDeletePath(manager.ModsDirectory);
			File.WriteAllText(path4, "{broken");
			Check(!manager.TrySetEnabled(owner, enabled: true).Success && manager.ResolveEnabledMods(new string[1] { owner }).LoadOrder.Count == 0 && !manager.RecoverPendingStorage(out diagnostic2), "corrupt deletion queue did not fail closed");
			Check(manager.TrySetEnabled(owner, enabled: false).Success && !manager.LoadEnabledIds().Contains(owner) && File.ReadAllText(path4) == "{broken" && Hash(target) == oldHash, "corrupt deletion queue blocked safe disable or was modified");
			File.Delete(path4);
			XWModInstallTransaction.AddPendingDelete(manager.ModsDirectory, new XWModInstallTransaction.PendingDelete
			{
				ModId = owner,
				PackagePath = target,
				PackageSha256 = newHash
			});
			Check(!manager.TrySetEnabled(owner, enabled: true).Success && !manager.RecoverPendingStorage(out diagnostic2) && Hash(target) == oldHash && File.Exists(path4), "changed pending target was deleted/enabled");
			XWModInstallTransaction.RemovePendingDelete(manager.ModsDirectory, owner);
			manager.SaveEnabledIds(Array.Empty<string>());
			using (XWModPackageInstaller.PreparedImport prepared9 = Prepare(source2))
			{
				XWModInstallTransaction.AddPendingDelete(manager.ModsDirectory, new XWModInstallTransaction.PendingDelete
				{
					ModId = owner.ToUpperInvariant(),
					PackagePath = target,
					PackageSha256 = oldHash
				});
				string text11 = File.ReadAllText(path4);
				string path5 = Path.Combine(manager.ModsDirectory, ".incoming");
				string[] second = (from result in Directory.GetFiles(path5)
					orderby result
					select result).ToArray();
				Check(!XWModPackageInstaller.TryPrepare(source2, "", owner, out var prepared10, out var diagnostic3) && prepared10 == null && diagnostic3.Contains("等待删除") && (from result in Directory.GetFiles(path5)
					orderby result
					select result).SequenceEqual(second), "pending delete preparation accepted or leaked staging");
				XWModPackageInstaller.InstallResult installResult = XWModPackageInstaller.Install(prepared9, enableAfterInstall: false);
				Check(!installResult.Success && installResult.Message.Contains("等待删除") && Hash(target) == oldHash && File.ReadAllText(path4) == text11 && manager.LoadEnabledIds().Count == 0 && !File.Exists(XWModInstallTransaction.JournalPath(manager.ModsDirectory)), "pending delete replacement changed package, queue or enable state");
			}
			Check(manager.RecoverPendingStorage(out diagnostic2) && !File.Exists(target) && !File.Exists(path4), "blocked replacement prevented queued deletion recovery");
			using (XWModPackageInstaller.PreparedImport prepared11 = Prepare(text))
			{
				Check(XWModPackageInstaller.Install(prepared11, enableAfterInstall: false).Success && Hash(target) == oldHash, "reinstall after pending deletion recovery failed");
			}
			manager.SaveEnabledIds(new string[1] { owner });
			using (XWModPackageInstaller.PreparedImport prepared12 = Prepare(source2))
			{
				string requestId = Guid.NewGuid().ToString();
				AndroidModInstallIntent.SavePendingReceipt(requestId, prepared12);
				Check(AndroidModInstallIntent.CheckReceipt(requestId, prepared12) == "retry", "pre-journal interruption cannot retry");
				XWModInstallTransaction.Journal journal6 = Journal(requestId);
				XWModInstallTransaction.BeginInstall(manager.ModsDirectory, journal6);
				File.Copy(text, journal6.BackupPath);
				File.Copy(source2, target, overwrite: true);
				XWModInstallTransaction.SetPhase(manager.ModsDirectory, journal6, "committed");
				Check(manager.RecoverPendingStorage(out diagnostic2), "committed receipt recovery");
				Check(AndroidModInstallIntent.CheckReceipt(requestId, prepared12) == "completed", "receipt not completed by committed transaction");
				using XWModPackageInstaller.PreparedImport prepared13 = Prepare(text);
				bool condition = false;
				try
				{
					AndroidModInstallIntent.CheckReceipt(requestId, prepared13);
				}
				catch (InvalidDataException)
				{
					condition = true;
				}
				Check(condition, "request ID reused with different hash");
			}
			string featurePath = Path.Combine(workRoot, "m5-feature.tres");
			Check(ResourceSaver.Save(new TowerDefenseBattleFeature(), featurePath, ResourceSaver.SaverFlags.None) == Error.Ok, "save feature fixture");
			CreatePackage(target, manifest.Replace("\"version\":", "\"resources\":[\"Battle/Features/m5_base.tres\"],\"version\":"), new Dictionary<string, string> { ["Battle/Features/m5_base.tres"] = featurePath });
			manager.SaveEnabledIds(new string[1] { owner });
			Check(manager.LoadEnabledModsWithResult() == 1, "apply active base fixture");
			string failedOwner = "probe.m5.missing-dependency";
			string text12 = Path.Combine(manager.ModsDirectory, failedOwner + ".pmod");
			CreatePackage(text12, "{\"schemaVersion\":1,\"id\":\"" + failedOwner + "\",\"name\":\"Missing dependency\",\"version\":\"1.0.0\",\"dependencies\":[{\"id\":\"not-installed\"}]}", new Dictionary<string, string>());
			manager.SaveEnabledIds(new string[2] { owner, failedOwner });
			XWModStartupReport xWModStartupReport = manager.LoadEnabledModsDetailed();
			Check(xWModStartupReport.LoadedIds.Contains(owner) && !xWModStartupReport.LoadedIds.Contains(failedOwner) && xWModStartupReport.FailedById.ContainsKey(failedOwner) && xWModStartupReport.HasFailures, "startup report confuses enabled and actually loaded packages");
			Check(manager.TrySetEnabled(failedOwner, enabled: false).Success, "disable failed package");
			Check(manager.ScanMods().Single((XWModManager.ModEntry entry) => entry.Id == failedOwner).EffectiveState == "未启用", "historical failure masks successful disable");
			File.Delete(text12);
			Check(manager.LoadEnabledModsWithResult() == 1, "reload valid package after failed package disabled");
			ModLoader.LoadedMod active = ModLoader.GetLoadedMods().Single((ModLoader.LoadedMod mod) => mod.LoadedId == owner);
			string activeHash = Hash(target);
			XWModEnvironmentStatus obj = await XWModEnvironmentService.CaptureAsync();
			Check(obj.Ready, "active base environment not Ready");
			using (XWModEnvironmentService.EnvironmentLease environmentLease = XWModEnvironmentService.TryAcquireEnvironment(obj.Generation))
			{
				using XWModPackageInstaller.PreparedImport prepared14 = Prepare(source2);
				Check(environmentLease != null, "network lease acquisition");
				Check(!XWModPackageInstaller.Install(prepared14, enableAfterInstall: false).Success && active == ModLoader.GetLoadedMods().Single((ModLoader.LoadedMod mod) => mod.LoadedId == owner) && Hash(target) == activeHash, "network rejection mutated active package");
			}
			string text13 = "probe.m5.child";
			string text14 = Path.Combine(manager.ModsDirectory, "m5-child.pmod");
			CreatePackage(text14, manifest.Replace(owner, text13).Replace("\"version\":", "\"dependencies\":[{\"id\":\"" + owner + "\"}],\"resources\":[\"Battle/Features/m5_child.tres\"],\"version\":"), new Dictionary<string, string> { ["Battle/Features/m5_child.tres"] = featurePath });
			manager.SaveEnabledIds(new string[2] { owner, text13 });
			Check(manager.LoadEnabledModsWithResult() == 2, "apply dependent fixture");
			active = ModLoader.GetLoadedMods().Single((ModLoader.LoadedMod mod) => mod.LoadedId == owner);
			using (XWModPackageInstaller.PreparedImport prepared15 = Prepare(source2))
			{
				Check(!XWModPackageInstaller.Install(prepared15, enableAfterInstall: false).Success && active == ModLoader.GetLoadedMods().Single((ModLoader.LoadedMod mod) => mod.LoadedId == owner) && Hash(target) == activeHash, "dependency rejection unloaded old runtime");
			}
			Check(!manager.TryDeleteMod(owner, activeHash).Success && Hash(target) == activeHash, "delete accepted an enabled dependent");
			string path6 = XWModInstallTransaction.PendingDeletePath(manager.ModsDirectory);
			File.WriteAllText(path6, "{broken");
			Check(!manager.TrySetEnabled(owner, enabled: false).Success && manager.LoadEnabledIds().Contains(owner) && File.ReadAllText(path6) == "{broken", "corrupt queue bypassed actual dependent protection");
			File.Delete(path6);
			Check(manager.TryUnloadAll(), "fixture unload");
			manager.SaveEnabledIds(new string[1] { owner });
			File.Delete(text14);
			string path7 = ProjectSettings.GlobalizePath(XWModPlayerProgressService.ProgressPath(new XWModLevelIdentity(owner, "m5", "preserved", "Normal")));
			Directory.CreateDirectory(Path.GetDirectoryName(path7));
			File.WriteAllText(path7, "keep");
			Check(manager.TryDeleteMod(owner, Hash(target)).Success && !File.Exists(target) && File.ReadAllText(path7) == "keep", "delete damaged progress or left installation");
			GD.Print("[M5_INSTALL_PROBE] cancellation, replacement, dependency, recovery, pending, receipts, progress=True");
			return true;
			XWModInstallTransaction.Journal Journal(string requestId2 = "")
			{
				return new XWModInstallTransaction.Journal
				{
					ModId = owner,
					TargetPath = target,
					BackupPath = target + ".backup-" + Guid.NewGuid().ToString("N"),
					PreviousSha256 = oldHash,
					NewSha256 = newHash,
					HadPreviousPackage = true,
					WasEnabled = true,
					DesiredEnabled = false,
					RequestId = requestId2
				};
			}
		}
		catch (Exception ex2)
		{
			GD.PrintErr("[M5_INSTALL_PROBE_FAILURE] " + ex2.GetBaseException().Message);
			return false;
		}
		static void Check(bool flag, string name)
		{
			if (!flag)
			{
				throw new InvalidOperationException(name);
			}
		}
		static string Hash(string text15)
		{
			Check(XWModInstallTransaction.TryHash(text15, out var sha), "hash: " + text15);
			return sha;
		}
		XWModPackageInstaller.PreparedImport Prepare(string sourcePath)
		{
			Check(XWModPackageInstaller.TryPrepare(sourcePath, "", owner, out var prepared16, out var diagnostic4), "prepare: " + diagnostic4);
			return prepared16;
		}
	}
}
