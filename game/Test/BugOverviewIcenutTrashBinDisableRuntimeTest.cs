using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewIcenutTrashBinDisableRuntimeTest : Node
{
	private const string IcenutScenePath = "res://Asset/Anime/Character/Plant/Chapter2/Icenut/Scene/TowerDefensePlantIcenut.tscn";

	private const string TrashBinScenePath = "res://Asset/Anime/Character/GraveStone/TrashBin/Scene/TowerDefenseTrashBin.tscn";

	private static readonly Vector2 GridBegin = new Vector2(100f, 100f);

	private static readonly Vector2 GridSize = new Vector2(100f, 76f);

	private static readonly Vector2I GridNum = new Vector2I(9, 5);

	private static readonly Vector2I TestGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BugOverviewIcenutTrashBinDisableControlStub control = null;
		TowerDefensePlantIcenut icenut = null;
		TowerDefenseTrashBin trashBin = null;
		bool icenutRegistered = false;
		bool trashBinRegistered = false;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				throw new InvalidOperationException("TowerDefenseManager is unavailable.");
			}
			control = new BugOverviewIcenutTrashBinDisableControlStub
			{
				Name = "IcenutTrashBinDisableControl",
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
			manager.gridBeginPos = GridBegin;
			manager.gridSize = GridSize;
			manager.gridNum = GridNum;
			icenut = Instantiate<TowerDefensePlantIcenut>("res://Asset/Anime/Character/Plant/Chapter2/Icenut/Scene/TowerDefensePlantIcenut.tscn");
			trashBin = Instantiate<TowerDefenseTrashBin>("res://Asset/Anime/Character/GraveStone/TrashBin/Scene/TowerDefenseTrashBin.tscn");
			Check(GodotObject.IsInstanceValid(icenut), "The real Icenut scene must instantiate.");
			Check(GodotObject.IsInstanceValid(trashBin), "The real TrashBin scene must instantiate.");
			if (!GodotObject.IsInstanceValid(icenut) || !GodotObject.IsInstanceValid(trashBin))
			{
				throw new InvalidOperationException("A real character fixture failed to instantiate.");
			}
			PlaceCharacter(icenut);
			PlaceCharacter(trashBin);
			TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
			{
				gridPos = TestGrid
			};
			towerDefenseCellInstance.characterList.Add(icenut);
			towerDefenseCellInstance.characterList.Add(trashBin);
			icenut.cell = towerDefenseCellInstance;
			trashBin.cell = towerDefenseCellInstance;
			control.characterNode.AddChild(icenut, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(trashBin, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(3);
			manager.CharacterRegister(icenut);
			icenutRegistered = true;
			manager.CharacterRegister(trashBin);
			trashBinRegistered = true;
			VerifyDisableLifecycle(icenut, trashBin);
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewIcenutTrashBinDisableRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				if (trashBinRegistered && GodotObject.IsInstanceValid(trashBin))
				{
					manager.CharacterUnregister(trashBin);
				}
				if (icenutRegistered && GodotObject.IsInstanceValid(icenut))
				{
					manager.CharacterUnregister(icenut);
				}
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(trashBin))
			{
				trashBin.QueueFree();
			}
			if (GodotObject.IsInstanceValid(icenut))
			{
				icenut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_ICENUT_TRASHBIN_DISABLE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyDisableLifecycle(TowerDefensePlantIcenut icenut, TowerDefenseTrashBin trashBin)
	{
		PeriodicAreaEventComponent periodicAreaEventComponent = icenut.componentManager?.GetRuntime<PeriodicAreaEventComponent>("character.periodic_area_event");
		Check(periodicAreaEventComponent != null && !periodicAreaEventComponent.IsReleased, "The real Icenut PeriodicAreaEventComponent must be active.");
		if (periodicAreaEventComponent == null || periodicAreaEventComponent.IsReleased)
		{
			throw new InvalidOperationException("Icenut periodic component is unavailable.");
		}
		Check(icenut.componentAlive, "Icenut components must begin enabled.");
		Check(trashBin.camp == TowerDefenseEnum.CHARACTER_CAMP.ALL && icenut.CanTarget(trashBin) && icenut.CanCollision(trashBin.instance.maskFlags), "The real TrashBin must be an eligible target for Icenut's contact damage.");
		double hitpoints = trashBin.instance.hitpoints;
		periodicAreaEventComponent.EventExecute();
		double num = hitpoints - trashBin.instance.hitpoints;
		Check(Math.Abs(num - 20.0) < 0.001, $"Enabled Icenut contact damage must deal 20 damage to the real TrashBin; dealt {num}.");
		trashBin.BatchUpdate(0.0);
		Check(icenut.characterDisabled && !icenut.componentAlive && icenut.invisible && !icenut.instance.canBeCollection && icenut.skipDestroySet, "The real TrashBin cover pass must disable and hide Icenut.");
		double hitpoints2 = trashBin.instance.hitpoints;
		float timer = periodicAreaEventComponent.timer;
		periodicAreaEventComponent.EventExecute();
		periodicAreaEventComponent.PhysicsProcess(1.5, Engine.GetPhysicsFrames() + 100);
		Check(Math.Abs(trashBin.instance.hitpoints - hitpoints2) < 0.001, "Covered Icenut must not keep damaging TrashBin through periodic events.");
		Check((double)Math.Abs(periodicAreaEventComponent.timer - timer) < 0.001, "Covered Icenut's periodic timer must pause instead of accumulating hidden attacks.");
		trashBin.DestroySet();
		Check(!icenut.characterDisabled && icenut.componentAlive && !icenut.invisible && icenut.instance.canBeCollection && !icenut.skipDestroySet, "Destroying the real TrashBin must restore Icenut's component lifecycle.");
		double hitpoints3 = trashBin.instance.hitpoints;
		periodicAreaEventComponent.EventExecute();
		double num2 = hitpoints3 - trashBin.instance.hitpoints;
		Check(Math.Abs(num2 - 20.0) < 0.001, $"Icenut contact damage must resume after TrashBin release; dealt {num2}.");
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

	private static void PlaceCharacter(TowerDefenseCharacter character)
	{
		character.Position = GridBegin + new Vector2((float)(TestGrid.X - 1) * GridSize.X, (float)(TestGrid.Y - 1) * GridSize.Y);
		character.gridPos = TestGrid;
		character.inGame = false;
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
			GD.PushError("[BugOverviewIcenutTrashBinDisableRuntimeTest] " + message);
		}
	}
}
