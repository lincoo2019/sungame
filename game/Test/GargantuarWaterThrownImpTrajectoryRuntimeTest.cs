using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class GargantuarWaterThrownImpTrajectoryRuntimeTest : Node
{
	private const string ImpScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Base/TowerDefenseZombieImp.tscn";

	private const string ResultMarker = "GARGANTUAR_WATER_THROWN_IMP_TRAJECTORY_RESULT";

	private static readonly Vector2I WaterGrid = new Vector2I(5, 2);

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	private Node2D controlCharacterNode => (TowerDefenseManager.Instance.currentControl as GargantuarWaterThrownImpTrajectoryControlStub)?.characterNode;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		GargantuarWaterThrownImpTrajectoryControlStub control = null;
		PackedScene scene = null;
		TowerDefenseZombieImpBase firstImp = null;
		TowerDefenseZombieImpBase secondImp = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Require(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload is unavailable.");
				control = new GargantuarWaterThrownImpTrajectoryControlStub
				{
					Name = "GargantuarWaterThrownImpTrajectoryControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				TowerDefenseZombie.UseBatch = false;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.CpuPose;
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				towerDefenseCellConfig.gridType.Add(TowerDefenseEnum.PLANTGRIDTYPE.WATER);
				TowerDefenseCellInstance waterCell = new TowerDefenseCellInstance
				{
					gridPos = WaterGrid
				};
				waterCell.Init(towerDefenseCellConfig);
				Check(waterCell.isWater, "The fixture must use a real WATER cell.");
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Base/TowerDefenseZombieImp.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(scene), "The real base Imp scene must load.");
				Require(GodotObject.IsInstanceValid(scene), "The real base Imp scene failed to load.");
				firstImp = SpawnThrownImp(scene, waterCell, 500f, 250f);
				CheckSpawnStart(firstImp, "first");
				await WaitFrames(4);
				CheckMidFlight(firstImp, 500f, 250f, "first");
				firstImp.QueueFree();
				firstImp = null;
				await WaitFrames(3);
				secondImp = SpawnReadyPreparedThrownImp(scene, waterCell, 520f, 270f);
				CheckSpawnStart(secondImp, "second");
				await WaitFrames(4);
				CheckMidFlight(secondImp, 520f, 270f, "second");
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
			}
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(firstImp) && !firstImp.IsQueuedForDeletion())
			{
				firstImp.QueueFree();
			}
			if (GodotObject.IsInstanceValid(secondImp) && !secondImp.IsQueuedForDeletion())
			{
				secondImp.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			await WaitFrames(4);
			scene?.Dispose();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
		}
		Finish();
	}

	private TowerDefenseZombieImpBase SpawnThrownImp(PackedScene scene, TowerDefenseCellInstance waterCell, float startX, float landX)
	{
		TowerDefenseZombieImpBase towerDefenseZombieImpBase = scene.Instantiate<TowerDefenseZombieImpBase>(PackedScene.GenEditState.Disabled);
		Require(GodotObject.IsInstanceValid(towerDefenseZombieImpBase), "The real base Imp scene failed to instantiate.");
		towerDefenseZombieImpBase.inGame = true;
		towerDefenseZombieImpBase.editorPreviewMode = false;
		towerDefenseZombieImpBase.gridPos = WaterGrid;
		towerDefenseZombieImpBase.cell = waterCell;
		towerDefenseZombieImpBase.GlobalPosition = new Vector2(startX, 180f);
		towerDefenseZombieImpBase.z = 90.0;
		towerDefenseZombieImpBase.ImportNetworkSpawnState(new Dictionary
		{
			["throw"] = true,
			["y_speed"] = -60.0,
			["land_pos_x"] = landX,
			["fall_duration"] = 1.0,
			["throw_ease"] = 1,
			["throw_transition"] = 4
		});
		controlCharacterNode.AddChild(towerDefenseZombieImpBase, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseZombieImpBase;
	}

	private TowerDefenseZombieImpBase SpawnReadyPreparedThrownImp(PackedScene scene, TowerDefenseCellInstance waterCell, float startX, float landX)
	{
		TowerDefenseZombieImpBase towerDefenseZombieImpBase = scene.Instantiate<TowerDefenseZombieImpBase>(PackedScene.GenEditState.Disabled);
		Require(GodotObject.IsInstanceValid(towerDefenseZombieImpBase), "The ready-prepared base Imp scene failed to instantiate.");
		towerDefenseZombieImpBase.inGame = false;
		towerDefenseZombieImpBase.editorPreviewMode = false;
		towerDefenseZombieImpBase.Visible = false;
		towerDefenseZombieImpBase.ProcessMode = ProcessModeEnum.Disabled;
		controlCharacterNode.AddChild(towerDefenseZombieImpBase, forceReadableName: false, InternalMode.Disabled);
		Require(towerDefenseZombieImpBase.IsNodeReady(), "The ready-prepared Imp must finish Ready before throw state import.");
		towerDefenseZombieImpBase.gridPos = WaterGrid;
		towerDefenseZombieImpBase.cell = waterCell;
		towerDefenseZombieImpBase.GlobalPosition = new Vector2(startX, 180f);
		towerDefenseZombieImpBase.z = 90.0;
		towerDefenseZombieImpBase.ImportNetworkSpawnState(new Dictionary
		{
			["throw"] = true,
			["y_speed"] = -60.0,
			["land_pos_x"] = landX,
			["fall_duration"] = 1.0,
			["throw_ease"] = 1,
			["throw_transition"] = 4
		});
		towerDefenseZombieImpBase.inGame = true;
		towerDefenseZombieImpBase.Visible = true;
		towerDefenseZombieImpBase.ProcessMode = ProcessModeEnum.Inherit;
		towerDefenseZombieImpBase.ActivateGameplayProcessing();
		towerDefenseZombieImpBase.groundHeightComponent?.DetectEnvironment();
		return towerDefenseZombieImpBase;
	}

	private void CheckSpawnStart(TowerDefenseZombieImpBase imp, string label)
	{
		Check(GodotObject.IsInstanceValid(imp) && imp.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Base/TowerDefenseZombieImp.tscn" && imp.config?.name == "ZombieImp", "The " + label + " spawn must use the real base Imp scene/config.");
		Check(imp._throw && !imp.landOver && !imp.isGround, "The " + label + " thrown Imp must enter the tree in airborne state.");
		Check(imp.inWater && Mathf.IsEqualApprox((float)imp.groundHeight, (float)(0.0 - imp.waterHeight)), "The " + label + " thrown Imp must retain the real water-lane landing surface.");
		Check(imp.z > imp.groundHeight + 80.0, $"The {label} thrown Imp must retain its hand-height z on water entry; z={imp.z}, ground={imp.groundHeight}.");
	}

	private void CheckMidFlight(TowerDefenseZombieImpBase imp, float startX, float landX, string label)
	{
		string text = imp.impFlightComponent?.StateMachine?.CurrentStateHandle?.StableId;
		Check(text == "imp_flight.fly", $"The {label} thrown Imp must still own the Fly state mid-trajectory; main={imp.CurrentStateHandle?.StableId}, flight={text}.");
		Check(imp.z > imp.groundHeight + 70.0, $"The {label} thrown Imp must remain visibly above the water mid-trajectory; z={imp.z}, ground={imp.groundHeight}, ySpeed={imp.ySpeed}, isGround={imp.isGround}, inWater={imp.inWater}.");
		float num = Mathf.Min(startX, landX);
		float num2 = Mathf.Max(startX, landX);
		Check(imp.GlobalPosition.X > num + 1f && imp.GlobalPosition.X < num2 - 1f, $"The {label} thrown Imp must traverse between hand and landing positions; x={imp.GlobalPosition.X}, range=({num}, {num2}).");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
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
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[GargantuarWaterThrownImpTrajectoryRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 17;
		GD.Print($"{"GARGANTUAR_WATER_THROWN_IMP_TRAJECTORY_RESULT"} passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
