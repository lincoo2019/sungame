using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class HypnotistHologramTargetRuntimeTest : Node
{
	private const string HypnotistScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn";

	private const string PotQxPacketPath = "res://Asset/Anime/Character/Plant/Star/PotQX/Packet/PlantPotQX.tres";

	private const string PotQxScenePath = "res://Asset/Anime/Character/Plant/Star/PotQX/Scene/TowerDefensePlantPotQX.tscn";

	private const string SourcePacketPath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres";

	private const string SourceScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn";

	private static readonly Vector2I ProjectionSourceGrid = new Vector2I(4, 2);

	private static readonly Vector2I NormalTargetGrid = new Vector2I(6, 3);

	private int _checks;

	private int _failures;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		HypnotistHologramTargetRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_009e;
				}
				GameSaveManager.Instance.SetConfigValue("Backgrounder", true);
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				RegisterRealFixtures();
				control = new HypnotistHologramTargetRuntimeControlStub
				{
					Name = "HypnotistHologramRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = characterNode;
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
				TowerDefensePacketConfig sourcePacket = LoadPacket("res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres");
				TowerDefensePacketConfig potQxPacket = LoadPacket("res://Asset/Anime/Character/Plant/Star/PotQX/Packet/PlantPotQX.tres");
				TowerDefensePlant source = sourcePacket?.Plant(ProjectionSourceGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefensePlantPotQX potQx = potQxPacket?.Plant(ProjectionSourceGrid, playAudio: false) as TowerDefensePlantPotQX;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(source) && GodotObject.IsInstanceValid(potQx), "Real source plant and PotQX must share the projection-source cell.");
				if (!GodotObject.IsInstanceValid(source) || !GodotObject.IsInstanceValid(potQx))
				{
					goto end_IL_009e;
				}
				potQx.CreateHologramCharacter();
				await WaitFrames(6);
				Check(potQx.createCharacterList.Count == manager.gridNum.Y - 1, $"PotQX must create one projection on every other row; got {potQx.createCharacterList.Count}.");
				foreach (TowerDefenseCharacter createCharacter in potQx.createCharacterList)
				{
					Check(GodotObject.IsInstanceValid(createCharacter) && createCharacter.config?.name == "PlantSunflowerPea", "PotQX must create real SunflowerPea card projections.");
					Check(createCharacter.instance.hologram && !createCharacter.instance.canBeCollection && createCharacter.IsInGroup("Plant"), "Every real PotQX projection must be a Plant-group hologram excluded from target collection.");
				}
				source.instance.canBeCollection = false;
				potQx.instance.canBeCollection = false;
				TowerDefenseZombieHypnotist noTargetHypnotist = CreateHypnotist(characterNode);
				noTargetHypnotist.Spawn();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				Check(noTargetHypnotist.isDestroy || noTargetHypnotist.IsQueuedForDeletion(), "Hypnotist must find no target when every Plant-group node is a non-collectible PotQX projection or source.");
				TowerDefensePlant normalTarget = sourcePacket?.Plant(NormalTargetGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(normalTarget) && normalTarget.instance.canBeCollection, "A normal real plant must remain collectible for the positive target check.");
				TowerDefenseZombieHypnotist towerDefenseZombieHypnotist = CreateHypnotist(characterNode);
				towerDefenseZombieHypnotist.Spawn();
				Check(!towerDefenseZombieHypnotist.isDestroy && !towerDefenseZombieHypnotist.IsQueuedForDeletion(), "Hypnotist must remain active when a normal collectible plant exists.");
				Check(towerDefenseZombieHypnotist.gridPos == NormalTargetGrid, $"Hypnotist must choose the normal plant rather than a projection; got {towerDefenseZombieHypnotist.gridPos}.");
				goto end_IL_007b;
				end_IL_009e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[HypnotistHologramTargetRuntimeTest] Unexpected exception: {value}");
				goto end_IL_007b;
			}
			return;
			end_IL_007b:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"HYPNOTIST_HOLOGRAM_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		towerDefenseBattleFeatureMap.mapConfig.gridNum = gridNum;
		towerDefenseBattleFeatureMap.mapConfig.gridBeginPos = Vector2.Zero;
		towerDefenseBattleFeatureMap.mapConfig.gridSize = new Vector2(100f, 76f);
		towerDefenseBattleFeatureMap.mapConfig.plantOffset = 50.0;
		towerDefenseBattleFeatureMap.groundRect = new Rect2(Vector2.Zero, new Vector2(100 * gridNum.X, 76 * gridNum.Y));
		towerDefenseBattleFeatureMap.rect = new Rect2(-1000f, -1000f, 4000f, 4000f);
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int i = 1; i <= gridNum.Y; i++)
		{
			towerDefenseBattleFeatureMap.lineUse[i] = true;
		}
		towerDefenseBattleFeatureMap.config.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y)
		});
		for (int j = 1; j <= gridNum.Y; j++)
		{
			towerDefenseBattleFeatureMap.config.lineUse.Add(j);
		}
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static TowerDefenseZombieHypnotist CreateHypnotist(Node parent)
	{
		TowerDefenseZombieHypnotist towerDefenseZombieHypnotist = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieHypnotist>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(towerDefenseZombieHypnotist))
		{
			towerDefenseZombieHypnotist.inGame = false;
			towerDefenseZombieHypnotist.editorPreviewMode = true;
			parent.AddChild(towerDefenseZombieHypnotist, forceReadableName: false, InternalMode.Disabled);
		}
		return towerDefenseZombieHypnotist;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPotQX", "res://Asset/Anime/Character/Plant/Star/PotQX/Packet/PlantPotQX.tres");
		RegisterPacket("PlantSunflowerPea", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres");
		RegisterCharacter("PlantPotQX", "res://Asset/Anime/Character/Plant/Star/PotQX/Scene/TowerDefensePlantPotQX.tscn");
		RegisterCharacter("PlantSunflowerPea", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn");
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
			GD.PushError("[HypnotistHologramTargetRuntimeTest] " + message);
		}
	}
}
