using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewPanGoldBeanReplacementRuntimeTest : Node
{
	private const string PanGoldBeanPacketPath = "res://Asset/Anime/Character/Plant/Chapter7/PanGoldBean/Packet/PlantPanGoldBean.tres";

	private const string PanGoldBeanScenePath = "res://Asset/Anime/Character/Plant/Chapter7/PanGoldBean/Scene/TowerDefensePlantPanGoldBean.tscn";

	private const string PeaShooterPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres";

	private const string PeaShooterScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn";

	private const string QueenSunFlowerPacketPath = "res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Packet/PlantQueenSunFlower.tres";

	private const string QueenSunFlowerScenePath = "res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn";

	private static readonly Vector2I ReplicaGrid = new Vector2I(3, 2);

	private static readonly Vector2I AuthorityGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<Resource> _registeredResources = new List<Resource>();

	private TowerDefensePacketBankData _previousGeneralPlantBank;

	private bool _generalPlantBankWasMissing;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousMultiplayerMode = Global.Instance?.isMultiplayerMode ?? false;
		bool previousHost = MultiPlayerManager.Instance?.isHost ?? false;
		PanGoldBeanReplacementRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(Global.Instance) && GodotObject.IsInstanceValid(MultiPlayerManager.Instance), "The runtime fixture requires the battle, resource, global, and multiplayer autoloads.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
				{
					goto end_IL_00ff;
				}
				RegisterRealFixtures();
				control = new PanGoldBeanReplacementRuntimeControlStub
				{
					Name = "PanGoldBeanReplacementRuntimeControl",
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
				TowerDefensePacketConfig peaPacket = TowerDefenseManager.GetPacketConfig("PlantPeaShooter");
				TowerDefensePacketConfig beanPacket = TowerDefenseManager.GetPacketConfig("PlantPanGoldBean");
				TowerDefensePacketConfig goldPacket = TowerDefenseManager.GetPacketConfig("PlantQueenSunFlower");
				Check(GodotObject.IsInstanceValid(peaPacket) && GodotObject.IsInstanceValid(beanPacket) && GodotObject.IsInstanceValid(goldPacket), "The regression must load the real PeaShooter, PanGoldBean, and QueenSunFlower packets.");
				if (!GodotObject.IsInstanceValid(peaPacket) || !GodotObject.IsInstanceValid(beanPacket) || !GodotObject.IsInstanceValid(goldPacket))
				{
					goto end_IL_00ff;
				}
				await VerifyReplicaCannotReplace(peaPacket, beanPacket);
				await VerifyAuthoritativeReplacement(control, peaPacket, beanPacket, goldPacket);
				goto end_IL_00ed;
				end_IL_00ff:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPanGoldBeanReplacementRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00ed;
			}
			return;
			end_IL_00ed:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isMultiplayerMode = previousMultiplayerMode;
			}
			if (GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
			{
				MultiPlayerManager.Instance.isHost = previousHost;
			}
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
			await WaitFrames(6);
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks >= 20;
		GD.Print($"PAN_GOLD_BEAN_REPLACEMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyReplicaCannotReplace(TowerDefensePacketConfig peaPacket, TowerDefensePacketConfig beanPacket)
	{
		TowerDefensePlant original = peaPacket.Plant(ReplicaGrid, playAudio: false) as TowerDefensePlant;
		await WaitFrames(4);
		TowerDefensePlantPanGoldBean bean = beanPacket.Plant(ReplicaGrid, playAudio: false) as TowerDefensePlantPanGoldBean;
		await WaitFrames(4);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(ReplicaGrid);
		Check(GodotObject.IsInstanceValid(original) && GodotObject.IsInstanceValid(bean) && cell.HasCharacter("PlantPeaShooter") && cell.HasCharacter("PlantPanGoldBean"), "The client-authority scenario must start with a real plant under a real PanGoldBean.");
		Global.Instance.isMultiplayerMode = true;
		MultiPlayerManager.Instance.isHost = false;
		bean.Explode();
		Global.Instance.isMultiplayerMode = false;
		await WaitFrames(3);
		Check(GodotObject.IsInstanceValid(original) && !original.IsQueuedForDeletion(), "A read-only multiplayer client must not retire the original plant.");
		Check(cell.HasCharacter("PlantPeaShooter"), "A read-only multiplayer client must preserve original grid occupancy.");
		Check(!cell.HasCharacter("PlantQueenSunFlower"), "A read-only multiplayer client must not roll or create a gold replacement.");
	}

	private async Task VerifyAuthoritativeReplacement(PanGoldBeanReplacementRuntimeControlStub control, TowerDefensePacketConfig peaPacket, TowerDefensePacketConfig beanPacket, TowerDefensePacketConfig goldPacket)
	{
		TowerDefensePlant original = peaPacket.Plant(AuthorityGrid, playAudio: false) as TowerDefensePlant;
		await WaitFrames(4);
		TowerDefensePlantPanGoldBean bean = beanPacket.Plant(AuthorityGrid, playAudio: false) as TowerDefensePlantPanGoldBean;
		await WaitFrames(4);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(AuthorityGrid);
		Check(GodotObject.IsInstanceValid(original) && GodotObject.IsInstanceValid(bean) && cell.HasCharacter("PlantPeaShooter") && cell.HasCharacter("PlantPanGoldBean"), "The authoritative scenario must start with a real plant under a real PanGoldBean.");
		Check(!cell.CanPacketPlant(goldPacket), "The original plant must own the ground slot before PanGoldBean begins the transaction.");
		original.cost = 0.0;
		ulong num = FindReplacementSeed();
		Check(num != 0, "The fixture must find a deterministic seed for PanGoldBean's five-percent replacement branch.");
		GD.Seed(num);
		bean.Explode();
		Check(!cell.GetCharacterList().Contains(original), "PanGoldBean must detach the original occupancy before requesting the gold plant.");
		Check(GodotObject.IsInstanceValid(original) && !original.IsQueuedForDeletion(), "The transaction must keep the original alive until the deferred replacement becomes ready.");
		Check(CountActivePlantsAtGrid(control.characterNode, AuthorityGrid) == 1, "The synchronous phase must leave exactly one active non-bean plant at the target grid.");
		int maxActivePlants = CountActivePlantsAtGrid(control.characterNode, AuthorityGrid);
		for (int frame = 0; frame < 6; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			maxActivePlants = Math.Max(maxActivePlants, CountActivePlantsAtGrid(control.characterNode, AuthorityGrid));
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			maxActivePlants = Math.Max(maxActivePlants, CountActivePlantsAtGrid(control.characterNode, AuthorityGrid));
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		TowerDefenseCharacter towerDefenseCharacter = null;
		foreach (TowerDefenseCharacter character in cell.GetCharacterList())
		{
			if (GodotObject.IsInstanceValid(character) && !(character.config.name == "PlantPanGoldBean"))
			{
				list.Add(character);
				if (character.config.name == "PlantQueenSunFlower")
				{
					towerDefenseCharacter = character;
				}
			}
		}
		Check(maxActivePlants <= 1, $"PanGoldBean replacement must never expose two active plants at one grid; observed {maxActivePlants}.");
		Check(list.Count == 1 && GodotObject.IsInstanceValid(towerDefenseCharacter), "The completed cell transaction must contain exactly one QueenSunFlower replacement.");
		Check(!GodotObject.IsInstanceValid(original) || original.IsQueuedForDeletion(), "The original plant must be retired once the gold replacement is ready.");
		Check(CountActivePlantsAtGrid(control.characterNode, AuthorityGrid) == 1, "The character tree must contain exactly one active non-bean plant after finalization.");
		Check(!cell.CanPacketPlant(peaPacket), "The gold replacement must own the ground slot and reject another ground plant.");
		Array<TowerDefenseCharacter> characterListSave = cell.GetCharacterListSave();
		int num2 = 0;
		bool flag = false;
		bool flag2 = false;
		foreach (TowerDefenseCharacter item in characterListSave)
		{
			if (GodotObject.IsInstanceValid(item) && !(item.config.name == "PlantPanGoldBean"))
			{
				num2++;
				flag |= item.config.name == "PlantQueenSunFlower";
				flag2 |= item.config.name == "PlantPeaShooter";
			}
		}
		Check(num2 == 1 && flag && !flag2, "Progress-save enumeration must include only the gold plant, never the detached original.");
		TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = new TowerDefenseCharacterSaveConfigCSharp();
		towerDefenseCharacterSaveConfigCSharp.SaveCharacter(towerDefenseCharacter);
		Check(towerDefenseCharacterSaveConfigCSharp.packetName == "PlantQueenSunFlower" && towerDefenseCharacterSaveConfigCSharp.gridPos == AuthorityGrid, "The replacement's progress payload must identify the gold plant at the original grid.");
		Check(towerDefenseCharacter.packet.saveKey == "PlantQueenSunFlower", "The replacement must retain the stable gold packet identity used by save and network restore.");
		Check(towerDefenseCharacter.instance.wakeUp, "The finalized replacement must be awake before its multiplayer spawn state is published.");
		towerDefenseCharacterSaveConfigCSharp.Dispose();
	}

	private static int CountActivePlantsAtGrid(Node2D characterNode, Vector2I gridPos)
	{
		int num = 0;
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is TowerDefensePlant towerDefensePlant && GodotObject.IsInstanceValid(towerDefensePlant) && !towerDefensePlant.IsQueuedForDeletion() && !(towerDefensePlant.gridPos != gridPos) && !(towerDefensePlant.config.name == "PlantPanGoldBean"))
			{
				num++;
			}
		}
		return num;
	}

	private static ulong FindReplacementSeed()
	{
		for (ulong num = 1uL; num <= 10000; num++)
		{
			GD.Seed(num);
			if ((double)GD.Randf() < 0.05)
			{
				return num;
			}
		}
		return 0uL;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f)
			}
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

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPanGoldBean", "res://Asset/Anime/Character/Plant/Chapter7/PanGoldBean/Packet/PlantPanGoldBean.tres");
		RegisterPacket("PlantPeaShooter", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres");
		RegisterPacket("PlantQueenSunFlower", "res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Packet/PlantQueenSunFlower.tres");
		RegisterCharacter("PlantPanGoldBean", "res://Asset/Anime/Character/Plant/Chapter7/PanGoldBean/Scene/TowerDefensePlantPanGoldBean.tscn");
		RegisterCharacter("PlantPeaShooter", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
		RegisterCharacter("PlantQueenSunFlower", "res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn");
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("GeneralPlant", out var value))
		{
			_previousGeneralPlantBank = value;
		}
		else
		{
			_generalPlantBankWasMissing = true;
		}
		instance.TOWERDEFENSE_PACKETBANKS["GeneralPlant"] = new TowerDefensePacketBankData
		{
			category = new Dictionary { ["Gold"] = new Array<string> { "PlantQueenSunFlower" } }
		};
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
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(towerDefensePacketConfig);
		instance.TOWERDEFENSE_PACKETS[key] = towerDefensePacketConfig;
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
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(packedScene);
		instance.TOWERDEFENSE_CHARCATERS[key] = packedScene;
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
		if (_generalPlantBankWasMissing)
		{
			instance.TOWERDEFENSE_PACKETBANKS.Remove("GeneralPlant");
		}
		else
		{
			instance.TOWERDEFENSE_PACKETBANKS["GeneralPlant"] = _previousGeneralPlantBank;
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
			GD.PushError("[BugOverviewPanGoldBeanReplacementRuntimeTest] " + message);
		}
	}
}
