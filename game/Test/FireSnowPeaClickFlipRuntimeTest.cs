using System;
using System.Threading.Tasks;
using Godot;

public partial class FireSnowPeaClickFlipRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Plant/Chapter2/FireSnowPea/Scene/TowerDefensePlantFireSnowPea.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		FireSnowPeaClickFlipControlStub control = null;
		TowerDefensePlantFireSnowPea plant = null;
		PackedScene scene = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00e2;
				}
				control = new FireSnowPeaClickFlipControlStub
				{
					Name = "FireSnowPeaClickFlipControl",
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
				manager.gridBeginPos = new Vector2(260f, 75f);
				manager.gridSize = new Vector2(80f, 98f);
				manager.gridNum = new Vector2I(9, 5);
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter2/FireSnowPea/Scene/TowerDefensePlantFireSnowPea.tscn", null, ResourceLoader.CacheMode.Ignore);
				plant = scene?.Instantiate<TowerDefensePlantFireSnowPea>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(scene) && GodotObject.IsInstanceValid(plant), "The real FireSnowPea scene and script must instantiate.");
				if (!GodotObject.IsInstanceValid(plant))
				{
					goto end_IL_00e2;
				}
				plant.editorPreviewMode = true;
				plant.inGame = true;
				plant.GlobalPosition = new Vector2(300f, 300f);
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				MousePressComponent mousePressComponent = plant.componentManager?.GetRuntime<MousePressComponent>();
				Check(mousePressComponent != null && !mousePressComponent.IsReleased && mousePressComponent.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(mousePressComponent.ClickShape), "The real FireSnowPea MousePress runtime and click shape must be active.");
				if (mousePressComponent == null || mousePressComponent.IsReleased || !GodotObject.IsInstanceValid(mousePressComponent.ClickShape))
				{
					goto end_IL_00e2;
				}
				Check(mousePressComponent.TryGetClickShapeScreenCenter(out var screenPosition), "The FireSnowPea click shape must expose a finite screen center.");
				Check(plant.componentManager.HasRuntimeInputWork && mousePressComponent.HasRuntimeInputWork && mousePressComponent.CanDispatchInputWork, "ComponentManager must dispatch the active FireSnowPea MousePress runtime.");
				Check(manager.IsGameRunning() && plant.inGame && plant.componentAlive && mousePressComponent.Alive, "FireSnowPea and MousePress must be interactable during the running fixture.");
				Check(plant.IsInsideComponentBattlefield, "FireSnowPea input must not be gated as outside the component battlefield.");
				Check(mousePressComponent.IsScreenPositionInsideClickShape(screenPosition), "The synthesized click must resolve inside FireSnowPea's configured click shape.");
				float initialScaleX = plant.Scale.X;
				Check(!Mathf.IsZeroApprox(initialScaleX), "The real FireSnowPea must begin with a non-zero horizontal scale.");
				int pressedCount = 0;
				mousePressComponent.OnPressed += delegate
				{
					pressedCount++;
				};
				DispatchMouse(plant.componentManager, screenPosition, pressed: true);
				DispatchMouse(plant.componentManager, screenPosition, pressed: false);
				Check(pressedCount == 1, "A first mouse click must emit exactly one FireSnowPea pressed event.");
				Check(Mathf.Sign(plant.Scale.X) == -Mathf.Sign(initialScaleX), "A first mouse click on FireSnowPea must flip its horizontal facing.");
				await WaitSeconds(0.3);
				DispatchMouse(plant.componentManager, screenPosition, pressed: true);
				DispatchMouse(plant.componentManager, screenPosition, pressed: false);
				Check(Mathf.Sign(plant.Scale.X) == Mathf.Sign(initialScaleX), "A later mouse click must flip FireSnowPea back.");
				await WaitSeconds(0.3);
				DispatchTouch(plant.componentManager, 0, screenPosition, pressed: true);
				DispatchTouch(plant.componentManager, 0, screenPosition, pressed: false);
				Check(Mathf.Sign(plant.Scale.X) == -Mathf.Sign(initialScaleX), "A touch tap on FireSnowPea must flip its horizontal facing once.");
				await WaitSeconds(0.3);
				DispatchTouch(plant.componentManager, 0, screenPosition, pressed: true);
				DispatchTouch(plant.componentManager, 0, screenPosition, pressed: false);
				Check(Mathf.Sign(plant.Scale.X) == Mathf.Sign(initialScaleX), "A repeated touch tap must flip FireSnowPea back.");
				goto end_IL_00c7;
				end_IL_00e2:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[FireSnowPeaClickFlipRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c7;
			}
			return;
			end_IL_00c7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(plant) && !plant.IsQueuedForDeletion())
			{
				plant.QueueFree();
			}
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			scene?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"FIRE_SNOW_PEA_CLICK_FLIP_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void DispatchMouse(ComponentManager manager, Vector2 screenPosition, bool pressed)
	{
		InputEventMouseButton inputEventMouseButton = new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Position = screenPosition,
			GlobalPosition = screenPosition,
			Pressed = pressed
		};
		manager._Input(inputEventMouseButton);
		inputEventMouseButton.Dispose();
	}

	private static void DispatchTouch(ComponentManager manager, int index, Vector2 screenPosition, bool pressed)
	{
		InputEventScreenTouch inputEventScreenTouch = new InputEventScreenTouch
		{
			Index = index,
			Position = screenPosition,
			Pressed = pressed
		};
		manager._Input(inputEventScreenTouch);
		inputEventScreenTouch.Dispose();
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitSeconds(double duration)
	{
		await ToSignal(GetTree().CreateTimer(duration, processAlways: false), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FireSnowPeaClickFlipRuntimeTest] " + message);
		}
	}
}
