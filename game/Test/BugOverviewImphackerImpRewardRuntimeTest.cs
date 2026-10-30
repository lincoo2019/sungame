using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewImphackerImpRewardRuntimeTest : Node
{
	private const string ImpPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/Base/ZombieImp.tres";

	private const string ImpScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Base/TowerDefenseZombieImp.tscn";

	private const string HackerScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Imphacker/Scene/TowerDefenseZombieImphacker.tscn";

	private const string PeaPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ImphackerImpRewardRuntimeControlStub control = null;
		TowerDefenseInGameSeedBank seedBank = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseMapControl mapControl = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_010d;
				}
				RegisterFixtures();
				TowerDefenseLevelConfig levelConfig = new TowerDefenseLevelConfig
				{
					finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM,
					packetBankMethod = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE,
					packetColdDownUse = false
				};
				control = new ImphackerImpRewardRuntimeControlStub
				{
					Name = "ImphackerImpRewardRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = levelConfig
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
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseBattleFeatureSun sunFeature = new TowerDefenseBattleFeatureSun
				{
					control = control
				};
				sunFeature.Init(new Dictionary { ["Begin"] = 100L });
				control.featureDictionary[new StringName("Sun")] = sunFeature;
				seedBank = new TowerDefenseInGameSeedBank();
				Check(seedBank.TryBindSunAccount(EconomyAccountId.Local), "The real IZM seed bank fixture must bind the local economy account.");
				TowerDefenseBattleFeatureSeedBank value = new TowerDefenseBattleFeatureSeedBank
				{
					control = control,
					seedBank = seedBank,
					config = new TowerDefenseLevelSeedBankConfig
					{
						method = TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE
					}
				};
				control.featureDictionary[new StringName("SeedBank")] = value;
				TowerDefenseInGamePacketShow firstSlot = await CreateSlot(control.characterNode, LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres"));
				TowerDefenseInGamePacketShow secondSlot = await CreateSlot(control.characterNode, LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres"));
				seedBank.packetList.Add(firstSlot);
				seedBank.packetList.Add(secondSlot);
				Check(firstSlot.HasSunAccount && secondSlot.HasSunAccount && firstSlot.SunAccountId == EconomyAccountId.Local && secondSlot.SunAccountId == EconomyAccountId.Local, "Both real card slots must use the seed bank's local economy account.");
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieImp");
				int ordinaryImpCost = packetConfig?.GetCost() ?? (-2147483648);
				int ordinaryImpOverrideCost = packetConfig?.overrideCost ?? (-2147483648);
				Check(GodotObject.IsInstanceValid(packetConfig) && ordinaryImpCost >= 0 && !GodotObject.IsInstanceValid(packetConfig._override), "The ordinary real Imp packet must start at its configured non-negative cost without a runtime override.");
				TowerDefenseZombieImphacker hacker = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/Imphacker/Scene/TowerDefenseZombieImphacker.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieImphacker>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(hacker), "The regression must instantiate the real Hacker Imp scene.");
				if (!GodotObject.IsInstanceValid(hacker))
				{
					goto end_IL_010d;
				}
				hacker.inGame = false;
				hacker.editorPreviewMode = false;
				control.characterNode.AddChild(hacker, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				Check(manager.IsIZMMode(), "The real Hacker Imp packet transformation must run under IZM rules.");
				hacker.SetPacket();
				await WaitFrames(12);
				TowerDefenseInGamePacketShow hackedSlot = FindHackedSlot(firstSlot, secondSlot);
				Check(GodotObject.IsInstanceValid(hackedSlot) && hackedSlot.config?.saveKey == "ZombieImp", "The real Hacker Imp must replace exactly one of the two card slots with ZombieImp.");
				if (!GodotObject.IsInstanceValid(hackedSlot))
				{
					goto end_IL_010d;
				}
				Check(hackedSlot.config.GetCost() == 0 && hackedSlot.itemCost == 0, $"The temporary Hacker Imp card must be free and non-negative; config={hackedSlot.config.GetCost()}, item={hackedSlot.itemCost}.");
				Check(hackedSlot.itemCostLabel?.Text == "0", "The live temporary Imp card must display 0 instead of -50; label=" + hackedSlot.itemCostLabel?.Text + ".");
				Check(hackedSlot.alive, "The account-bound free Imp card must remain selectable in IZM.");
				hackedSlot.alive = true;
				SunSpendReceipt spendReceipt;
				bool flag = hackedSlot.TryBeginPendingUse(out spendReceipt);
				Check(flag && (spendReceipt?.IsActive ?? false), "A free hacked Imp card must begin a real pending use without a negative spend.");
				Check(spendReceipt != null && spendReceipt.TryRollback() && sunFeature.GetSun(EconomyAccountId.Local) == 100, "Cancelling the pending placement must not grant the 50-sun reward.");
				hackedSlot.alive = false;
				Check(hackedSlot.Plant(TestGrid) == null && sunFeature.GetSun(EconomyAccountId.Local) == 100, "A rejected card use must not spawn an Imp or grant Sun.");
				hackedSlot.alive = true;
				control.isGameRunning = true;
				TowerDefenseCharacter spawned = hackedSlot.Plant(TestGrid);
				await WaitFrames(6);
				Check(spawned is TowerDefenseZombieImp && GodotObject.IsInstanceValid(spawned) && spawned.gridPos == TestGrid, "A successful use must create the real base Imp at the requested IZM grid.");
				Check(sunFeature.GetSun(EconomyAccountId.Local) == 150, $"Only the successful real placement must credit exactly 50 Sun; balance={sunFeature.GetSun(EconomyAccountId.Local)}.");
				Check(hackedSlot.config?.saveKey != "ZombieImp", "The one-use hacked card must restore the original slot after successful placement.");
				TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig("ZombieImp");
				Check(GodotObject.IsInstanceValid(packetConfig2) && packetConfig2.GetCost() == ordinaryImpCost && packetConfig2.overrideCost == ordinaryImpOverrideCost && !GodotObject.IsInstanceValid(packetConfig2._override), $"The temporary free/reward override must not leak into ordinary Imp packets; before={ordinaryImpCost}, after={packetConfig2?.GetCost()}.");
				goto end_IL_00ee;
				end_IL_010d:;
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[BugOverviewImphackerImpRewardRuntimeTest] Unexpected exception: {value2}");
				goto end_IL_00ee;
			}
			return;
			end_IL_00ee:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(control?.characterNode))
			{
				foreach (Node child in control.characterNode.GetChildren())
				{
					if (!child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
			}
			await WaitFrames(4);
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
			if (GodotObject.IsInstanceValid(seedBank))
			{
				seedBank.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreFixtures();
			await WaitFrames(4);
		}
		bool flag2 = _failures == 0 && _checks >= 17;
		GD.Print($"IMPHACKER_IMP_REWARD_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private async Task<TowerDefenseInGamePacketShow> CreateSlot(Node parent, TowerDefensePacketConfig config)
	{
		TowerDefenseInGamePacketShow slot = TowerDefenseManager.CreatePacketShow();
		parent.AddChild(slot, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(2);
		slot.Init(config);
		slot.originalSaveKey = config.saveKey;
		slot.useCost = true;
		slot.start = true;
		slot.coldDownOpen = false;
		slot.alive = true;
		Check(slot.TryBindSunAccount(EconomyAccountId.Local), "The real " + config.saveKey + " slot must bind the local economy account.");
		return slot;
	}

	private static TowerDefenseInGamePacketShow FindHackedSlot(TowerDefenseInGamePacketShow first, TowerDefenseInGamePacketShow second)
	{
		if (first?.config?.saveKey == "ZombieImp")
		{
			return first;
		}
		if (second?.config?.saveKey == "ZombieImp")
		{
			return second;
		}
		return null;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
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
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterFixtures()
	{
		RegisterPacket("ZombieImp", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/Base/ZombieImp.tres");
		RegisterCharacter("ZombieImp", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Base/TowerDefenseZombieImp.tscn");
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
			GD.PushError("[BugOverviewImphackerImpRewardRuntimeTest] " + message);
		}
	}
}
