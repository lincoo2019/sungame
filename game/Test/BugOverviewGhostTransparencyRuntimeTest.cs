using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGhostTransparencyRuntimeTest : Node
{
	private const string GhostScenePath = "res://Asset/Anime/Character/Zombie/Chapter8/Ghost/Scene/TowerDefenseZombieGhost.tscn";

	private const string PlanternScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Plantern/Scene/TowerDefensePlantern.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I GhostGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewGhostTransparencyRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseCharacter plantern = null;
		TowerDefenseCellInstance ghostCell = null;
		try
		{
			_ = 10;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_0114;
				}
				control = new BugOverviewGhostTransparencyRuntimeControlStub
				{
					Name = "GhostTransparencyRuntimeControl",
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				ghostCell = TowerDefenseManager.GetMapCell(GhostGrid);
				TowerDefenseZombieGhost ghost = Instantiate<TowerDefenseZombieGhost>("res://Asset/Anime/Character/Zombie/Chapter8/Ghost/Scene/TowerDefenseZombieGhost.tscn");
				plantern = Instantiate<TowerDefenseCharacter>("res://Asset/Anime/Character/Plant/Chapter0/Plantern/Scene/TowerDefensePlantern.tscn");
				Check(GodotObject.IsInstanceValid(ghost) && GodotObject.IsInstanceValid(plantern) && ghost.config?.name == "ZombieGhost" && plantern.config?.name == "PlantPlantern", "The scenario must instantiate the real ZombieGhost and Plantern scenes.");
				if (!GodotObject.IsInstanceValid(ghost) || !GodotObject.IsInstanceValid(plantern))
				{
					goto end_IL_0114;
				}
				PrepareCharacter(ghost, GhostGrid);
				PrepareCharacter(plantern, GhostGrid);
				ghost.ProcessMode = ProcessModeEnum.Disabled;
				plantern.ProcessMode = ProcessModeEnum.Disabled;
				control.characterNode.AddChild(ghost, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(plantern, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				BugOverviewGhostTransparencyRuntimeTest bugOverviewGhostTransparencyRuntimeTest = this;
				int condition;
				if (GodotObject.IsInstanceValid(ghost.sprite) && GodotObject.IsInstanceValid(ghost.instance))
				{
					TargetRegistrationComponent targetRegistrationComponent = ghost.targetRegistrationComponent;
					condition = ((targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewGhostTransparencyRuntimeTest.Check((byte)condition != 0, "The real Ghost must expose its authored sprite, instance, and target registration runtime.");
				Check((plantern.instance.physiqueTypeFlags & 0x40) != 0, "The real Plantern must retain the LIGHT physique used by Ghost visibility.");
				Check(GodotObject.IsInstanceValid(ghostCell) && !ghostCell.HasLight(), "The Ghost cell must begin dark before the Plantern is registered.");
				await StepGhost(ghost, 6);
				Check(ghost.ghost, "A real Ghost in a dark 3x3 neighborhood must remain logically hidden.");
				Check(AlphaApproximately(ghost.sprite.meshColor.A, 0.5f), $"A hidden Ghost must remain half-transparent across frames; alpha={ghost.sprite.meshColor.A}.");
				Check(!ghost.instance.canBeCollection && !ghost.targetRegistrationComponent.canProjectileCheck, "A hidden Ghost must retain its authored untargetable gameplay state.");
				Dictionary data = new Dictionary
				{
					["ghost"] = true,
					["carrierSyncId"] = -1,
					["ghostTimeScaleSave"] = 1.0
				};
				for (int i = 0; i < 5; i++)
				{
					ghost.ImportNetworkSpecialState(data);
				}
				Check(AlphaApproximately(ghost.sprite.meshColor.A, 0.5f), $"Repeated hidden-state imports must not compound alpha; alpha={ghost.sprite.meshColor.A}.");
				AddToCell(plantern, ghostCell);
				Check(ghostCell.HasLight(), "Registering the real Plantern must light the Ghost cell.");
				await StepGhost(ghost, 4);
				Check(!ghost.ghost, "A real Ghost inside the Plantern's light must become logically visible.");
				Check(AlphaApproximately(ghost.sprite.meshColor.A, 1f), $"A Ghost in a lit area must restore opaque alpha; alpha={ghost.sprite.meshColor.A}.");
				Check(ghost.instance.canBeCollection && ghost.targetRegistrationComponent.canProjectileCheck, "A visible Ghost must restore its authored targetable gameplay state.");
				ghostCell.characterList.Remove(plantern);
				Check(!ghostCell.HasLight(), "Removing the real Plantern must make the Ghost neighborhood dark again.");
				await StepGhost(ghost, 6);
				Check(ghost.ghost, "A Ghost leaving the lit state must return to logical hiding.");
				Check(AlphaApproximately(ghost.sprite.meshColor.A, 0.5f), $"A Ghost must return to stable half-transparent alpha across later frames; alpha={ghost.sprite.meshColor.A}.");
				TowerDefenseZombie carrier = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				TowerDefenseZombieGhost hostileGhost = Instantiate<TowerDefenseZombieGhost>("res://Asset/Anime/Character/Zombie/Chapter8/Ghost/Scene/TowerDefenseZombieGhost.tscn");
				Check(GodotObject.IsInstanceValid(carrier) && GodotObject.IsInstanceValid(hostileGhost) && carrier.config?.name == "ZombieNormal" && hostileGhost.config?.name == "ZombieGhost", "The overlap scenario must instantiate the real Normal Zombie and a second real Ghost.");
				if (!GodotObject.IsInstanceValid(carrier) || !GodotObject.IsInstanceValid(hostileGhost))
				{
					goto end_IL_0114;
				}
				PrepareCharacter(carrier, GhostGrid);
				PrepareCharacter(hostileGhost, GhostGrid);
				carrier.camp = ghost.camp;
				hostileGhost.camp = ((ghost.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
				carrier.ProcessMode = ProcessModeEnum.Disabled;
				hostileGhost.ProcessMode = ProcessModeEnum.Disabled;
				control.characterNode.AddChild(carrier, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(hostileGhost, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				await StepGhost(ghost, 2);
				Check(ghost.carryCharacter == carrier && carrier.hasGhost && carrier.ghostCharacter == ghost, "A real Ghost must attach to an overlapping eligible same-camp zombie.");
				Check(ghost.GetLogicalGlobalPosition().IsEqualApprox(carrier.GetLogicalGlobalPosition() + new Vector2(5f, 0f)), "A carried Ghost must publish its server-authoritative position beside its carrier.");
				Check(ghost.attackComponent.target == hostileGhost, "A real Ghost must acquire an overlapping hostile Ghost with the same config name.");
				TowerDefenseEnum.CHARACTER_CAMP hostileCamp = hostileGhost.camp;
				hostileGhost.camp = ghost.camp;
				await StepGhost(ghost, 1);
				Check(ghost.attackComponent.target == null, "Changing a registered hostile Ghost to the owner camp must invalidate the classified target cache.");
				hostileGhost.camp = hostileCamp;
				await StepGhost(ghost, 1);
				Check(ghost.attackComponent.target == hostileGhost, "Restoring the hostile camp must rebuild the classified target cache and reacquire the Ghost.");
				TowerDefenseCharacterConfig hostileConfig = hostileGhost.config;
				hostileGhost.config = carrier.config;
				await StepGhost(ghost, 1);
				Check(ghost.attackComponent.target == null, "Changing a registered target's config name must invalidate the classified target cache.");
				hostileGhost.config = hostileConfig;
				await StepGhost(ghost, 1);
				Check(ghost.attackComponent.target == hostileGhost, "Restoring the hostile Ghost config must rebuild the classified target cache and reacquire it.");
				hostileGhost.SetLogicalGlobalPosition(hostileGhost.GetLogicalGlobalPosition() + new Vector2(2000f, 0f));
				hostileGhost.gridPos = new Vector2I(9, GhostGrid.Y);
				await StepGhost(ghost, 2);
				Check(ghost.attackComponent.target == null, "A real Ghost must clear its hostile-Ghost target after the target leaves the hit box.");
				carrier.targetRegistrationComponent.canCarry = false;
				ghost.DetachGhost();
				Check(!GodotObject.IsInstanceValid(ghost.carryCharacter) && !carrier.hasGhost && !GodotObject.IsInstanceValid(carrier.ghostCharacter), "Detaching a real Ghost must restore both sides of the carrier relation.");
				goto end_IL_00dc;
				end_IL_0114:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGhostTransparencyRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00dc;
			}
			return;
			end_IL_00dc:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(ghostCell) && GodotObject.IsInstanceValid(plantern))
			{
				ghostCell.characterList.Remove(plantern);
			}
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
		}
		bool flag = _failures == 0 && _checks == 27;
		GD.Print($"GHOST_TRANSPARENCY_RESULT version=3 passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I grid)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = grid;
		character.cell = TowerDefenseManager.GetMapCell(grid);
		character.Position = instance.gridBeginPos + new Vector2(((float)grid.X - 0.5f) * instance.gridSize.X, ((float)grid.Y - 0.5f) * instance.gridSize.Y);
	}

	private static void AddToCell(TowerDefenseCharacter character, TowerDefenseCellInstance cell)
	{
		if (GodotObject.IsInstanceValid(cell) && !cell.characterList.Contains(character))
		{
			cell.characterList.Add(character);
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
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j))?.Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private async Task StepGhost(TowerDefenseZombieGhost ghost, int frameCount)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			ghost.BatchUpdate(1.0 / 60.0);
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

	private static bool AlphaApproximately(float actual, float expected)
	{
		return Mathf.Abs(actual - expected) <= 0.001f;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewGhostTransparencyRuntimeTest] " + message);
		}
	}
}
