using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewDoomTanglekelpZombieDamagePositionRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/DoomTanglekelp/TowerDefenseZombieSnorkleDoomTanglekelp.tscn";

	private const string PacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkleDoomTanglekelp.tres";

	private static readonly Vector2I TestGrid = new Vector2I(7, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		DoomTanglekelpZombieDamagePositionRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombieSnorkleDoomTanglekelp zombie = null;
		PackedScene scene = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
				}
				control = new DoomTanglekelpZombieDamagePositionRuntimeControlStub
				{
					Name = "DoomTanglekelpZombieDamagePositionControl",
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
				mapFeature = CreateWaterMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkleDoomTanglekelp.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/DoomTanglekelp/TowerDefenseZombieSnorkleDoomTanglekelp.tscn", null, ResourceLoader.CacheMode.Ignore);
				zombie = scene?.Instantiate<TowerDefenseZombieSnorkleDoomTanglekelp>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieSnorkleDoomTanglekelp", "The real packet and Snorkle DoomTanglekelp Zombie scene must load.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(zombie))
				{
					throw new InvalidOperationException("The production packet or character scene did not instantiate.");
				}
				zombie.packet = towerDefensePacketConfig;
				zombie.gridPos = TestGrid;
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(GodotObject.IsInstanceValid(mapCell) && mapCell.isWater, "The focused runtime map must expose the reported cell as a real water cell.");
				zombie.cell = mapCell;
				zombie.inGame = true;
				zombie.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(8);
				if (!zombie.inWater)
				{
					zombie.inWater = true;
					await WaitFrames(2);
				}
				TanglekelpComponent tanglekelp = zombie.componentManager?.GetRuntime<TanglekelpComponent>();
				AdobeAnimateSpriteBase head = zombie.sprite?.GetNodeOrNull<AdobeAnimateSpriteBase>("%Head");
				Check(zombie.inWater && tanglekelp != null && !tanglekelp.IsReleased && tanglekelp.Alive && GodotObject.IsInstanceValid(head) && head.Visible, $"The reported zombie must begin submerged with its real tangle and head presentation active; inWater={zombie.inWater}, tangleAlive={tanglekelp?.Alive}, headValid={GodotObject.IsInstanceValid(head)}, headVisible={head?.Visible}.");
				if (!zombie.inWater || tanglekelp == null || tanglekelp.IsReleased || !tanglekelp.Alive || !GodotObject.IsInstanceValid(head))
				{
					throw new InvalidOperationException("The production submerged presentation did not activate.");
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				Vector2 logicalBefore = zombie.GetLogicalGlobalPosition();
				Vector2 rootCanvasBefore = zombie.GetGlobalTransformWithCanvas().Origin;
				Vector2 transformCanvasBefore = zombie.transformPoint.GetGlobalTransformWithCanvas().Origin;
				Vector2 spriteCanvasBefore = zombie.sprite.GetGlobalTransformWithCanvas().Origin;
				double num = zombie.instance.damagePoints[0]["Persontage"].AsDouble();
				double firstDamageThreshold = zombie.instance.hitpointsNearDeath + (zombie.instance.hitpointsSave - zombie.instance.hitpointsNearDeath) * num;
				double num2 = Math.Max(1.0, zombie.instance.hitpoints - firstDamageThreshold + 1.0);
				zombie.Hurt(num2, playSplatAudio: false, Vector2.Zero);
				await WaitFrames(4);
				Check(zombie.instance.damagePointIndex == 1 && zombie.instance.hitpoints < firstDamageThreshold, "The real damage call must cross exactly the first authored damage-point threshold.");
				Check(zombie.GetLogicalGlobalPosition().IsEqualApprox(logicalBefore), "Crossing the first damage point must not move the zombie's logical root.");
				Check(zombie.GetGlobalTransformWithCanvas().Origin.IsEqualApprox(rootCanvasBefore), "Crossing the first damage point must not move the zombie root in canvas space.");
				Check(zombie.transformPoint.GetGlobalTransformWithCanvas().Origin.IsEqualApprox(transformCanvasBefore), "Crossing the first damage point must not move the zombie TransformPoint in canvas space.");
				Check(zombie.sprite.GetGlobalTransformWithCanvas().Origin.IsEqualApprox(spriteCanvasBefore), "Crossing the first damage point must not move the zombie animation sprite in canvas space.");
				Check(tanglekelp != null && !tanglekelp.IsReleased && tanglekelp.Alive && head.Visible, "The half-health damage stage must retain the submerged tangle and head until the authored Head damage point.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewDoomTanglekelpZombieDamagePositionRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
			scene?.Dispose();
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"DOOM_TANGLEKELP_ZOMBIE_DAMAGE_POSITION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateWaterMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(100f, 76f)
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y),
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
			config = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
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
			GD.PushError("[BugOverviewDoomTanglekelpZombieDamagePositionRuntimeTest] " + message);
		}
	}
}
