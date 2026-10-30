using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPowShroomAnimationOwnershipRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Plant/Chapter7/PowShroom/Scene/TowerDefensePlantPowShroom.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		PowShroomAnimationOwnershipRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlantPowShroom plant = null;
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
				control = new PowShroomAnimationOwnershipRuntimeControlStub
				{
					Name = "PowShroomAnimationOwnershipRuntimeControl",
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
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter7/PowShroom/Scene/TowerDefensePlantPowShroom.tscn", null, ResourceLoader.CacheMode.Ignore);
				plant = scene?.Instantiate<TowerDefensePlantPowShroom>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(scene) && GodotObject.IsInstanceValid(plant), "The real Pow Shroom scene and production script must instantiate.");
				if (!GodotObject.IsInstanceValid(plant))
				{
					goto end_IL_00df;
				}
				plant.editorPreviewMode = true;
				plant.inGame = true;
				plant.GlobalPosition = new Vector2(300f, 300f);
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				CannonComponent cannon = plant.componentManager?.GetRuntime<CannonComponent>("character.cannon");
				AttackComponent attack = plant.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				Check(cannon != null && !cannon.IsReleased && attack != null && !attack.IsReleased && cannon.Lifecycle == ComponentRuntimeLifecycle.Active && attack.Lifecycle == ComponentRuntimeLifecycle.Active, "The real CannonComponent and AttackComponent runtimes must be active.");
				Check(GodotObject.IsInstanceValid(cannon?.sprite) && cannon?.sprite == attack?.sprite, "Pow Shroom must exercise the production shared animation sprite.");
				if (cannon == null || cannon.IsReleased || attack == null || attack.IsReleased || !GodotObject.IsInstanceValid(cannon.sprite))
				{
					goto end_IL_00df;
				}
				plant.WakeUp();
				plant.componentManager.GetRuntime<SleepComponent>()?.ReevaluateEnvironmentState();
				await WaitFrames(6);
				Check(!plant.instance.sleep && plant.componentAlive, "The production wake-up path must restore both sleep and component activity.");
				attack.checkAll = true;
				for (int i = 0; i < 2; i++)
				{
					Check(cannon.SendStateEvent("ToRest"), $"Cycle {i}: Cannon must enter Rest.");
					Check(attack.SendStateEvent("ToAttack"), $"Cycle {i}: shared attack must enter Attack.");
					Check(attack.StateMachine?.CurrentStateHandle?.StableId == "attack.attack" && cannon.sprite.clip == attack.attackAnimeClips, $"Cycle {i}: Attack must own the Fire clip.");
					attack.AttackProcessing(1.0 / 60.0);
					double timeScale = cannon.sprite.timeScale;
					cannon.RestProcessing(1.0 / 60.0);
					Check(Mathf.IsEqualApprox((float)cannon.sprite.timeScale, (float)timeScale) && timeScale > cannon.restAnimeTimeScale, $"Cycle {i}: Rest processing must not slow the active attack.");
					cannon.Timeout("Rest");
					cannon.ChargeProcessing(1.0 / 60.0);
					Check(cannon.StateMachine?.CurrentStateHandle?.StableId == "cannon.charge" && cannon.canFire && cannon.sprite.clip == attack.attackAnimeClips, $"Cycle {i}: becoming clickable must not replace the active Fire clip.");
					attack.AnimeCompleted(attack.attackAnimeClips);
					cannon.ChargeProcessing(1.0 / 60.0);
					Check(attack.StateMachine?.CurrentStateHandle?.StableId == "attack.idle" && cannon.sprite.clip == cannon.chargeAnimeClips && Mathf.IsEqualApprox((float)cannon.sprite.timeScale, (float)(plant.timeScale * cannon.chargeAnimeTimeScale)), $"Cycle {i}: attack exit must restore the charged cannon animation.");
				}
				cannon.FireAt(new Vector2(620f, 360f));
				Check(!cannon.canFire && cannon.StateMachine?.CurrentStateHandle?.StableId == "cannon.fire" && cannon.sprite.clip == cannon.fireAnimeClips, "The restored charged state must still accept a click and play Pow.");
				goto end_IL_00cd;
				end_IL_00df:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPowShroomAnimationOwnershipRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00cd;
			}
			return;
			end_IL_00cd:;
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
		bool flag = _failures == 0 && _checks == 18;
		GD.Print($"POW_SHROOM_ANIMATION_OWNERSHIP_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
			GD.PushError("[BugOverviewPowShroomAnimationOwnershipRuntimeTest] " + message);
		}
	}
}
