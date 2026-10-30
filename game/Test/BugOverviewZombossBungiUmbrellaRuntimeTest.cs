using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewZombossBungiUmbrellaRuntimeTest : Node
{
	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn";

	private const string BungiPacketPath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres";

	private const string BungiScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn";

	private const string UmbrellaPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Umbrellaleaf/Packet/PlantUmbrellaleaf.tres";

	private const string UmbrellaScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Umbrellaleaf/Scene/TowerDefensePlantUmbrellaleaf.tscn";

	private static readonly Vector2I ProtectedGrid = new Vector2I(2, 3);

	private int _checks;

	private int _failures;

	private int _blockEvents;

	private ZombossBungiUmbrellaRuntimeControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<Resource> _loadedResources = new List<Resource>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BlockComponent blockComponent = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				RegisterFixture("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
				RegisterFixture("PlantUmbrellaleaf", "res://Asset/Anime/Character/Plant/Chapter0/Umbrellaleaf/Packet/PlantUmbrellaleaf.tres", "res://Asset/Anime/Character/Plant/Chapter0/Umbrellaleaf/Scene/TowerDefensePlantUmbrellaleaf.tscn");
				SetupBattleFixture(manager);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", null, ResourceLoader.CacheMode.Ignore);
				_loadedResources.Add(packedScene);
				Check(GodotObject.IsInstanceValid(packedScene), "The production Zomboss scene must load.");
				TowerDefenseZombieBoss boss = packedScene?.Instantiate<TowerDefenseZombieBoss>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(boss), "The regression must instantiate the production Zomboss object.");
				if (!GodotObject.IsInstanceValid(boss))
				{
					throw new InvalidOperationException("Production Zomboss could not be instantiated.");
				}
				boss.Name = "RuntimeZomboss";
				boss.inGame = true;
				boss.ProcessMode = ProcessModeEnum.Disabled;
				_control.characterNode.AddChild(boss, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				_control.isGameRunning = true;
				Check(TowerDefenseManager._IsGameRunning(), "The umbrella must be planted through the production running-battle lifecycle.");
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantUmbrellaleaf");
				Check(GodotObject.IsInstanceValid(packetConfig), "The production Umbrellaleaf packet must load.");
				TowerDefensePlantUmbrellaleaf umbrella = packetConfig?.Plant(ProtectedGrid, playAudio: false) as TowerDefensePlantUmbrellaleaf;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(umbrella) && umbrella.config?.name == "PlantUmbrellaleaf" && umbrella.gridPos == ProtectedGrid, "The regression must place the real Umbrellaleaf at the protected cell.");
				if (!GodotObject.IsInstanceValid(umbrella))
				{
					throw new InvalidOperationException("Production Umbrellaleaf could not be planted.");
				}
				blockComponent = umbrella.componentManager?.GetRuntime<BlockComponent>("character.block");
				Check(blockComponent != null && !blockComponent.IsReleased && blockComponent.parent == umbrella, "The real Umbrellaleaf BlockComponent must be active and bound.");
				if (blockComponent == null || blockComponent.IsReleased)
				{
					throw new InvalidOperationException("Umbrellaleaf BlockComponent is unavailable.");
				}
				blockComponent.OnBlock += CountBlock;
				Check(umbrella.inGame && umbrella.componentAlive && umbrella.IsOwnerBatchRegistered && umbrella.IsOwnerBatchDispatchActive && umbrella.componentManager.HasRuntimePhysicsWork, $"The umbrella must participate in the real character physics batch; inGame={umbrella.inGame}, componentAlive={umbrella.componentAlive}, batchRegistered={umbrella.IsOwnerBatchRegistered}, batchActive={umbrella.IsOwnerBatchDispatchActive}, runtimePhysics={umbrella.componentManager.HasRuntimePhysicsWork}.");
				RefreshRegistration(manager, boss, umbrella);
				blockComponent.SetAlive(alive: false);
				boss.BungeeSpawn();
				await WaitFrames(8);
				TowerDefenseZombieBungi bungi = FindBossBungi(boss);
				Check(GodotObject.IsInstanceValid(bungi), "Zomboss must create a live production Bungi object.");
				if (!GodotObject.IsInstanceValid(bungi))
				{
					throw new InvalidOperationException("Zomboss did not create its Bungi payload.");
				}
				Check(bungi.skipBungeeTarget && !bungi.instance.hypnoses, "The fixture must exercise Zomboss' fixed-position, non-hypnotized Bungi variant.");
				Check(bungi.canBlock && !bungi.isGround && bungi.z > 50.0, $"The real Bungi must begin as an airborne, blockable payload; canBlock={bungi.canBlock}, isGround={bungi.isGround}, z={bungi.z}.");
				bungi.ImportNetworkSpawnState(new Dictionary { ["skipBungeeTarget"] = true });
				bungi.ImportVariantSave(bungi.ExportVariantSave());
				Check(await WaitUntil(() => GodotObject.IsInstanceValid(bungi) && bungi.isGround, 2.0), "The restored Zomboss Bungi must finish its real drop before targetability is asserted.");
				Check(bungi.HasHitBox && bungi.IsHitBoxEnabled && !bungi.instance.invincible && bungi.instance.canBeCollection && bungi.targetRegistrationComponent.canProjectileCheck && bungi.instance.maskFlags != 0, $"Network/save restore must make a landed non-hypnotized Zomboss Bungi detectable; hasHitBox={bungi.HasHitBox}, enabled={bungi.IsHitBoxEnabled}, invincible={bungi.instance.invincible}, collectible={bungi.instance.canBeCollection}, projectileCheck={bungi.targetRegistrationComponent.canProjectileCheck}, mask={bungi.instance.maskFlags}.");
				Check(bungi.gridPos == ProtectedGrid && bungi.GetLogicalGlobalPosition().DistanceTo(umbrella.GetLogicalGlobalPosition()) <= 0.5f, $"Zomboss' Bungi must descend over the real protected plant; bungiGrid={bungi.gridPos}, umbrellaGrid={umbrella.gridPos}.");
				Check(bungi.canBlock && bungi.isGround && bungi.z <= 50.0, $"The restored Bungi must finish its drop in the blockable landing band; canBlock={bungi.canBlock}, isGround={bungi.isGround}, z={bungi.z}.");
				Check(blockComponent.checkShape.TryGetWorldRect(umbrella.GlobalTransform, out var rect) && AabbShapeUtil.Intersects(rect, bungi.WorldHitRect), $"The production block AABB must overlap the Zomboss Bungi hit box; blockRect={rect}, bungiRect={bungi.WorldHitRect}.");
				List<TowerDefenseCharacter> charactersIntersectingRect = manager.GetCharactersIntersectingRect(rect);
				Check(charactersIntersectingRect.Contains(bungi), $"The production character registry must return the Zomboss Bungi to BlockComponent; candidateCount={charactersIntersectingRect.Count}.");
				Check(blockComponent.blockType.Contains(bungi.BlockType()), $"The umbrella must accept the Bungi block type; type={bungi.BlockType()}, accepted={string.Join(",", blockComponent.blockType)}.");
				blockComponent.SetAlive(alive: true);
				Check(await WaitUntil(() => !GodotObject.IsInstanceValid(bungi) || !bungi.canBlock, 4.0) && GodotObject.IsInstanceValid(bungi), "The live Umbrellaleaf BlockComponent must automatically detect and block the descending Bungi.");
				if (GodotObject.IsInstanceValid(bungi))
				{
					Check(_blockEvents == 1, $"The real umbrella must emit exactly one block event; actual={_blockEvents}.");
					Check(!bungi.canBlock && bungi.instance.invincible, $"Umbrella blocking must enter Bungi's protected rise path; canBlock={bungi.canBlock}, invincible={bungi.instance.invincible}.");
					Check(bungi.StateMachine?.CurrentStateHandle?.StableId == "zombie.bungi.rise", "The blocked Bungi must transition through its production rise state; state=" + bungi.StateMachine?.CurrentStateHandle?.StableId + ".");
					Check(!bungi.hasPlant && !bungi.grabOver, "A Bungi bounced by the umbrella must not grab the protected plant.");
				}
				Check(!umbrella.die && !umbrella.nearDie && !umbrella.isDestroy, "The protected real Umbrellaleaf must survive the bounce interaction.");
				Check(await WaitUntil(() => !GodotObject.IsInstanceValid(bungi) || bungi.IsQueuedForDeletion(), 3.0), "The bounced Zomboss Bungi must finish the real rise tween and leave the battlefield.");
				Check(GodotObject.IsInstanceValid(umbrella) && !umbrella.die && !umbrella.isDestroy, "The umbrella must remain alive after the Bungi exits.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewZombossBungiUmbrellaRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (blockComponent != null)
			{
				blockComponent.OnBlock -= CountBlock;
			}
			if (GodotObject.IsInstanceValid(_control?.characterNode))
			{
				foreach (Node child in _control.characterNode.GetChildren())
				{
					if (GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
			}
			await WaitFrames(6);
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
			if (GodotObject.IsInstanceValid(_control) && !_control.IsQueuedForDeletion())
			{
				_control.QueueFree();
			}
			RestoreFixtures();
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			foreach (Resource loadedResource in _loadedResources)
			{
				loadedResource?.Dispose();
			}
			_loadedResources.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks >= 17;
		GD.Print($"ZOMBOSS_BUNGI_UMBRELLA_RESULT passed={flag} checks={_checks} failures={_failures} block_events={_blockEvents}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void CountBlock()
	{
		_blockEvents++;
	}

	private static TowerDefenseZombieBungi FindBossBungi(TowerDefenseZombieBoss boss)
	{
		foreach (Variant bungee in boss.bungeeList)
		{
			if (bungee.AsGodotObject() is TowerDefenseZombieBungi towerDefenseZombieBungi && GodotObject.IsInstanceValid(towerDefenseZombieBungi))
			{
				return towerDefenseZombieBungi;
			}
		}
		return null;
	}

	private void SetupBattleFixture(TowerDefenseManager manager)
	{
		_control = new ZombossBungiUmbrellaRuntimeControlStub
		{
			Name = "ZombossBungiUmbrellaRuntimeControl",
			isGameRunning = false,
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
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j))?.Init(new TowerDefenseCellConfig());
			}
		}
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private static void RefreshRegistration(TowerDefenseManager manager, params TowerDefenseCharacter[] characters)
	{
		foreach (TowerDefenseCharacter towerDefenseCharacter in characters)
		{
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				manager.CharacterUnregister(towerDefenseCharacter);
				manager.CharacterRegister(towerDefenseCharacter);
			}
		}
	}

	private void RegisterFixture(string key, string packetPath, string characterPath)
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
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value2))
		{
			_previousCharacters[key] = value2;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(packetPath, null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(characterPath, null, ResourceLoader.CacheMode.Ignore);
		_loadedResources.Add(towerDefensePacketConfig);
		_loadedResources.Add(packedScene);
		instance.TOWERDEFENSE_PACKETS[key] = towerDefensePacketConfig;
		instance.TOWERDEFENSE_CHARCATERS[key] = packedScene;
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

	private async Task<bool> WaitUntil(Func<bool> predicate, double timeoutSeconds)
	{
		ulong deadline = Time.GetTicksMsec() + (ulong)Math.Ceiling(timeoutSeconds * 1000.0);
		while (Time.GetTicksMsec() <= deadline)
		{
			if (predicate())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return predicate();
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
			GD.PushError("[BugOverviewZombossBungiUmbrellaRuntimeTest] " + message);
		}
	}
}
