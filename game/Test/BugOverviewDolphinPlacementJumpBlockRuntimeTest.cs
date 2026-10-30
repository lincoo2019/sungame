using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewDolphinPlacementJumpBlockRuntimeTest : Node
{
	private const string DolphinScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinrider.tscn";

	private const string DolphinPacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Packet/ZombieDolphinrider.tres";

	private const string PeashooterScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private const string SeaNutScenePath = "res://Asset/Anime/Character/Plant/Chapter3/SeaNut/Scene/TowerDefensePlantSeaNut.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousBackZombie = manager?.backZombie ?? false;
		DolphinPlacementJumpBlockRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
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
				control = new DolphinPlacementJumpBlockRuntimeControlStub
				{
					Name = "DolphinPlacementJumpBlockControl",
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
				manager.backZombie = false;
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateWaterMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				await VerifyPlacement(control, new Vector2I(7, 1));
				await VerifyOrdinaryJump(control, new Vector2I(5, 2));
				await VerifySeaNutBlock(control, new Vector2I(5, 3));
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewDolphinPlacementJumpBlockRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			AudioManager.Instance?.AudioStopAll();
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
				manager.backZombie = previousBackZombie;
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
			await WaitFrames(6);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"DOLPHIN_PLACEMENT_JUMP_BLOCK_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyPlacement(DolphinPlacementJumpBlockRuntimeControlStub control, Vector2I grid)
	{
		TowerDefenseZombieDolphinrider dolphin = CreateDolphin(grid);
		Check(GodotObject.IsInstanceValid(dolphin) && dolphin.packet?.saveKey == "ZombieDolphinrider", "The placement scenario must use the real Dolphin Rider scene and packet.");
		control.characterNode.AddChild(dolphin, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(6);
		dolphin.ProcessMode = ProcessModeEnum.Disabled;
		dolphin.sprite.SetAnimation("JumpInWater", loop: false);
		float expectedLogicalX = dolphin.GetLogicalGlobalPosition().X - dolphin.Scale.X * dolphin.transformPoint.Scale.X * 64f;
		dolphin.AnimeCompleted("JumpInWater");
		await WaitFrames(2);
		Check(Mathf.IsEqualApprox(dolphin.GetLogicalGlobalPosition().X, expectedLogicalX), $"The water-entry completion must retain its authored 64-pixel animation compensation; expected={expectedLogicalX}, after={dolphin.GetLogicalGlobalPosition()}.");
		Check(dolphin.gridPos == TowerDefenseManager.Instance.GetMapGridPos(dolphin.GetLogicalGlobalPosition()), "The compensated Dolphin position must refresh its logical grid.");
		Check(Mathf.IsEqualApprox(dolphin.sprite.offset.X, 24f), "The water-entry completion must retain the authored sprite offset paired with the logical compensation.");
		dolphin.QueueFree();
		await WaitFrames(3);
	}

	private async Task VerifyOrdinaryJump(DolphinPlacementJumpBlockRuntimeControlStub control, Vector2I grid)
	{
		TowerDefensePlant plant = CreatePlant<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn", grid);
		TowerDefenseZombieDolphinrider dolphin = CreateDolphin(grid);
		control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
		control.characterNode.AddChild(dolphin, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(6);
		dolphin.ProcessMode = ProcessModeEnum.Disabled;
		plant.ProcessMode = ProcessModeEnum.Disabled;
		AttackComponent attackComponent = dolphin.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
		Check(attackComponent != null && !attackComponent.IsReleased && plant.instance.height < TowerDefenseEnum.CHARACTER_HEIGHT.TALL, "The ordinary-jump scenario must use the real secondary jump AttackComponent and a short plant.");
		PrepareJump(dolphin, attackComponent, plant);
		Check(dolphin.CurrentStateHandle?.StableId == "zombie.dolphinrider.jump" && dolphin.isJump && dolphin.dolphin, "The real Dolphin Rider must enter its jump state against the short plant.");
		dolphin.JumpProcessing(1.0 / 60.0);
		Vector2 logicalBeforeCompletion = dolphin.GetLogicalGlobalPosition();
		Vector2 spriteBeforeCompletion = dolphin.sprite.GetGlobalTransformWithCanvas().Origin;
		dolphin.AnimeCompleted("DolphinJump");
		await WaitFrames(2);
		Vector2 logicalGlobalPosition = dolphin.GetLogicalGlobalPosition();
		Vector2 origin = dolphin.sprite.GetGlobalTransformWithCanvas().Origin;
		Check(logicalGlobalPosition.X <= logicalBeforeCompletion.X + 0.01f, $"A normal Dolphin jump must not teleport backward; before={logicalBeforeCompletion}, after={logicalGlobalPosition}.");
		Check(origin.X <= spriteBeforeCompletion.X + 1f, $"A normal Dolphin jump must not visibly teleport backward; before={spriteBeforeCompletion}, after={origin}.");
		Vector2 settledLogical = dolphin.GetLogicalGlobalPosition();
		await WaitFrames(4);
		Check(dolphin.GetLogicalGlobalPosition().IsEqualApprox(settledLogical), "The Dolphin Rider landing position must remain stable after subsequent frames.");
		dolphin.QueueFree();
		plant.QueueFree();
		await WaitFrames(3);
	}

	private async Task VerifySeaNutBlock(DolphinPlacementJumpBlockRuntimeControlStub control, Vector2I grid)
	{
		TowerDefensePlant seaNut = CreatePlant<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter3/SeaNut/Scene/TowerDefensePlantSeaNut.tscn", grid);
		TowerDefenseZombieDolphinrider dolphin = CreateDolphin(grid);
		control.characterNode.AddChild(seaNut, forceReadableName: false, InternalMode.Disabled);
		control.characterNode.AddChild(dolphin, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(6);
		dolphin.ProcessMode = ProcessModeEnum.Disabled;
		seaNut.ProcessMode = ProcessModeEnum.Disabled;
		AttackComponent attackComponent = dolphin.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
		Check(attackComponent != null && !attackComponent.IsReleased && seaNut.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL && seaNut.config?.name == "PlantSeaNut", "The block scenario must use the real tall SeaNut and secondary Dolphin jump attack.");
		PrepareJump(dolphin, attackComponent, seaNut);
		dolphin.JumpProcessing(1.0 / 60.0);
		Vector2 blockedPosition = dolphin.GetLogicalGlobalPosition();
		Check(!dolphin.dolphin && !dolphin.isJump && dolphin.CurrentStateHandle?.StableId == "zombie.walk", $"SeaNut must immediately block the real Dolphin jump; state={dolphin.CurrentStateHandle?.StableId}, dolphin={dolphin.dolphin}, block={dolphin.isBlock}, jump={dolphin.isJump}.");
		Check(Mathf.IsEqualApprox(blockedPosition.X, seaNut.GetLogicalGlobalPosition().X + 40f), $"The blocked Dolphin Rider must stop in front of SeaNut; blocked={blockedPosition}, seaNut={seaNut.GetLogicalGlobalPosition()}.");
		dolphin.AnimeCompleted("DolphinJump");
		await WaitFrames(2);
		Check(dolphin.GetLogicalGlobalPosition().IsEqualApprox(blockedPosition), $"A late DolphinJump completion must not move an already blocked Dolphin through SeaNut; blocked={blockedPosition}, after={dolphin.GetLogicalGlobalPosition()}.");
		Check(dolphin.gridPos.X >= seaNut.gridPos.X, "The blocked Dolphin Rider must remain on SeaNut's approach side after the late animation callback.");
		dolphin.QueueFree();
		seaNut.QueueFree();
		await WaitFrames(3);
	}

	private static TowerDefenseZombieDolphinrider CreateDolphin(Vector2I grid)
	{
		TowerDefenseZombieDolphinrider obj = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinrider.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieDolphinrider>(PackedScene.GenEditState.Disabled);
		obj.packet = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Packet/ZombieDolphinrider.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
		PrepareCharacter(obj, grid);
		return obj;
	}

	private static T CreatePlant<T>(string scenePath, Vector2I grid) where T : TowerDefensePlant
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		T obj = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		PrepareCharacter(obj, grid);
		return obj;
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I grid)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = grid;
		character.cell = TowerDefenseManager.GetMapCell(grid);
		character.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(grid));
	}

	private static void PrepareJump(TowerDefenseZombieDolphinrider dolphin, AttackComponent jumpAttack, TowerDefensePlant target)
	{
		jumpAttack.target = target;
		jumpAttack.alive = true;
		jumpAttack.timer = 0.0;
		jumpAttack.checkIntrevalNow = 0;
		dolphin.SendStateEvent("ToJump");
		jumpAttack.target = target;
		dolphin.AnimeEvent("check", default(Variant));
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
			GD.PushError("[BugOverviewDolphinPlacementJumpBlockRuntimeTest] " + message);
		}
	}
}
