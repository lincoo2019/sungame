using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewSquashVaseZamboniSmashRuntimeTest : Node
{
	private const string SquashVaseScenePath = "res://Asset/Anime/Character/Plant/Star/SquashVase/Scene/TowerDefensePlantSquashVase.tscn";

	private const string ZamboniGargantuarScenePath = "res://Asset/Anime/Character/Zombie/Challenge/GargantuarZamboni/Scene/Base/TowerDefenseZombieGargantuarZamboni.tscn";

	private static readonly Vector2I TargetGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		SquashVaseZamboniSmashControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00e0;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("BugOverviewSquashVaseZamboniSmashRuntimeTest");
				control = new SquashVaseZamboniSmashControlStub
				{
					Name = "SquashVaseZamboniSmashControl",
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
				TowerDefensePlantSquashVase squashVase = Instantiate<TowerDefensePlantSquashVase>("res://Asset/Anime/Character/Plant/Star/SquashVase/Scene/TowerDefensePlantSquashVase.tscn");
				TowerDefenseZombieGargantuarZamboni zamboni = Instantiate<TowerDefenseZombieGargantuarZamboni>("res://Asset/Anime/Character/Zombie/Challenge/GargantuarZamboni/Scene/Base/TowerDefenseZombieGargantuarZamboni.tscn");
				Check(GodotObject.IsInstanceValid(squashVase) && squashVase.config?.name == "PlantSquashVase", "The production Squash Vase scene must instantiate.");
				Check(GodotObject.IsInstanceValid(zamboni) && zamboni.config?.name == "ZombieGargantuarZamboni", "The production Zamboni Gargantuar scene must instantiate.");
				if (!GodotObject.IsInstanceValid(squashVase) || !GodotObject.IsInstanceValid(zamboni))
				{
					goto end_IL_00e0;
				}
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(TargetGrid);
				squashVase.inGame = true;
				squashVase.gridPos = TargetGrid;
				squashVase.SetLogicalGlobalPosition(mapCellPlantPos);
				zamboni.inGame = true;
				zamboni.ProcessMode = ProcessModeEnum.Disabled;
				zamboni.gridPos = TargetGrid;
				zamboni.SetLogicalGlobalPosition(mapCellPlantPos);
				control.characterNode.AddChild(squashVase, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zamboni, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				SquashComponent squashComponent = squashVase.componentManager?.GetRuntime<SquashComponent>();
				AttackComponent zamboniAttack = zamboni.attackComponent;
				Check(squashComponent != null && !squashComponent.IsReleased && squashComponent.checkAliveCharacter, "The production Squash Vase must expose its delayed alive-target capture component.");
				Check(zamboniAttack != null && !zamboniAttack.IsReleased && zamboniAttack.attackType == "Smash" && zamboniAttack.checkVase, "The production Zamboni Gargantuar must expose its vase-aware Smash component.");
				if (squashComponent == null || squashComponent.IsReleased || zamboniAttack == null || zamboniAttack.IsReleased)
				{
					goto end_IL_00e0;
				}
				squashComponent.Execute(zamboni);
				squashComponent.JumpDownSmashAction();
				await WaitFrames(5);
				TowerDefenseVaseSquashBlack blackVase = FindBlackVase(control.characterNode);
				Check(GodotObject.IsInstanceValid(blackVase), "The real Squash impact must create the production black capture vase.");
				Check(GodotObject.IsInstanceValid(blackVase) && blackVase.CharacterList.Contains(zamboni) && !zamboni.Visible && zamboni.ProcessMode == ProcessModeEnum.Disabled, "The black vase must actually hold and disable the reported Zamboni Gargantuar.");
				if (!GodotObject.IsInstanceValid(blackVase))
				{
					goto end_IL_00e0;
				}
				zamboniAttack.target = blackVase;
				zamboniAttack.SmashAttackCell(((TowerDefenseZombieConfig)zamboni.config).smashAttack);
				await WaitFrames(3);
				Check(GodotObject.IsInstanceValid(blackVase) && !blackVase.isDestroy, "A black Squash capture vase must not be crushed by the Zamboni Gargantuar smash already running in the impact frame.");
				Check(!zamboni.Visible && zamboni.ProcessMode == ProcessModeEnum.Disabled, "The captured Zamboni Gargantuar must remain sealed after the smash attempt.");
				goto end_IL_00c5;
				end_IL_00e0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSquashVaseZamboniSmashRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c5;
			}
			return;
			end_IL_00c5:;
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
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"SQUASH_VASE_ZAMBONI_SMASH_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefenseVaseSquashBlack FindBlackVase(Node2D characterNode)
	{
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is TowerDefenseVaseSquashBlack towerDefenseVaseSquashBlack && GodotObject.IsInstanceValid(towerDefenseVaseSquashBlack))
			{
				return towerDefenseVaseSquashBlack;
			}
		}
		return null;
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
			GD.PushError("[BugOverviewSquashVaseZamboniSmashRuntimeTest] " + message);
		}
	}
}
