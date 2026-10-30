using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.ModSystem;

public partial class BugOverviewBungiSquashShovelSurvivalRuntimeTest : Node
{
	private const string BungiSquashPacketPath = "res://Asset/Anime/Character/Plant/Chapter8/BungiSquash/Packet/PlantBungiSquash.tres";

	private const string BungiSquashScenePath = "res://Asset/Anime/Character/Plant/Chapter8/BungiSquash/Scene/TowerDefensePlantBungiSquash.tscn";

	private const string BungiPacketPath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres";

	private const string BungiScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn";

	private static readonly Vector2I ShovelGrid = new Vector2I(3, 3);

	private static readonly Vector2I NormalGrid = new Vector2I(6, 3);

	private int _checks;

	private int _failures;

	private BungiSquashShovelSurvivalRuntimeControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				GameSaveManager.Instance.SetConfigValue("Backgrounder", true);
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				XWModManager xWModManager = new XWModManager(ProjectSettings.GlobalizePath("user://BungiSquashMods"));
				xWModManager.SaveEnabledIds(Array.Empty<string>());
				xWModManager.LoadEnabledModsWithResult();
				XWModEnvironmentStatus xWModEnvironmentStatus = await XWModEnvironmentService.EnsureReadyAsync();
				if (!xWModEnvironmentStatus.Ready)
				{
					throw new InvalidOperationException(xWModEnvironmentStatus.Reason);
				}
				RegisterRealFixtures();
				SetupBattleFixture(manager);
				await VerifyShoveledLandingDoesNotSurvive();
				await RemoveAllBungi();
				await VerifyNormalLandingStillSummonsHypnotizedBungi();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewBungiSquashShovelSurvivalRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			_mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 24;
		GD.Print($"BUNGI_SQUASH_SHOVEL_SURVIVAL_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void SetupBattleFixture(TowerDefenseManager manager)
	{
		_control = new BungiSquashShovelSurvivalRuntimeControlStub
		{
			Name = "BungiSquashShovelSurvivalRuntimeControl",
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		manager.gridBeginPos = Vector2.Zero;
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		_mapControl = new TowerDefenseMapControl
		{
			Name = "MapControl"
		};
		_mapFeature = CreateMapFeature(_mapControl, manager.gridNum);
		_mapFeature.control = _control;
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
	}

	private async Task VerifyShoveledLandingDoesNotSurvive()
	{
		TowerDefensePlantBungiSquash towerDefensePlantBungiSquash = await SpawnSquash(ShovelGrid);
		Check(GodotObject.IsInstanceValid(towerDefensePlantBungiSquash), "The shovel race must use a real Bungi Squash.");
		if (!GodotObject.IsInstanceValid(towerDefensePlantBungiSquash))
		{
			throw new InvalidOperationException("The real Bungi Squash did not spawn for the shovel race.");
		}
		DestroyComponent destroyComponent = towerDefensePlantBungiSquash.componentManager?.GetRuntime<DestroyComponent>();
		Check(destroyComponent != null && !destroyComponent.IsReleased, "The real Bungi Squash must expose its Destroy runtime.");
		PrepareImmediateLanding(towerDefensePlantBungiSquash);
		towerDefensePlantBungiSquash.ShovelDestroy();
		Check(towerDefensePlantBungiSquash.isShovel, "ShovelDestroy must mark the airborne Bungi Squash as shoveled synchronously.");
		Check(towerDefensePlantBungiSquash.isDestroy && towerDefensePlantBungiSquash.die, "ShovelDestroy must mark destruction before the deferred free frame.");
		towerDefensePlantBungiSquash.PhysiceUpdate(0.01f);
		List<TowerDefenseZombieBungi> list = FindBungiAtGrid(ShovelGrid);
		Check(list.Count == 0, $"A shoveled Bungi Squash must not summon a Bungi when landing in the deferred-free window; got {list.Count}.");
		TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = new TowerDefenseLevelSaveConfigCSharp();
		towerDefenseLevelSaveConfigCSharp.Save();
		Check(CountSavedBungi(towerDefenseLevelSaveConfigCSharp, ShovelGrid) == 0, "The same-frame shovel race must not add ZombieBungi to survival progress.");
	}

	private async Task VerifyNormalLandingStillSummonsHypnotizedBungi()
	{
		TowerDefensePlantBungiSquash towerDefensePlantBungiSquash = await SpawnSquash(NormalGrid);
		Check(GodotObject.IsInstanceValid(towerDefensePlantBungiSquash), "The normal landing control must use a second real Bungi Squash.");
		if (!GodotObject.IsInstanceValid(towerDefensePlantBungiSquash))
		{
			throw new InvalidOperationException("The real Bungi Squash did not spawn for the control landing.");
		}
		PrepareImmediateLanding(towerDefensePlantBungiSquash);
		towerDefensePlantBungiSquash.PhysiceUpdate(0.01f);
		List<TowerDefenseZombieBungi> list = FindBungiAtGrid(NormalGrid);
		Check(list.Count == 1, $"A normal Bungi Squash landing must summon exactly one real Bungi; got {list.Count}.");
		TowerDefenseZombieBungi bungi = ((list.Count == 1) ? list[0] : null);
		Check(GodotObject.IsInstanceValid(bungi), "The normal landing result must be a real TowerDefenseZombieBungi instance.");
		Check(bungi?.skipBungeeTarget ?? false, "The Bungi Squash payload must skip normal bungee target acquisition.");
		BugOverviewBungiSquashShovelSurvivalRuntimeTest bugOverviewBungiSquashShovelSurvivalRuntimeTest = this;
		TowerDefenseZombieBungi towerDefenseZombieBungi = bungi;
		bugOverviewBungiSquashShovelSurvivalRuntimeTest.Check(towerDefenseZombieBungi != null && towerDefenseZombieBungi.instance?.hypnoses == true, "The normally summoned Bungi must be hypnotized.");
		BugOverviewBungiSquashShovelSurvivalRuntimeTest bugOverviewBungiSquashShovelSurvivalRuntimeTest2 = this;
		TowerDefenseZombieBungi towerDefenseZombieBungi2 = bungi;
		bugOverviewBungiSquashShovelSurvivalRuntimeTest2.Check(towerDefenseZombieBungi2 != null && towerDefenseZombieBungi2.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, $"The normally summoned Bungi must join the plant camp; got {bungi?.camp}.");
		Check(bungi?.gridPos == NormalGrid, $"The normally summoned Bungi must preserve the Bungi Squash grid {NormalGrid}; got {bungi?.gridPos}.");
		Check(GodotObject.IsInstanceValid(bungi) && bungi.inGame && !bungi.die && !bungi.isDestroy, "The normally summoned hypnotized Bungi must remain a live in-game character.");
		bool flag = await WaitUntil(() => GodotObject.IsInstanceValid(bungi) && bungi.CurrentStateHandle?.StableId == "zombie.bungi.drop", 10);
		double dropStartZ = bungi?.z ?? 0.0;
		Check(flag && !bungi.isGround && dropStartZ >= 500.0, $"The summoned hypnotized Bungi must begin its real drop from above the board; state={bungi?.CurrentStateHandle?.StableId}, isGround={bungi?.isGround}, z={dropStartZ:F3}.");
		Check(await WaitUntil(() => GodotObject.IsInstanceValid(bungi) && bungi.isGround, 120), "The hypnotized Bungi summoned by Bungi Squash must finish descending instead of hanging in the air.");
		Check(GodotObject.IsInstanceValid(bungi) && bungi.z < dropStartZ - 100.0 && Math.Abs(bungi.z - bungi.groundHeight) < 0.01, $"The landed hypnotized Bungi must reach its real ground height; start={dropStartZ:F3}, end={bungi?.z:F3}, ground={bungi?.groundHeight:F3}.");
		Check(bungi?.CurrentStateHandle?.StableId == "character.idle" && bungi.waitGrab, $"A landed hypnotized Bungi must enter its authored wait-to-grab state; state={bungi?.CurrentStateHandle?.StableId}, waitGrab={bungi?.waitGrab}.");
		TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = new TowerDefenseLevelSaveConfigCSharp();
		towerDefenseLevelSaveConfigCSharp.Save();
		Check(CountSavedBungi(towerDefenseLevelSaveConfigCSharp, NormalGrid) == 1, "The normal landing control must remain representable in survival progress.");
		TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = FindSavedBungi(towerDefenseLevelSaveConfigCSharp, NormalGrid);
		Check(towerDefenseCharacterSaveConfigCSharp != null, "Survival progress must contain the normally summoned Bungi.");
		Check(towerDefenseCharacterSaveConfigCSharp?.gridPos == NormalGrid, "The saved normal Bungi must retain its landing grid.");
		bool condition = towerDefenseCharacterSaveConfigCSharp != null && towerDefenseCharacterSaveConfigCSharp.variantSave != null && towerDefenseCharacterSaveConfigCSharp.variantSave.GetValueOrDefault("skipBungeeTarget", false).AsBool();
		Check(condition, "The saved normal Bungi must retain skipBungeeTarget for the next survival round.");
	}

	private async Task<TowerDefensePlantBungiSquash> SpawnSquash(Vector2I gridPos)
	{
		TowerDefensePlantBungiSquash squash = ((ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter8/BungiSquash/Packet/PlantBungiSquash.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) is TowerDefensePacketConfig towerDefensePacketConfig) ? towerDefensePacketConfig.Plant(gridPos, playAudio: false) : null) as TowerDefensePlantBungiSquash;
		await WaitFrames(2);
		if (GodotObject.IsInstanceValid(squash))
		{
			squash.ProcessMode = ProcessModeEnum.Disabled;
		}
		return squash;
	}

	private static void PrepareImmediateLanding(TowerDefensePlantBungiSquash squash)
	{
		squash.isGround = false;
		squash.z = squash.groundHeight + 1.0;
		squash.ySpeed = 200.0;
	}

	private List<TowerDefenseZombieBungi> FindBungiAtGrid(Vector2I gridPos)
	{
		List<TowerDefenseZombieBungi> list = new List<TowerDefenseZombieBungi>();
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return list;
		}
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombieBungi towerDefenseZombieBungi && GodotObject.IsInstanceValid(towerDefenseZombieBungi) && towerDefenseZombieBungi.gridPos == gridPos)
			{
				list.Add(towerDefenseZombieBungi);
			}
		}
		return list;
	}

	private async Task RemoveAllBungi()
	{
		if (GodotObject.IsInstanceValid(_control?.characterNode))
		{
			foreach (Node child in _control.characterNode.GetChildren())
			{
				if (child is TowerDefenseZombieBungi towerDefenseZombieBungi && GodotObject.IsInstanceValid(towerDefenseZombieBungi))
				{
					towerDefenseZombieBungi.QueueFree();
				}
			}
		}
		await WaitFrames(2);
	}

	private static int CountSavedBungi(TowerDefenseLevelSaveConfigCSharp progress, Vector2I gridPos)
	{
		int num = 0;
		foreach (TowerDefenseCharacterSaveConfigCSharp character in progress.characterList)
		{
			if (character != null && character.packetName == "ZombieBungi" && character.gridPos == gridPos)
			{
				num++;
			}
		}
		return num;
	}

	private static TowerDefenseCharacterSaveConfigCSharp FindSavedBungi(TowerDefenseLevelSaveConfigCSharp progress, Vector2I gridPos)
	{
		foreach (TowerDefenseCharacterSaveConfigCSharp character in progress.characterList)
		{
			if (character != null && character.packetName == "ZombieBungi" && character.gridPos == gridPos)
			{
				return character;
			}
		}
		return null;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.config.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y)
		});
		for (int i = 1; i <= gridNum.Y; i++)
		{
			towerDefenseBattleFeatureMap.config.lineUse.Add(i);
		}
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantBungiSquash", "res://Asset/Anime/Character/Plant/Chapter8/BungiSquash/Packet/PlantBungiSquash.tres");
		RegisterPacket("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres");
		RegisterCharacter("PlantBungiSquash", "res://Asset/Anime/Character/Plant/Chapter8/BungiSquash/Scene/TowerDefensePlantBungiSquash.tscn");
		RegisterCharacter("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
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

	private void RestoreRealFixtures()
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

	private async Task<bool> WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return condition();
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewBungiSquashShovelSurvivalRuntimeTest] " + message);
		}
	}
}
