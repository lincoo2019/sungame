using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGardenerBarrowExitDirectionRuntimeTest : Node
{
	private const string GardenerScenePath = "res://Asset/Anime/Character/Zombie/Puzzle/Gardener/Scene/TowerDefenseZombieGardener.tscn";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private const string PlantPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewGardenerBarrowExitDirectionControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseZombieGardener gardener = null;
		TowerDefensePlant plant = null;
		Node2D movementSource = null;
		int plantDestroyNotifications = 0;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00f0;
				}
				control = new BugOverviewGardenerBarrowExitDirectionControlStub
				{
					Name = "GardenerBarrowExitDirectionControl",
					isGameRunning = false,
					isInit = true
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
				TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = CreateMapFeature(mapControl, manager.gridNum);
				towerDefenseBattleFeatureMap.control = control;
				control.featureDictionary[new StringName("Map")] = towerDefenseBattleFeatureMap;
				gardener = LoadCharacter<TowerDefenseZombieGardener>("res://Asset/Anime/Character/Zombie/Puzzle/Gardener/Scene/TowerDefenseZombieGardener.tscn");
				plant = LoadCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
				if (GodotObject.IsInstanceValid(plant))
				{
					plant.packet = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
				}
				Check(GodotObject.IsInstanceValid(gardener) && gardener.config?.name == "ZombieGardener", "The reported Gardener Zombie scene must instantiate.");
				Check(GodotObject.IsInstanceValid(plant) && plant.config?.name == "PlantWallnut", "The fixture must use a real non-spike plant target.");
				if (!GodotObject.IsInstanceValid(gardener) || !GodotObject.IsInstanceValid(plant))
				{
					goto end_IL_00f0;
				}
				Vector2 overlapPosition = new Vector2(400f, 252f);
				Vector2I overlapGrid = new Vector2I(4, 2);
				gardener.inGame = true;
				gardener.editorPreviewMode = false;
				gardener.gridPos = overlapGrid;
				gardener.GlobalPosition = overlapPosition;
				plant.inGame = true;
				plant.editorPreviewMode = false;
				plant.gridPos = overlapGrid;
				plant.GlobalPosition = overlapPosition;
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(gardener, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				gardener.ProcessMode = ProcessModeEnum.Disabled;
				plant.ProcessMode = ProcessModeEnum.Disabled;
				gardener.gridPos = overlapGrid;
				gardener.GlobalPosition = overlapPosition;
				plant.gridPos = overlapGrid;
				plant.GlobalPosition = overlapPosition;
				await WaitFrames(2);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(overlapGrid);
				Check(GodotObject.IsInstanceValid(mapCell), "The focused map fixture must expose the Gardener pickup cell.");
				if (!GodotObject.IsInstanceValid(mapCell))
				{
					goto end_IL_00f0;
				}
				plant.cell = mapCell;
				gardener.cell = mapCell;
				mapCell.CharacterPlant(plant.packet, plant);
				control.RegisterSyncCharacter(71022, plant);
				plant.OnDestroy += delegate
				{
					plantDestroyNotifications++;
				};
				control.isGameRunning = true;
				AttackComponent attackComponent = gardener.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				GroundMoveComponent groundMoveComponent = gardener.groundMoveComponent;
				Check(attackComponent != null && !attackComponent.IsReleased, "The real wide barrow attack component must be active.");
				Check(groundMoveComponent != null && !groundMoveComponent.IsReleased && GodotObject.IsInstanceValid(groundMoveComponent.groundNode), "The real Gardener GroundMoveComponent and authored GroundSlot must resolve.");
				if (attackComponent == null || attackComponent.IsReleased || groundMoveComponent == null || groundMoveComponent.IsReleased)
				{
					goto end_IL_00f0;
				}
				movementSource = new Node2D
				{
					Name = "DeterministicGroundSlot"
				};
				AddChild(movementSource, forceReadableName: false, InternalMode.Disabled);
				groundMoveComponent.groundNode = movementSource;
				groundMoveComponent.delay = 0f;
				groundMoveComponent.SetAlive(true);
				gardener.sprite.pause = false;
				gardener.sprite.blend = false;
				groundMoveComponent.RefreshDirectionCache();
				movementSource.Position = Vector2.Zero;
				groundMoveComponent.BatchUpdate(0.016);
				float x = ((gardener.spriteGroup.Scale.X * gardener.Scale.X * gardener.sprite.Scale.X >= 0f) ? 5f : (-5f));
				float x2 = gardener.GlobalPosition.X;
				movementSource.Position += new Vector2(x, 0f);
				groundMoveComponent.BatchUpdate(0.016);
				Check(gardener.GlobalPosition.X < x2 - 0.01f, "The real movement cache must be primed with a leftward pre-pickup step.");
				attackComponent.groundRight = 1000.0;
				attackComponent.alive = true;
				attackComponent.timer = 0.0;
				attackComponent.checkIntrevalNow = 0;
				Check(attackComponent.CanAttack() && attackComponent.target == plant, "The real barrow targeter must acquire the overlapping Wall-nut.");
				float num = Mathf.Sign(gardener.Scale.X);
				gardener.WalkProcessing(0.016);
				Check(gardener.barrowPlant == plant && !plant.inGame && !gardener.canBarrow, "The real walk behavior must carry the acquired plant.");
				Check((float)Mathf.Sign(gardener.Scale.X) == 0f - num, "Carrying the plant must turn the Gardener toward its exit.");
				Check(plantDestroyNotifications == 0, "Temporarily carrying a live plant must not emit its destroy lifecycle event.");
				Check(control._syncCharacters.TryGetValue(71022, out var value) && value == plant, "A carried plant must retain its existing replication registration.");
				float num2 = 0f;
				for (int num3 = 0; num3 < 3; num3++)
				{
					float x3 = gardener.GlobalPosition.X;
					movementSource.Position += new Vector2(x, 0f);
					groundMoveComponent.BatchUpdate(0.016);
					float num4 = gardener.GlobalPosition.X - x3;
					if (Mathf.Abs(num4) > 0.01f)
					{
						num2 = num4;
						break;
					}
				}
				Check(num2 > 0.01f, $"The first effective movement after turning must head right toward the exit; deltaX={num2}.");
				gardener.Plant();
				Check(!GodotObject.IsInstanceValid(gardener.barrowPlant) && plant.inGame && plant.componentAlive && plant.cell == mapCell && mapCell.characterList.Contains(plant), "Putting the carried plant down must restore its live map-cell ownership.");
				Check(control._syncCharacters.TryGetValue(71022, out var value2) && value2 == plant && manager.characterRegistry.GetActiveCharacters().Contains(plant), "Putting the plant down must leave both battle and replication registrations intact.");
				goto end_IL_00de;
				end_IL_00f0:;
			}
			catch (Exception value3)
			{
				_failures++;
				GD.PushError($"[BugOverviewGardenerBarrowExitDirectionRuntimeTest] Unexpected exception: {value3}");
				goto end_IL_00de;
			}
			return;
			end_IL_00de:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(gardener))
			{
				gardener.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(movementSource))
			{
				movementSource.QueueFree();
			}
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"GARDENER_BARROW_EXIT_DIRECTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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

	private static T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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
			GD.PushError("[BugOverviewGardenerBarrowExitDirectionRuntimeTest] " + message);
		}
	}
}
