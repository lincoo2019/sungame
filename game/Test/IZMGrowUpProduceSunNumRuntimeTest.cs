using System;
using Godot;

public partial class IZMGrowUpProduceSunNumRuntimeTest : Node
{
	private const string SunShroomScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunShroom/Scene/TowerDefensePlantSunShroom.tscn";

	private const int ExpectedGrownSunNum = 137;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		IZMGrowUpProduceSunNumRuntimeControlStub control = null;
		TowerDefensePlantSunShroom sunShroom = null;
		try
		{
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_0069;
				}
				control = new IZMGrowUpProduceSunNumRuntimeControlStub
				{
					Name = "IZMGrowUpProduceSunNumRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM,
						izmManager = new TowerDefenseLevelIZMManagerConfig()
					}
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				Check(manager.IsIZMMode(), "The fixture must run through the production IZM mode check.");
				sunShroom = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/SunShroom/Scene/TowerDefensePlantSunShroom.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantSunShroom>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(sunShroom), "The real SunShroom scene must instantiate.");
				if (!GodotObject.IsInstanceValid(sunShroom))
				{
					goto end_IL_0069;
				}
				sunShroom.inGame = false;
				sunShroom.growUpSunNum = 137;
				AddChild(sunShroom, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				GrowUpComponent growUpComponent = sunShroom.componentManager?.GetRuntime<GrowUpComponent>();
				ProduceComponent produceComponent = sunShroom.componentManager?.GetRuntime<ProduceComponent>();
				Check(growUpComponent != null && !growUpComponent.IsReleased && produceComponent != null && !produceComponent.IsReleased, "The real growth and production runtimes must both be active.");
				Check(growUpComponent?.growUpReach == growUpComponent?.growUpTime.Count && growUpComponent != null && growUpComponent.growUpReach == 1, $"IZM must fast-forward the real growth runtime; reach={growUpComponent?.growUpReach}.");
				Check(produceComponent != null && produceComponent.num == 137, $"The IZM growth callback must synchronize grown sunNum to ProduceComponent; actual={produceComponent?.num}.");
				IZMGrowUpProduceSunNumRuntimeTest iZMGrowUpProduceSunNumRuntimeTest = this;
				Vector2? vector = sunShroom.transformPoint?.Scale;
				Vector2 one = Vector2.One;
				float? obj = growUpComponent?.growUpSize[0];
				iZMGrowUpProduceSunNumRuntimeTest.Check(vector == one * obj, "IZM fast growth must still apply the final configured visual scale.");
				goto end_IL_0060;
				end_IL_0069:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[IZMGrowUpProduceSunNumRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0060;
			}
			return;
			end_IL_0060:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(sunShroom))
			{
				sunShroom.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 7;
		GD.Print($"IZM_GROW_UP_PRODUCE_SUN_NUM_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[IZMGrowUpProduceSunNumRuntimeTest] " + message);
		}
	}
}
