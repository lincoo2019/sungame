using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGloomSquashDeathrattleRuntimeTest : Node
{
	private const string GloomPacketPath = "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Packet/PlantGloomSquash.tres";

	private const string GloomScenePath = "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn";

	private const string GloomSquashDefinitionPath = "res://Script/Component/TowerDefense/Character/SquashComponent/Definitions/GloomSquashDefinition.tres";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewGloomSquashDeathrattleRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			int num;
			_ = num - 1;
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0137;
				}
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Packet/PlantGloomSquash.tres");
				PackedScene packedScene = LoadScene("res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn");
				SquashComponentDefinition squashComponentDefinition = ResourceLoader.Load<SquashComponentDefinition>("res://Script/Component/TowerDefense/Character/SquashComponent/Definitions/GloomSquashDefinition.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate() && GodotObject.IsInstanceValid(squashComponentDefinition), "The scenario must load the authored GloomSquash packet, scene, and Squash definition.");
				Check(towerDefensePacketConfig?.saveKey == "PlantGloomSquash" && towerDefensePacketConfig.characterConfig?.name == "PlantGloomSquash" && squashComponentDefinition != null && !squashComponentDefinition.InitiallyAlive, "The authored GloomSquash must keep a dormant Squash definition until zero hitpoints.");
				TowerDefensePacketConfig zombiePacket = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
				PackedScene packedScene2 = LoadScene("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(zombiePacket) && zombiePacket.saveKey == "ZombieNormal" && GodotObject.IsInstanceValid(packedScene2) && packedScene2.CanInstantiate(), "The target fixture must load the authored ZombieNormal packet and scene.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(squashComponentDefinition) || !GodotObject.IsInstanceValid(zombiePacket) || !GodotObject.IsInstanceValid(packedScene2))
				{
					goto end_IL_0137;
				}
				RegisterRealFixtures();
				control = new BugOverviewGloomSquashDeathrattleRuntimeControlStub
				{
					Name = "GloomSquashDeathrattleRuntimeControl",
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
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "NightMapControl"
				};
				mapFeature = CreateNightMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseCellInstance testCell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(TowerDefenseManager.GetMapIsNight() && GodotObject.IsInstanceValid(testCell) && testCell.gridPos == TestGrid, "The scenario must use a real cell from an active night map feature.");
				TowerDefensePlantGloomSquash gloom = towerDefensePacketConfig.Plant(TestGrid, playAudio: false) as TowerDefensePlantGloomSquash;
				await WaitFrames(10);
				TowerDefenseZombieNormal zombie = zombiePacket.Plant(TestGrid, playAudio: false) as TowerDefenseZombieNormal;
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(gloom) && gloom.config?.name == "PlantGloomSquash" && gloom.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn", "The test object must be the authored TowerDefensePlantGloomSquash scene.");
				Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieNormal" && zombie.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", "The target must be the authored TowerDefenseZombieNormal scene.");
				if (!GodotObject.IsInstanceValid(gloom) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_0137;
				}
				gloom.ProcessMode = ProcessModeEnum.Disabled;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				gloom.inGame = true;
				zombie.inGame = true;
				gloom.gridPos = TestGrid;
				zombie.gridPos = TestGrid;
				gloom.cell = testCell;
				zombie.cell = testCell;
				zombie.GlobalPosition = gloom.GlobalPosition;
				AddToCell(gloom, testCell);
				AddToCell(zombie, testCell);
				Check(testCell.characterList.Contains(gloom) && testCell.characterList.Contains(zombie), "The real GloomSquash and ZombieNormal must share the same production map cell.");
				SquashComponent runtime = gloom.componentManager.GetRuntime<SquashComponent>("character.squash");
				AttackComponent runtime2 = gloom.componentManager.GetRuntime<AttackComponent>("character.attack.0");
				AttackComponent runtime3 = gloom.componentManager.GetRuntime<AttackComponent>("character.attack.1");
				Check(runtime != null && !runtime.IsReleased && runtime2 != null && !runtime2.IsReleased && runtime3 != null && !runtime3.IsReleased, "The real scene must expose its Squash and both authored Attack runtimes.");
				if (runtime == null || runtime.IsReleased || runtime2 == null || runtime2.IsReleased || runtime3 == null || runtime3.IsReleased)
				{
					goto end_IL_0137;
				}
				Check(!runtime.Alive && !runtime.IsRunning(), "The real Squash component must begin dormant.");
				runtime.IdleProcessing(0.0);
				Check(!runtime.IsRunning() && runtime.target == null, "A co-located real zombie must not trigger the dormant Squash component.");
				double hitpoints = gloom.instance.hitpoints;
				gloom.Hurt(10.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
				Check(!gloom.instance.invincible && Mathf.IsEqualApprox(gloom.instance.hitpoints, hitpoints - 10.0), "A real nonlethal Hurt must pass through the production damage pipeline.");
				Check(gloom.instance.hitpoints > 0.0, "The nonlethal damage fixture must leave positive hitpoints.");
				Check(!gloom.instance.nearDie && !gloom.nearDie, "Nonlethal damage must not set either near-death layer.");
				Check(!runtime.Alive && !runtime.IsRunning(), "Nonlethal real damage must keep the Squash component dormant.");
				gloom.instance.nearDie = true;
				gloom.nearDie = true;
				gloom.BatchUpdate(0.0);
				Check(gloom.instance.hitpoints > 0.0 && !runtime.Alive && !runtime.IsRunning(), "Near-death flags alone at positive hitpoints must not arm the Squash component.");
				gloom.instance.nearDie = false;
				gloom.nearDie = false;
				gloom.SkipInvincibleHurt(gloom.instance.hitpoints + 1.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
				Check(gloom.instance.hitpoints <= 0.0, "A lethal SkipInvincibleHurt must drive the real plant to zero hitpoints.");
				Check(gloom.instance.nearDie && gloom.nearDie, "The production lethal-damage path must set both near-death layers.");
				Check(!runtime.Alive && !runtime.IsRunning(), "The Squash component must remain dormant until GloomSquash.BatchUpdate observes zero hitpoints.");
				gloom.BatchUpdate(0.0);
				Check(runtime.Alive && !runtime2.Alive && runtime3.Alive, "Zero hitpoints must arm the real Squash component, stop Attack0, and retain Attack1 targeting.");
				Check(Mathf.IsEqualApprox(gloom.instance.hitpoints, 300.0) && !gloom.instance.nearDie && !gloom.nearDie && !gloom.instance.die && !gloom.die, "Deathrattle activation must restore 300 hitpoints and clear both instance and character death state.");
				control.isGameRunning = true;
				gloom.componentAlive = true;
				gloom.componentRunning = false;
				runtime3.target = null;
				runtime3.checkIntrevalNow = 0;
				runtime.IdleProcessing(0.0);
				Check(runtime.IsRunning() && runtime.target == zombie, "The armed production Squash component must run against the real co-located ZombieNormal.");
				goto end_IL_0122;
				end_IL_0137:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[GloomSquashDeathrattle] Unexpected exception: {value}");
				goto end_IL_0122;
			}
			return;
			end_IL_0122:;
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
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(2);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"GLOOM_SQUASH_DEATHRATTLE_ACTIVATION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateNightMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			isNight = true,
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y)
		});
		for (int i = 1; i <= gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static void AddToCell(TowerDefenseCharacter character, TowerDefenseCellInstance cell)
	{
		if (GodotObject.IsInstanceValid(cell) && !cell.characterList.Contains(character))
		{
			cell.characterList.Add(character);
		}
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.IgnoreDeep)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static PackedScene LoadScene(string path)
	{
		return ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantGloomSquash", "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Packet/PlantGloomSquash.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantGloomSquash", "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
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
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
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
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[GloomSquashDeathrattle] " + message);
		}
	}
}
