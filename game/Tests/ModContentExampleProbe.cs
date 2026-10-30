using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Tools;

public partial class ModContentExampleProbe : Node
{
	public override async void _Ready()
	{
		bool passed = false;
		string owner = "";
		try
		{
			await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
			string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
			string output = ((cmdlineUserArgs.Length != 0) ? cmdlineUserArgs[0] : ProjectSettings.GlobalizePath("user://M4/Export"));
			string pmodPath = Path.Combine(output, "ContentDemo.pmod");
			if (cmdlineUserArgs.Length < 2 || cmdlineUserArgs[1] != "cold")
			{
				string parentDir = ProjectSettings.GlobalizePath("user://M4");
				ModProject project = ModProject.Create(parentDir, "ContentDemo", "1.0.0", "PVZHE", "新植物、新僵尸和连续两关");
				Require(project != null, "create authoring project");
				CreateCharacter(project.ProjectPath, "Plants", "new-character-plant", "M4Sprout", "Mod 幼苗");
				CreateCharacter(project.ProjectPath, "Zombies", "new-character-zombie", "M4Walker", "Mod 行者");
				LevelChapterConfig levelChapterConfig = new LevelChapterConfig
				{
					chapterName = "Mod 内容示例",
					preOpen = 1,
					unlockImage = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Chapter1.png"),
					background = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Chapter1Background.jpg"),
					building = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Chapter1Building.png")
				};
				for (int i = 1; i <= 2; i++)
				{
					string text = "M4Level" + i;
					TowerDefenseLevelNewConfig towerDefenseLevelNewConfig = XWNewLevelResourceDefaults.Create(text, "Mod 示例 " + i);
					Dictionary[] array = new Dictionary[2]
					{
						towerDefenseLevelNewConfig.featureData["Wave"],
						towerDefenseLevelNewConfig.processData
					};
					foreach (Dictionary obj in array)
					{
						obj["BeginCol"] = 3.0;
						obj["FlagZombieUse"] = false;
						obj["Wave"].AsGodotArray()[0].AsGodotDictionary()["Spawn"].AsGodotArray()[0].AsGodotDictionary()["Zombie"] = "M4Walker";
					}
					string path = Path.Combine(project.ProjectPath, "Resources", "Levels", text + ".tres");
					Save(towerDefenseLevelNewConfig, path, project.ProjectPath);
					TowerDefenseLevelNewConfig towerDefenseLevelNewConfig2 = ResourceLoader.Load<TowerDefenseLevelNewConfig>(path, null, ResourceLoader.CacheMode.Reuse);
					Require(towerDefenseLevelNewConfig2?.name == text, "authored modern level save/reload");
					levelChapterConfig.levelList.Add(new LevelChooseConfig
					{
						saveKey = text,
						openKey = ((i == 1) ? "" : "M4Level1"),
						normalLevel = towerDefenseLevelNewConfig2,
						unlockImage = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Adventure_LEVEL1-" + i + ".png")
					});
				}
				Save(new LevelCatalogConfig
				{
					catalogKey = "M4Content",
					chapterList = { levelChapterConfig }
				}, Path.Combine(project.ProjectPath, "Resources", "LevelCatalogs", "M4Content.tres"), project.ProjectPath);
				XWModManifestSyncService.SyncProject(project.ProjectPath);
				XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(project.ProjectPath, "mod.json"));
				owner = xWModManifest.Id;
				Directory.CreateDirectory(output);
				ModProject.ExportResult exportResult = await project.ExportAsync(output);
				Require(exportResult.Success && exportResult.ContainsRuntimeAssembly, "compiled export: " + exportResult.ErrorMessage);
				ZipFile.CreateFromDirectory(project.ProjectPath, Path.Combine(output, "ContentDemo-source.zip"));
				pmodPath = exportResult.OutputPath;
			}
			ModLoader.LoadedMod loadedMod = ModLoader.LoadMod(pmodPath);
			Require(loadedMod != null && ModLoader.ApplyMod(loadedMod), "apply compiled content: " + string.Join(";", loadedMod?.Diagnostics ?? new List<string>()));
			owner = loadedMod.Manifest.Id;
			Require(XWModContentCatalog.GetPackets(plants: true).Any((XWModContentCatalog.Packet packet) => packet.Key == "M4Sprout"), "new plant discoverable");
			Require(XWModContentCatalog.GetPackets(plants: false).Any((XWModContentCatalog.Packet packet) => packet.Key == "M4Walker"), "new zombie discoverable");
			XWModLevelIdentity xWModLevelIdentity = new XWModLevelIdentity(owner.ToLowerInvariant(), "M4Content", "M4Level1", "Normal");
			Require(XWModContentCatalog.TryResolve(xWModLevelIdentity, out var config, out var error) && config is TowerDefenseLevelNewConfig, "resolve first modern level: " + error);
			Require(XWModContentCatalog.TryResolve(xWModLevelIdentity with
			{
				LevelSaveKey = "M4Level2"
			}, out var config2, out error), "resolve second modern level: " + error);
			string[] array2;
			foreach (Variant item in XWModContentCatalog.GetCatalogs().Single((XWModContentCatalog.Catalog item) => item.Key == "M4Content").Data["Chapter"].AsGodotArray())
			{
				Dictionary dictionary = item.AsGodotDictionary();
				array2 = new string[3] { "UnlockImage", "Background", "Building" };
				foreach (string text2 in array2)
				{
					RequireTexture(dictionary[text2].AsString());
				}
				foreach (Variant item2 in dictionary["Level"].AsGodotArray())
				{
					RequireTexture(item2.AsGodotDictionary()["UnlockImage"].AsString());
				}
			}
			GameSaveManager instance = GameSaveManager.Instance;
			instance.EnsureLoaded();
			instance.SetUserCurrent("M4-content-profile-isolation");
			string text3 = Json.Stringify(instance.GetTowerDefensePacketDictionary());
			array2 = new string[2] { "M4Sprout", "M4Walker" };
			foreach (string text4 in array2)
			{
				TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.Instance.CreateCharacter(text4);
				Require(towerDefenseCharacter != null && towerDefenseCharacter.GetType().Assembly != typeof(TowerDefenseCharacter).Assembly, "formal factory uses companion: " + text4);
				TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(text4);
				Require(packetConfigReadOnly.Unlock(), "new packet defaults unlocked");
				AdobeAnimateSprite packetSprite = TowerDefenseManager.GetPacketSprite(packetConfigReadOnly);
				Require(packetSprite != null, "sprite preview: " + text4);
				towerDefenseCharacter.packet = packetConfigReadOnly;
				towerDefenseCharacter.config = (TowerDefenseCharacterConfig)packetConfigReadOnly.characterConfig.Duplicate(deep: true);
				towerDefenseCharacter.sprite = packetSprite;
				Dictionary packetState = XWModPlayerProgressService.GetPacketState(text4);
				packetState["Love"] = true;
				packetState["Key"] = new Dictionary { ["Custom"] = "removed-custom" };
				XWModPlayerProgressService.SetPacketState(text4, packetState);
				typeof(TowerDefenseCharacter).GetMethod("RefreshPacketCustomFromSave", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(towerDefenseCharacter, null);
				XWModPlayerProgressService.ClearCache();
				packetState = XWModPlayerProgressService.GetPacketState(text4);
				Require(packetState["Love"].AsBool() && packetState["Key"].AsGodotDictionary()["Custom"].AsString() == "", "character repairs removed custom in Mod profile and retains favorite");
				towerDefenseCharacter.config.customData.customDictionary["probe-custom"] = new Dictionary();
				towerDefenseCharacter.SwitchCustom("probe-custom");
				XWModPlayerProgressService.ClearCache();
				Require(XWModPlayerProgressService.GetPacketState(text4)["Key"].AsGodotDictionary()["Custom"].AsString() == "probe-custom", "character custom selection persists in Mod profile");
				towerDefenseCharacter.Free();
				packetSprite.Free();
			}
			Require(Json.Stringify(instance.GetTowerDefensePacketDictionary()) == text3, "Mod favorites and character custom reads/writes leave main packet dictionary unchanged");
			Require(ModLoader.UnloadMod(owner), "unload content");
			Require(!XWModContentCatalog.GetPackets(plants: true).Any((XWModContentCatalog.Packet packet) => packet.Key == "M4Sprout") && !XWModContentCatalog.TryResolve(xWModLevelIdentity, out config2, out var _), "disabled content disappears");
			GD.Print("MOD_CONTENT_EXAMPLE output=" + output);
			passed = true;
		}
		catch (Exception ex)
		{
			GD.PrintErr("MOD_CONTENT_FAILURE " + ex);
		}
		finally
		{
			if (owner.Length > 0)
			{
				ModLoader.UnloadMod(owner);
			}
			GD.Print("MOD_CONTENT_RESULT passed=" + passed);
			GetTree().Quit((!passed) ? 2 : 0);
		}
	}

	private static void CreateCharacter(string root, string family, string action, string key, string title)
	{
		string text = Path.Combine(root, "Resources", "Characters", family);
		XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction(action, text, key);
		Require(templateCreateResult.Success, "character authoring: " + templateCreateResult.Error);
		TowerDefenseCharacterConfig towerDefenseCharacterConfig = ResourceLoader.Load<TowerDefenseCharacterConfig>(Path.Combine(text, key, "Config", key + "Config.tres"), null, ResourceLoader.CacheMode.Reuse);
		Require(towerDefenseCharacterConfig != null, "authored character configuration reload");
		Save(new TowerDefensePacketConfig
		{
			saveKey = key,
			name = title,
			describe = "Mod 内容示例",
			characterConfig = towerDefenseCharacterConfig,
			packetAnimeScale = Vector2.One,
			unlockCheckList = new Array<UnlockConditionBaseConfig>()
		}, Path.Combine(root, "Resources", "Cards", key + ".tres"), root);
	}

	private static void Save(Resource resource, string path, string projectRoot)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		Require(ResourceSaver.Save(resource, path, ResourceSaver.SaverFlags.ChangePath) == Error.Ok, "save " + path);
		XWModProjectLayout.MakeSavedTextResourceReferencesPortable(path, projectRoot);
	}

	private static void RequireTexture(string path)
	{
		Require(!string.IsNullOrEmpty(path), "catalog artwork reference is missing");
		Texture2D texture2D = GD.Load<Texture2D>(path);
		Require(texture2D != null && texture2D.GetWidth() > 0 && texture2D.GetHeight() > 0, "catalog artwork loads: " + path);
	}

	private static void Require(bool value, string message)
	{
		if (!value)
		{
			throw new InvalidOperationException(message);
		}
	}
}
