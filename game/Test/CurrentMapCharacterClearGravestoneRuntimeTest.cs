using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class CurrentMapCharacterClearGravestoneRuntimeTest : Node
{
	private const string PacketKey = "GraveStoneDefault";

	private const string PacketPath = "res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres";

	private const string ScenePath = "res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn";

	private const int ExpectedChecks = 9;

	private int _checks;

	private int _failures;

	private Resource _previousPacket;

	private Resource _previousCharacter;

	private bool _packetWasMissing;

	private bool _characterWasMissing;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		CurrentMapCharacterClearGravestoneControlStub control = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseMap currentMap = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required gameplay autoloads are unavailable.");
				}
				RegisterRealResources();
				control = new CurrentMapCharacterClearGravestoneControlStub
				{
					Name = "CurrentMapCharacterClearGravestoneControl",
					isGameRunning = true,
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
				currentMap = (mapFeature.currentMap = new TowerDefenseMap
				{
					Name = "CurrentMap"
				});
				TowerDefenseLevelEventCurrentMapCharacterClear clearEvent = new TowerDefenseLevelEventCurrentMapCharacterClear();
				TowerDefenseGravestone existing = LoadPacket()?.Plant(new Vector2I(5, 3), playAudio: false) as TowerDefenseGravestone;
				Check(GodotObject.IsInstanceValid(existing), "The fixture must create a real already-entered gravestone.");
				await WaitFrames(3);
				Check(CountLiveGravestones(manager) == 1, "The real default gravestone must be alive in the persistent Gravestone group.");
				Check(GetRegisteredGravestones(manager).Count == 0, "The default gravestone fixture must reproduce its intentional targeting-registry exclusion.");
				clearEvent.Execute();
				Check(GodotObject.IsInstanceValid(existing) && existing.isDestroy && existing.die, "CharacterClear must enter the normal destroy lifecycle for an existing gravestone.");
				await WaitFrames(3);
				Check(CountLiveGravestones(manager) == 0, "The existing gravestone must leave the battlefield after CharacterClear.");
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(5, 3));
				Check(GodotObject.IsInstanceValid(mapCell), "The real gravestone must use a valid map cell.");
				Check(!CellHasLiveGravestone(mapCell), "The cleared gravestone must leave map-cell occupancy.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[CurrentMapCharacterClearGravestoneRuntimeTest] Unexpected exception: {value}");
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
			if (mapFeature != null)
			{
				mapFeature.mapControl = null;
				mapFeature.currentMap = null;
				mapFeature.Destroy();
			}
			if (GodotObject.IsInstanceValid(currentMap))
			{
				currentMap.Free();
			}
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealResources();
			await WaitFrames(8);
		}
		bool flag = _failures == 0 && _checks == 9;
		GD.Print($"CURRENT_MAP_CHARACTER_CLEAR_GRAVESTONE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum
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

	private static TowerDefensePacketConfig LoadPacket()
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static List<TowerDefenseGravestone> GetRegisteredGravestones(TowerDefenseManager manager)
	{
		List<TowerDefenseGravestone> list = new List<TowerDefenseGravestone>();
		foreach (Variant item in manager.GetCharacter())
		{
			if (item.AsGodotObject() is TowerDefenseGravestone towerDefenseGravestone && GodotObject.IsInstanceValid(towerDefenseGravestone) && !towerDefenseGravestone.isDestroy)
			{
				list.Add(towerDefenseGravestone);
			}
		}
		return list;
	}

	private static int CountLiveGravestones(TowerDefenseManager manager)
	{
		int num = 0;
		foreach (Node item in manager.GetTree().GetNodesInGroup("Gravestone"))
		{
			if (item is TowerDefenseGravestone towerDefenseGravestone && GodotObject.IsInstanceValid(towerDefenseGravestone) && !towerDefenseGravestone.isDestroy)
			{
				num++;
			}
		}
		return num;
	}

	private static bool CellHasLiveGravestone(TowerDefenseCellInstance cell)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return false;
		}
		cell.ClearEmpty();
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character is TowerDefenseGravestone && GodotObject.IsInstanceValid(character) && !character.isDestroy)
			{
				return true;
			}
		}
		return false;
	}

	private void RegisterRealResources()
	{
		ResourceManager instance = ResourceManager.Instance;
		_packetWasMissing = !instance.TOWERDEFENSE_PACKETS.TryGetValue("GraveStoneDefault", out _previousPacket);
		_characterWasMissing = !instance.TOWERDEFENSE_CHARCATERS.TryGetValue("GraveStoneDefault", out _previousCharacter);
		instance.TOWERDEFENSE_PACKETS["GraveStoneDefault"] = ResourceLoader.Load<Resource>("res://Asset/Anime/Character/GraveStone/Default/Packet/GraveStoneDefault.tres", null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_CHARCATERS["GraveStoneDefault"] = ResourceLoader.Load<Resource>("res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn", null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreRealResources()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			if (_packetWasMissing)
			{
				instance.TOWERDEFENSE_PACKETS.Remove("GraveStoneDefault");
			}
			else if (GodotObject.IsInstanceValid(_previousPacket))
			{
				instance.TOWERDEFENSE_PACKETS["GraveStoneDefault"] = _previousPacket;
			}
			if (_characterWasMissing)
			{
				instance.TOWERDEFENSE_CHARCATERS.Remove("GraveStoneDefault");
			}
			else if (GodotObject.IsInstanceValid(_previousCharacter))
			{
				instance.TOWERDEFENSE_CHARCATERS["GraveStoneDefault"] = _previousCharacter;
			}
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
			GD.PushError("[CurrentMapCharacterClearGravestoneRuntimeTest] " + message);
		}
	}
}
