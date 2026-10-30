using System;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentMobileDoomCobCannonAimRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Plant/Gold/DoomCobCannon/Scene/TowerDefensePlantDoomCobCannon.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		MobileDoomCobCannonAimRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlantDoomCobCannon cannonPlant = null;
		PackedScene scene = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00df;
				}
				control = new MobileDoomCobCannonAimRuntimeControlStub
				{
					Name = "MobileDoomCobCannonAimRuntimeControl",
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
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
				{
					mapControl = mapControl,
					control = control,
					config = new TowerDefenseMapConfig(),
					groundRect = new Rect2(-1000f, -1000f, 3000f, 3000f)
				});
				control.featureDictionary[new StringName("Map")] = mapFeature;
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/DoomCobCannon/Scene/TowerDefensePlantDoomCobCannon.tscn", null, ResourceLoader.CacheMode.Ignore);
				cannonPlant = scene?.Instantiate<TowerDefensePlantDoomCobCannon>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(scene) && GodotObject.IsInstanceValid(cannonPlant), "The real Doom Cob Cannon scene and production script must instantiate.");
				if (!GodotObject.IsInstanceValid(cannonPlant))
				{
					goto end_IL_00df;
				}
				cannonPlant.editorPreviewMode = true;
				cannonPlant.inGame = true;
				cannonPlant.GlobalPosition = new Vector2(300f, 300f);
				control.characterNode.AddChild(cannonPlant, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				CannonComponent cannon = cannonPlant.componentManager?.GetRuntime<CannonComponent>();
				MousePressComponent mousePress = cannonPlant.componentManager?.GetRuntime<MousePressComponent>();
				Check(cannon != null && !cannon.IsReleased && cannon.Lifecycle == ComponentRuntimeLifecycle.Active && mousePress != null && !mousePress.IsReleased && mousePress.Lifecycle == ComponentRuntimeLifecycle.Active, "The real CannonComponent and MousePressComponent runtimes must be active.");
				if (cannon == null || cannon.IsReleased || mousePress == null || mousePress.IsReleased)
				{
					goto end_IL_00df;
				}
				Vector2 firstScreenPosition;
				bool flag = mousePress.TryGetClickShapeScreenCenter(out firstScreenPosition);
				Check(string.Equals(cannon.mode, "Marker", StringComparison.OrdinalIgnoreCase) && mousePress.ToggleMode && flag && GodotObject.IsInstanceValid(mousePress.ClickShape) && GodotObject.IsInstanceValid(mousePress.ToggleTarget), "The real Doom Cob Cannon must use the two-stage marker aiming definition.");
				cannon.canFire = true;
				mousePress.SetAlive(alive: true);
				await WaitFrames(2);
				int fireCount = 0;
				cannon.OnFire += delegate
				{
					fireCount++;
				};
				Vector2 vector = new Vector2(620f, 360f);
				Vector2 screenPosition = cannonPlant.GetCanvasTransform() * vector;
				Check(manager.GetGroundRect().HasPoint(vector), "The confirmation touch must resolve inside the production ground target rect.");
				DispatchMouse(cannonPlant.componentManager, firstScreenPosition, pressed: true);
				Check(mousePress.IsPressed && mousePress.ToggleTarget.Visible && fireCount == 0 && cannon.canFire, "A mouse-first Android compatibility event may start aiming but must not fire.");
				DispatchTouch(cannonPlant.componentManager, 0, firstScreenPosition, pressed: true);
				Check(mousePress.IsPressed && mousePress.ToggleTarget.Visible && fireCount == 0 && cannon.canFire, "The matching first touch-down must be deduplicated instead of becoming confirmation.");
				DispatchTouch(cannonPlant.componentManager, 0, firstScreenPosition, pressed: false);
				DispatchMouse(cannonPlant.componentManager, firstScreenPosition, pressed: false);
				Check(mousePress.IsPressed && mousePress.ToggleTarget.Visible && fireCount == 0 && cannon.canFire, "Releasing the first mobile touch must preserve aiming without firing.");
				DispatchMouse(cannonPlant.componentManager, screenPosition, pressed: true);
				Check(mousePress.IsPressed && mousePress.ToggleTarget.Visible && fireCount == 0 && cannon.canFire, "After touch input is observed, its compatibility mouse copy must be ignored.");
				DispatchTouch(cannonPlant.componentManager, 1, screenPosition, pressed: true);
				Check(mousePress.IsPressed && mousePress.ToggleTarget.Visible && fireCount == 0 && cannon.canFire, "The confirmation touch-down must arm the selected target without firing early.");
				DispatchTouch(cannonPlant.componentManager, 1, screenPosition, pressed: false);
				DispatchMouse(cannonPlant.componentManager, screenPosition, pressed: false);
				Check(!mousePress.IsPressed && !mousePress.ToggleTarget.Visible && fireCount == 1 && !cannon.canFire, "Only the matching confirmation release may commit one cannon shot.");
				Check(cannon.targetPos.IsEqualApprox(vector), $"The committed cannon target must be the confirmation position; expected={vector}, actual={cannon.targetPos}.");
				goto end_IL_00cd;
				end_IL_00df:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentMobileDoomCobCannonAimRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00cd;
			}
			return;
			end_IL_00cd:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(cannonPlant) && !cannonPlant.IsQueuedForDeletion())
			{
				cannonPlant.QueueFree();
			}
			await WaitFrames(6);
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
			await WaitFrames(6);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			scene?.Dispose();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag2 = _failures == 0 && _checks == 12;
		GD.Print($"MOBILE_DOOM_COB_CANNON_AIM_RESULT version=2 passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
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

	private static void DispatchMouse(ComponentManager manager, Vector2 screenPosition, bool pressed)
	{
		InputEventMouseButton inputEventMouseButton = new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Position = screenPosition,
			Pressed = pressed
		};
		manager._Input(inputEventMouseButton);
		inputEventMouseButton.Dispose();
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
			GD.PushError("[BugDepartmentMobileDoomCobCannonAimRuntimeTest] " + message);
		}
	}
}
