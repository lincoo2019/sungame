using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class ExportedSpecialPlantFireRuntimeTest : Node
{
	private readonly record struct PlantCase(string Name, string PacketName, string PacketPath, string ScenePath, Vector2I GridPos, string ProjectileName, int ExpectedProjectiles);

	private const string FirePeaSAnimationPath = "res://Asset/Config/Projectile/Pea/Sprite/FirePeaS/FirePeaS.tres";

	private static readonly PlantCase[] Cases = new PlantCase[2]
	{
		new PlantCase("SunPeashooter", "PlantSunPeashooter", "res://Asset/Anime/Character/Plant/Chapter5/SunPeashooter/Packet/PlantSunPeashooter.tres", "res://Asset/Anime/Character/Plant/Chapter5/SunPeashooter/Scene/TowerDefensePlantSunPeashooter.tscn", new Vector2I(2, 1), "WhiteFirePea", 1),
		new PlantCase("ThreePeaterU", "PlantThreePeaterU", "res://Asset/Anime/Character/Plant/Gold/ThreePeaterU/Packet/PlantThreePeaterU.tres", "res://Asset/Anime/Character/Plant/Gold/ThreePeaterU/Scene/TowerDefensePlantThreePeaterU.tscn", new Vector2I(2, 3), "FirePeaS", 3)
	};

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		ExportedSpecialPlantFireControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance), "ProjectileUpdateManager must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
				{
					goto end_IL_00e6;
				}
				ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				TowerDefenseProjectileRegistry.Init();
				RegisterFixtures();
				control = new ExportedSpecialPlantFireControlStub
				{
					Name = "ExportedSpecialPlantFireControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				PlantCase[] cases = Cases;
				for (int i = 0; i < cases.Length; i++)
				{
					PlantCase item = cases[i];
					TowerDefensePlant plant = LoadPacket(item.PacketPath)?.Plant(item.GridPos, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefensePlant;
					await WaitFrames(5);
					AdobeAnimateRuntimeDefinition poisonedDefinition = ((item.ProjectileName == "FirePeaS") ? PoisonFirePeaSDefinitionCache() : null);
					await VerifyAnimatedVolley(item, plant, control);
					if (poisonedDefinition != null)
					{
						VerifyFirePeaSDefinitionRecovered(poisonedDefinition);
					}
					if (GodotObject.IsInstanceValid(plant))
					{
						plant.QueueFree();
					}
					await WaitFrames(3);
				}
				VerifyAllPoseManifestSources();
				VerifyStandalonePoseFallback();
				goto end_IL_00cf;
				end_IL_00e6:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[ExportedSpecialPlantFireRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00cf;
			}
			return;
			end_IL_00cf:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
			{
				ProjectileUpdateManager.Instance.ProcessMode = previousProjectileProcessMode;
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"EXPORTED_SPECIAL_PLANT_FIRE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private AdobeAnimateRuntimeDefinition PoisonFirePeaSDefinitionCache()
	{
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Config/Projectile/Pea/Sprite/FirePeaS/FirePeaS.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(adobeAnimateData), "FirePeaS animation data must load before the exported-cache regression.");
		if (!GodotObject.IsInstanceValid(adobeAnimateData))
		{
			return null;
		}
		AdobeAnimateRuntimeDefinition orBuildForCompactCrowd = AdobeAnimateDefinitionCache.GetOrBuildForCompactCrowd(adobeAnimateData);
		Check(HasCompactCrowdTextureArrays(orBuildForCompactCrowd), "FirePeaS must initially resolve both shared exported texture arrays.");
		if (orBuildForCompactCrowd == null)
		{
			return null;
		}
		Check(orBuildForCompactCrowd.HasGpuPoseManifestEntry, "FirePeaS must have a pose allocation in the exported atlas manifest.");
		Check(orBuildForCompactCrowd.GpuPoseSignature == orBuildForCompactCrowd.GpuPoseManifestSignature, $"FirePeaS runtime and manifest pose signatures must match; runtime={orBuildForCompactCrowd.GpuPoseSignature:X16}, manifest={orBuildForCompactCrowd.GpuPoseManifestSignature:X16}.");
		orBuildForCompactCrowd.GpuPoseTexture = null;
		orBuildForCompactCrowd.GpuPoseTextureArray = null;
		orBuildForCompactCrowd.GpuPoseTextureRid = default(Rid);
		orBuildForCompactCrowd.GpuPoseTextureSize = Vector2I.Zero;
		orBuildForCompactCrowd.UsesGpuPoseTextureArray = false;
		Check(!HasCompactCrowdTextureArrays(orBuildForCompactCrowd), "The regression fixture must reproduce an early cached definition without a pose array.");
		return orBuildForCompactCrowd;
	}

	private void VerifyFirePeaSDefinitionRecovered(AdobeAnimateRuntimeDefinition poisonedDefinition)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(ResourceLoader.Load<AdobeAnimateData>("res://Asset/Config/Projectile/Pea/Sprite/FirePeaS/FirePeaS.tres", null, ResourceLoader.CacheMode.Reuse));
		Check(poisonedDefinition != orBuild, "BulletField registration must replace the incomplete cached FirePeaS definition.");
		Check(HasCompactCrowdTextureArrays(orBuild), "Recovered FirePeaS definition must retain valid visual and pose arrays.");
		Check(orBuild != null && orBuild.HasGpuPoseManifestEntry && orBuild.GpuPoseSignature == orBuild.GpuPoseManifestSignature, "Recovered FirePeaS definition must bind the exact manifest pose signature.");
	}

	private void VerifyStandalonePoseFallback()
	{
		AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Config/Projectile/Pea/Sprite/FirePeaS/FirePeaS.tres", null, ResourceLoader.CacheMode.Ignore);
		bool flag = GodotObject.IsInstanceValid(adobeAnimateData) && (adobeAnimateData.HasPackedRuntimeData() || adobeAnimateData.EnsurePackedRuntimeData());
		Check(flag, "FirePeaS packed data must be available for the stale-manifest fallback regression.");
		if (flag && adobeAnimateData.sliceAlpha.Length != 0)
		{
			adobeAnimateData.sliceAlpha = (float[])adobeAnimateData.sliceAlpha.Clone();
			adobeAnimateData.sliceAlpha[0] = Math.Clamp(adobeAnimateData.sliceAlpha[0] - 0.125f, 0f, 1f);
			AdobeAnimateDefinitionCache.Invalidate(adobeAnimateData);
			AdobeAnimateRuntimeDefinition orBuildForCompactCrowd = AdobeAnimateDefinitionCache.GetOrBuildForCompactCrowd(adobeAnimateData);
			Check(HasCompactCrowdTextureArrays(orBuildForCompactCrowd), "A stale shared pose signature must rebuild a usable standalone pose array.");
			Check(orBuildForCompactCrowd != null && !orBuildForCompactCrowd.UsesBakedGpuPoseTexture && orBuildForCompactCrowd.UsesGpuPoseTextureArray, "The stale-signature recovery must use the runtime-built pose array, not mislabel it as baked.");
			Check(orBuildForCompactCrowd != null && orBuildForCompactCrowd.HasGpuPoseManifestEntry && orBuildForCompactCrowd.GpuPoseSignature != orBuildForCompactCrowd.GpuPoseManifestSignature, "The fallback regression must preserve the manifest mismatch that triggered runtime rebuilding.");
			bool value = HasCompactCrowdTextureArrays(orBuildForCompactCrowd) && orBuildForCompactCrowd != null && !orBuildForCompactCrowd.UsesBakedGpuPoseTexture && orBuildForCompactCrowd.UsesGpuPoseTextureArray && orBuildForCompactCrowd.HasGpuPoseManifestEntry && orBuildForCompactCrowd.GpuPoseSignature != orBuildForCompactCrowd.GpuPoseManifestSignature;
			GD.Print($"EXPORTED_ADOBE_ANIMATE_POSE_FALLBACK_RESULT passed={value} runtime={orBuildForCompactCrowd?.GpuPoseSignature:X16} manifest={orBuildForCompactCrowd?.GpuPoseManifestSignature:X16}");
			AdobeAnimateDefinitionCache.Invalidate(adobeAnimateData);
		}
	}

	private void VerifyAllPoseManifestSources()
	{
		AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = ResourceLoader.Load<AdobeAnimateGlobalAtlasManifest>("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(adobeAnimateGlobalAtlasManifest) && adobeAnimateGlobalAtlasManifest.SourceKeys.Count > 0, "The exported global atlas manifest must expose its animation sources.");
		if (!GodotObject.IsInstanceValid(adobeAnimateGlobalAtlasManifest) || adobeAnimateGlobalAtlasManifest.SourceKeys.Count == 0)
		{
			return;
		}
		List<string> list = new List<string>();
		int num = 0;
		for (int i = 0; i < adobeAnimateGlobalAtlasManifest.SourceKeys.Count; i++)
		{
			string text = adobeAnimateGlobalAtlasManifest.SourceKeys[i];
			string text2 = ResolveManifestDefinitionPath(text);
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(text2, null, ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(adobeAnimateData))
			{
				list.Add(text + ": missing " + text2);
				continue;
			}
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateData);
			if (!HasCompactCrowdTextureArrays(orBuild) || !orBuild.HasGpuPoseManifestEntry || orBuild.GpuPoseSignature != orBuild.GpuPoseManifestSignature)
			{
				list.Add($"{text}: runtime={orBuild?.GpuPoseSignature:X16} manifest={orBuild?.GpuPoseManifestSignature:X16} arrays={HasCompactCrowdTextureArrays(orBuild)}");
			}
			else
			{
				num++;
			}
		}
		string value = ((list.Count == 0) ? string.Empty : string.Join("; ", list.GetRange(0, Math.Min(8, list.Count))));
		Check(list.Count == 0 && num == adobeAnimateGlobalAtlasManifest.SourceKeys.Count, $"Every DAT-free exported animation must bind its shared pose atlas; verified={num}/{adobeAnimateGlobalAtlasManifest.SourceKeys.Count}, failures={list.Count}, first={value}.");
		GD.Print($"EXPORTED_ADOBE_ANIMATE_POSE_MANIFEST_RESULT passed={list.Count == 0} verified={num} total={adobeAnimateGlobalAtlasManifest.SourceKeys.Count} failures={list.Count}");
	}

	private static string ResolveManifestDefinitionPath(string sourceKey)
	{
		return sourceKey switch
		{
			"res://Asset/Anime/Character/GraveStone/Skeleton/Skeletuar.dat" => "res://Asset/Anime/Character/GraveStone/Skeleton/Skeleton.tres", 
			"res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/FumeShroomSea.dat" => "res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/FumeSeaShroom.tres", 
			"res://Asset/Anime/Character/Plant/Other/SplitPeaCat/SpiltPeaCat.dat" => "res://Asset/Anime/Character/Plant/Other/SplitPeaCat/SplitPeaCat.tres", 
			"res://Asset/Anime/Character/Plant/Other/ThreeHybirdPeater/ThreeHybridPeater.dat" => "res://Asset/Anime/Character/Plant/Other/ThreeHybirdPeater/ThreeHybirdPeater.tres", 
			_ => sourceKey.GetBaseDir().PathJoin(sourceKey.GetFile().GetBaseName() + ".tres"), 
		};
	}

	private static bool HasCompactCrowdTextureArrays(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition != null && definition.AtlasTextureArrayRid.IsValid && GodotObject.IsInstanceValid(definition.AtlasTextureArray) && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f && definition.UsesGpuPoseTextureArray && definition.GpuPoseTextureRid.IsValid && GodotObject.IsInstanceValid(definition.GpuPoseTextureArray) && definition.GpuPoseTextureSize.X > 0)
		{
			return definition.GpuPoseTextureSize.Y > 0;
		}
		return false;
	}

	private async Task VerifyAnimatedVolley(PlantCase item, TowerDefensePlant plant, ExportedSpecialPlantFireControlStub control)
	{
		Check(GodotObject.IsInstanceValid(plant) && plant.config?.name == item.PacketName, item.Name + " real scene must instantiate inside the battlefield.");
		if (!GodotObject.IsInstanceValid(plant))
		{
			return;
		}
		FireComponent fire = plant.componentManager?.GetRuntime<FireComponent>("character.fire");
		Check(fire != null && !fire.IsReleased, item.Name + " must expose an active FireComponent runtime.");
		if (fire == null || fire.IsReleased)
		{
			return;
		}
		FireComponentCheckConfig fireComponentCheckConfig = ((fire.fireCheckList.Count > 0) ? fire.fireCheckList[0] : null);
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = fireComponentCheckConfig?.projectile?.GetProjectile();
		Check(GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) && towerDefenseProjectileCreateData.projectileName == new StringName(item.ProjectileName), item.Name + " must retain projectile " + item.ProjectileName + " in its runtime definition.");
		Check(GodotObject.IsInstanceValid(fire.sprite) && fire.sprite.HasClip(fire.fireAnimeClips), item.Name + " must retain fire animation " + fire.fireAnimeClips + ".");
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) || !GodotObject.IsInstanceValid(fire.sprite))
		{
			return;
		}
		BulletField field = BulletField.EnsureMountedOnCharacterNode();
		Check(GodotObject.IsInstanceValid(field), item.Name + " must have a mounted BulletField.");
		if (!GodotObject.IsInstanceValid(field))
		{
			return;
		}
		field.ClearActiveBullets();
		int animationEvents = 0;
		int volleySignals = 0;
		int spawnedProjectiles = 0;
		bool previousCheckUse = fire.checkUse;
		fire.sprite.OnAnimeEvent += OnAnimationEvent;
		fire.OnFireVolley += OnVolley;
		field.OnBulletSpawned += OnBulletSpawned;
		try
		{
			control.isGameRunning = true;
			fire.alive = true;
			fire.checkUse = false;
			fire.runningCheck = fireComponentCheckConfig;
			fire.runningCheckId = 0;
			fire.currentFireNum = 0;
			fire.AttackEntered();
			for (int frame = 0; frame < 240; frame++)
			{
				if (spawnedProjectiles >= item.ExpectedProjectiles)
				{
					break;
				}
				fire.AttackProcessing(1.0 / 60.0);
				plant.BatchUpdate(1.0 / 60.0);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
		}
		finally
		{
			fire.checkUse = previousCheckUse;
			fire.alive = false;
			fire.sprite.OnAnimeEvent -= OnAnimationEvent;
			fire.OnFireVolley -= OnVolley;
			field.OnBulletSpawned -= OnBulletSpawned;
		}
		Check(animationEvents == 1, $"{item.Name} packed fire animation must emit one fire event; events={animationEvents}.");
		Check(volleySignals == item.ExpectedProjectiles, $"{item.Name} fire event must emit {item.ExpectedProjectiles} volley signals; volleys={volleySignals}.");
		Check(spawnedProjectiles == item.ExpectedProjectiles, $"{item.Name} fire event must spawn {item.ExpectedProjectiles} BulletField projectiles; spawned={spawnedProjectiles}.");
		void OnAnimationEvent(string command, Variant _)
		{
			if (command == fire.fireEventName)
			{
				animationEvents++;
			}
		}
		void OnBulletSpawned(int index)
		{
			if (field.GetBulletDataRef(index).fireCharacter == plant)
			{
				spawnedProjectiles++;
			}
		}
		void OnVolley(ulong _)
		{
			volleySignals++;
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterFixtures()
	{
		PlantCase[] cases = Cases;
		for (int i = 0; i < cases.Length; i++)
		{
			PlantCase plantCase = cases[i];
			RegisterPacket(plantCase.PacketName, plantCase.PacketPath);
			RegisterCharacter(plantCase.PacketName, plantCase.ScenePath);
		}
	}

	private void RegisterPacket(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ExportedSpecialPlantFireRuntimeTest] " + message);
		}
	}
}
