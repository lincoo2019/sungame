using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGameplayRaceBatchRuntimeTest : Node
{
	private const string GloompultGargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Gloompult/TowerDefenseZombieGargantuarGloompult.tscn";

	private const string PeashooterScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private const string ShootingStarsScenePath = "res://Asset/Anime/Character/Plant/Star/ShootingStars/Scene/TowerDefensePlantShootingStars.tscn";

	private const string GargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn";

	private const string RewindClockScenePath = "res://Asset/Anime/Character/Plant/Star/AppleGreen/Scene/TowerDefensePlantAppleGreen.tscn";

	private static readonly Vector2I PlantGrid = new Vector2I(5, 3);

	private static readonly Vector2I ZombieGrid = new Vector2I(6, 3);

	private GameplayRaceBatchRuntimeControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private int _groomSpawnCount;

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
		bool previousBackPacket = manager?.backPacket ?? false;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00fd;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("BugOverviewGameplayRaceBatchRuntimeTest");
				CreateBattleFixture(manager);
				await VerifyFluorescentPlantDoesNotReceiveCommittedGloompultAttack();
				await ClearCharacterNode();
				await VerifyShovelingShootingStarsStopsRain();
				await ClearCharacterNode();
				await VerifyRewindDoesNotThrowSecondImp();
				await ClearCharacterNode();
				await VerifyRapidRewindDoesNotInjectGloompultAttacks();
				goto end_IL_00d2;
				end_IL_00fd:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGameplayRaceBatchRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00d2;
			}
			return;
			end_IL_00d2:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.backZombie = previousBackZombie;
				manager.backPacket = previousBackPacket;
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
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 29;
		GD.Print($"GAMEPLAY_RACE_BATCH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void CreateBattleFixture(TowerDefenseManager manager)
	{
		_control = new GameplayRaceBatchRuntimeControlStub
		{
			Name = "GameplayRaceBatchRuntimeControl",
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode.ChildEnteredTree += OnCharacterNodeChildEntered;
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

	private async Task VerifyFluorescentPlantDoesNotReceiveCommittedGloompultAttack()
	{
		TowerDefensePlantPeaShooterSingle plant = Instantiate<TowerDefensePlantPeaShooterSingle>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn");
		TowerDefenseZombieGargantuarGloompult gargantuar = Instantiate<TowerDefenseZombieGargantuarGloompult>("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Gloompult/TowerDefenseZombieGargantuarGloompult.tscn");
		Check(GodotObject.IsInstanceValid(plant) && plant.config?.name == "PlantPeaShooterSingle", "The fluorescence fixture must instantiate the production Peashooter.");
		Check(GodotObject.IsInstanceValid(gargantuar) && gargantuar.config?.name == "ZombieGargantuarGloompult", "The fluorescence fixture must instantiate the production Gloompult Gargantuar.");
		if (GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(gargantuar))
		{
			PlaceCharacter(plant, PlantGrid);
			PlaceCharacter(gargantuar, ZombieGrid);
			plant.SetLogicalGlobalPosition(gargantuar.GetLogicalGlobalPosition() - new Vector2(30f, 0f));
			_control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			_control.characterNode.AddChild(gargantuar, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(5);
			plant.ProcessMode = ProcessModeEnum.Disabled;
			gargantuar.ProcessMode = ProcessModeEnum.Disabled;
			AttackComponent attack = gargantuar.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
			Check(attack != null && !attack.IsReleased && attack.checkAll, "The fixture must use the production all-target Gloompult AttackComponent.");
			if (attack != null && !attack.IsReleased)
			{
				attack.target = plant;
				Check(attack.target == plant && plant.instance.canBeCollection, "The production Gloompult component must hold the ordinary plant as its committed target before fluorescence is applied.");
				plant.BuffAdd(new TowerDefenseCharacterBuffFluorescence
				{
					time = 50.0
				});
				await WaitFrames(1);
				Check(!plant.instance.canBeCollection, "The real Fluorescence buff must remove the plant from attack collection before the committed animation event.");
				int before = _groomSpawnCount;
				attack.AnimeEvent(attack.attackEventName, default(Variant));
				await WaitFrames(1);
				int groomSpawnCount = _groomSpawnCount;
				Check(groomSpawnCount == before, $"A committed Gloompult animation must not attack a fluorescent plant; before={before}, after={groomSpawnCount}.");
			}
		}
	}

	private async Task VerifyShovelingShootingStarsStopsRain()
	{
		TowerDefensePlantShootingStars shootingStars = Instantiate<TowerDefensePlantShootingStars>("res://Asset/Anime/Character/Plant/Star/ShootingStars/Scene/TowerDefensePlantShootingStars.tscn");
		Check(GodotObject.IsInstanceValid(shootingStars) && shootingStars.config?.name == "PlantShootingStars", "The shovel fixture must instantiate the production Shooting Stars plant.");
		if (GodotObject.IsInstanceValid(shootingStars))
		{
			PlaceCharacter(shootingStars, PlantGrid);
			_control.characterNode.AddChild(shootingStars, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			shootingStars.IdleEntered();
			await WaitFrames(2);
			Check(CountChildren<TowerDefenseProjectileEffectShootingStars>() == 1, "The production Shooting Stars plant must start one real rain effect before shoveling.");
			shootingStars.ShovelDestroy();
			await WaitSeconds(0.4);
			Check(shootingStars.isDestroy, "The production shovel destroy chain must retire the Shooting Stars plant.");
			Check(CountChildren<TowerDefenseProjectileEffectShootingStars>() == 0, "Shoveling Shooting Stars must interrupt and remove its active rain effect immediately.");
		}
	}

	private async Task VerifyRewindDoesNotThrowSecondImp()
	{
		TowerDefenseZombieGargantuar gargantuar = Instantiate<TowerDefenseZombieGargantuar>("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn");
		Check(GodotObject.IsInstanceValid(gargantuar) && gargantuar.config?.name == "ZombieGargantuar", "The imp fixture must instantiate the production Gargantuar.");
		if (GodotObject.IsInstanceValid(gargantuar))
		{
			gargantuar.inGame = true;
			gargantuar.gridPos = ZombieGrid;
			gargantuar.SetLogicalGlobalPosition(new Vector2(900f, TowerDefenseManager.GetMapCellPlantPos(ZombieGrid).Y));
			_control.characterNode.AddChild(gargantuar, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(5);
			gargantuar.ProcessMode = ProcessModeEnum.Disabled;
			gargantuar.DamagePointReach(gargantuar.impThrowDamagePointName);
			BugOverviewGameplayRaceBatchRuntimeTest bugOverviewGameplayRaceBatchRuntimeTest = this;
			int condition;
			if (gargantuar.impThrowFlag)
			{
				ImpThrowerComponent impThrowerComponent = gargantuar.impThrowerComponent;
				condition = ((impThrowerComponent != null && !impThrowerComponent.IsReleased) ? 1 : 0);
			}
			else
			{
				condition = 0;
			}
			bugOverviewGameplayRaceBatchRuntimeTest.Check((byte)condition != 0, "The real ThrowImp damage point must arm the production ImpThrowerComponent.");
			gargantuar.AnimeEvent(gargantuar.impFireEvent, default(Variant));
			await WaitFrames(3);
			Check(CountChildren<TowerDefenseZombieImpBase>() == 1, "The first production fire event must throw exactly one real Imp.");
			TowerDefensePlantAppleGreen clock = await StartRewindClock();
			gargantuar.sprite.playBack = true;
			gargantuar.AnimeEvent(gargantuar.impFireEvent, default(Variant));
			await WaitFrames(3);
			Check(CountChildren<TowerDefenseZombieImpBase>() == 1, "Crossing the same Gargantuar fire event during Rewind Clock playback must not throw a second Imp.");
			await StopRewindClock(clock);
		}
	}

	private async Task VerifyRapidRewindDoesNotInjectGloompultAttacks()
	{
		TowerDefensePlantPeaShooterSingle plant = Instantiate<TowerDefensePlantPeaShooterSingle>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn");
		TowerDefenseZombieGargantuarGloompult gargantuar = Instantiate<TowerDefenseZombieGargantuarGloompult>("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Gloompult/TowerDefenseZombieGargantuarGloompult.tscn");
		Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(gargantuar), "The rapid-rewind fixture must instantiate both production combatants.");
		if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(gargantuar))
		{
			return;
		}
		PlaceCharacter(plant, PlantGrid);
		PlaceCharacter(gargantuar, ZombieGrid);
		plant.SetLogicalGlobalPosition(gargantuar.GetLogicalGlobalPosition() - new Vector2(30f, 0f));
		_control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode.AddChild(gargantuar, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(5);
		plant.ProcessMode = ProcessModeEnum.Disabled;
		gargantuar.ProcessMode = ProcessModeEnum.Disabled;
		AttackComponent attack = gargantuar.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
		Check(attack != null && !attack.IsReleased, "The rapid-rewind fixture must use the production Gloompult AttackComponent.");
		if (attack != null && !attack.IsReleased)
		{
			attack.target = plant;
			Check(attack.target == plant && plant.instance.canBeCollection, "The rapid-rewind component must hold a valid ordinary plant as its committed target.");
			int beforeForward = _groomSpawnCount;
			attack.AnimeEvent(attack.attackEventName, default(Variant));
			await WaitFrames(1);
			int baseline = _groomSpawnCount;
			Check(baseline == beforeForward + 1, "One forward animation event must create one production Gloompult effect before rewind cycling.");
			for (int cycle = 0; cycle < 3; cycle++)
			{
				TowerDefensePlantAppleGreen clock = await StartRewindClock();
				gargantuar.sprite.playBack = true;
				attack.AnimeEvent(attack.attackEventName, default(Variant));
				await WaitFrames(1);
				await StopRewindClock(clock);
				gargantuar.sprite.playBack = false;
			}
			int groomSpawnCount = _groomSpawnCount;
			Check(groomSpawnCount == baseline, $"Rapid Rewind Clock placement and shoveling must not inject reverse Gloompult attacks; baseline={baseline}, final={groomSpawnCount}.");
		}
	}

	private async Task<TowerDefensePlantAppleGreen> StartRewindClock()
	{
		TowerDefensePlantAppleGreen clock = Instantiate<TowerDefensePlantAppleGreen>("res://Asset/Anime/Character/Plant/Star/AppleGreen/Scene/TowerDefensePlantAppleGreen.tscn");
		clock.inGame = false;
		clock.gridPos = new Vector2I(2, 2);
		clock.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(clock.gridPos));
		_control.characterNode.AddChild(clock, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(2);
		clock.RunApple();
		await WaitFrames(1);
		Check(TowerDefenseManager.Instance.backZombie && TowerDefenseManager.Instance.backPacket, "The production Rewind Clock must activate both global rewind flags.");
		return clock;
	}

	private async Task StopRewindClock(TowerDefensePlantAppleGreen clock)
	{
		clock.ShovelDestroy();
		await WaitFrames(3);
		Check(!TowerDefenseManager.Instance.backZombie && !TowerDefenseManager.Instance.backPacket, "Shoveling the only Rewind Clock must release both global rewind flags.");
	}

	private static void PlaceCharacter(TowerDefenseCharacter character, Vector2I gridPos)
	{
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = gridPos;
		character.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(gridPos));
	}

	private async Task ClearCharacterNode()
	{
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return;
		}
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
			{
				child.QueueFree();
			}
		}
		await WaitFrames(3);
	}

	private int CountChildren<T>() where T : Node
	{
		int num = 0;
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is T && GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
			{
				num++;
			}
		}
		return num;
	}

	private void OnCharacterNodeChildEntered(Node child)
	{
		if (child is TowerDefenseProjectileEffectGroom)
		{
			_groomSpawnCount++;
		}
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
		towerDefenseBattleFeatureMap.PlantGridInit();
		towerDefenseBattleFeatureMap.rect = new Rect2(-200f, -200f, 1600f, 1000f);
		towerDefenseBattleFeatureMap.groundRect = new Rect2(0f, 0f, (float)gridNum.X * towerDefenseBattleFeatureMap.config.gridSize.X, (float)gridNum.Y * towerDefenseBattleFeatureMap.config.gridSize.Y);
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private static T Instantiate<T>(string scenePath) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.IgnoreDeep);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);
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
			GD.PushError("[BugOverviewGameplayRaceBatchRuntimeTest] " + message);
		}
	}
}
