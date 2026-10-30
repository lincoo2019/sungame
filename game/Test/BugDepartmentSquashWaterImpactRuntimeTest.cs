using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentSquashWaterImpactRuntimeTest : Node
{
	private const string SquashPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Squash/Packet/PlantSquash.tres";

	private const string SquashScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Squash/Scene/TowerDefensePlantSquash.tscn";

	private const string LilyPadPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres";

	private const string LilyPadScenePath = "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Scene/TowerDefensePlantLilyPad.tscn";

	private const string SnorklePacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres";

	private const string SnorkleScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Base/TowerDefenseZombieSnorkle.tscn";

	private static readonly Vector2I ImpactGrid = new Vector2I(4, 2);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

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
		SquashWaterImpactControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			int num;
			_ = num - 1;
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0159;
				}
				RegisterRealFixtures();
				control = new SquashWaterImpactControlStub
				{
					Name = "SquashWaterImpactControl",
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
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseCellInstance waterCell = TowerDefenseManager.GetMapCell(ImpactGrid);
				Check(GodotObject.IsInstanceValid(waterCell) && waterCell.isWater && waterCell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER), "The impact scenario must use a real WATER map cell.");
				TowerDefensePlantLilyPad lilyPad = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres")?.Plant(ImpactGrid, playAudio: false) as TowerDefensePlantLilyPad;
				await WaitFrames(4);
				TowerDefensePlantSquash squash = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Squash/Packet/PlantSquash.tres")?.Plant(ImpactGrid, playAudio: false) as TowerDefensePlantSquash;
				await WaitFrames(4);
				TowerDefenseZombieSnorkle snorkle = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres")?.Plant(ImpactGrid, playAudio: false) as TowerDefenseZombieSnorkle;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(lilyPad) && lilyPad.config?.name == "PlantLilyPad", "The water stack must contain the corresponding real LilyPad object.");
				Check(GodotObject.IsInstanceValid(squash) && squash.config?.name == "PlantSquash", "The scenario must plant the corresponding real Squash scene on the LilyPad.");
				Check(GodotObject.IsInstanceValid(snorkle) && snorkle.config?.name == "ZombieSnorkle", "The impact target must be the corresponding real submerged Snorkle scene.");
				if (!GodotObject.IsInstanceValid(squash) || !GodotObject.IsInstanceValid(snorkle))
				{
					goto end_IL_0159;
				}
				SquashComponent squashRuntime = squash.componentManager.GetRuntime<SquashComponent>();
				BugDepartmentSquashWaterImpactRuntimeTest bugDepartmentSquashWaterImpactRuntimeTest = this;
				int condition;
				if (squashRuntime != null && !squashRuntime.IsReleased)
				{
					Array<TowerDefenseCharacterEventBase> eventList = squashRuntime.eventList;
					condition = ((eventList != null && eventList.Count > 0) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugDepartmentSquashWaterImpactRuntimeTest.Check((byte)condition != 0, "The real Squash must expose its authored smash runtime and hurt event.");
				if (squashRuntime == null || squashRuntime.IsReleased)
				{
					goto end_IL_0159;
				}
				squash.inGame = true;
				snorkle.inGame = true;
				squash.cell = waterCell;
				squash.gridPos = ImpactGrid;
				snorkle.gridPos = ImpactGrid;
				snorkle.GlobalPosition = squash.GlobalPosition;
				snorkle.inWater = true;
				snorkle.instance.maskFlags = 32;
				int num2 = 32;
				Check((squash.instance.collisionFlags & num2) != 0 && (snorkle.instance.maskFlags & num2) != 0, "The real Squash collision mask must overlap the real submerged target mask.");
				double hitpointsBefore = snorkle.instance.hitpoints;
				bool landed = false;
				squashRuntime.OnJumpDownSmash += delegate
				{
					landed = true;
				};
				squashRuntime.Execute(snorkle);
				Check(squashRuntime.IsRunning(), "The real Squash must enter its production ready/jump workflow against the submerged target.");
				for (int frame = 0; frame < 360; frame++)
				{
					if (landed)
					{
						break;
					}
					await WaitFrames(1);
				}
				Check(landed && squashRuntime.over, "The production Squash animation workflow must reach the real water landing action.");
				await WaitFrames(4);
				bool condition2 = !GodotObject.IsInstanceValid(snorkle) || snorkle.die || snorkle.isDestroy || snorkle.instance.hitpoints < hitpointsBefore;
				Check(condition2, "A Squash landing from a LilyPad must damage the real submerged zombie before water cleanup releases its hurt event.");
				Check(!GodotObject.IsInstanceValid(snorkle) || snorkle.die || snorkle.instance.hitpoints <= 0.0, "The authored 1800-damage Squash hit must defeat the 200-HP submerged Snorkle.");
				await WaitFrames(40);
				Check(!GodotObject.IsInstanceValid(squash) || squash.isDestroy || squash.IsQueuedForDeletion(), "The water impact must still clean up the one-use Squash after its damage resolves.");
				goto end_IL_0132;
				end_IL_0159:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentSquashWaterImpactRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0132;
			}
			return;
			end_IL_0132:;
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
		bool flag = _failures == 0 && _checks >= 11;
		GD.Print($"BUG_DEPARTMENT_SQUASH_WATER_IMPACT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
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
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(ImpactGrid.X, ImpactGrid.Y, ImpactGrid.X, ImpactGrid.Y),
			gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
			{
				TowerDefenseEnum.PLANTGRIDTYPE.WATER,
				TowerDefenseEnum.PLANTGRIDTYPE.AIR
			}
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

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantSquash", "res://Asset/Anime/Character/Plant/Chapter0/Squash/Packet/PlantSquash.tres");
		RegisterPacket("PlantLilyPad", "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres");
		RegisterPacket("ZombieSnorkle", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres");
		RegisterCharacter("PlantSquash", "res://Asset/Anime/Character/Plant/Chapter0/Squash/Scene/TowerDefensePlantSquash.tscn");
		RegisterCharacter("PlantLilyPad", "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Scene/TowerDefensePlantLilyPad.tscn");
		RegisterCharacter("ZombieSnorkle", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Base/TowerDefenseZombieSnorkle.tscn");
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugDepartmentSquashWaterImpactRuntimeTest] " + message);
		}
	}
}
