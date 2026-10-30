using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSunGodJalapenoSunRuntimeTest : Node
{
	private const string SunGodScenePath = "res://Asset/Anime/Character/Plant/Diamond/SunGodBean/Scene/TowerDefensePlantSunGodBean.tscn";

	private const string JalaSunShroomScenePath = "res://Asset/Anime/Character/Plant/Chapter2/JalaSunShroom/Scene/TowerDefensePlantJalaSunShroom.tscn";

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		SunGodJalapenoSunRuntimeControlStub control = null;
		TowerDefensePlantSunGodBean sunGod = null;
		TowerDefensePlantJalaSunShroom jalaSunShroom = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(ObjectManager.Instance), "Required runtime autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_007b;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				control = new SunGodJalapenoSunRuntimeControlStub
				{
					Name = "SunGodJalapenoSunRuntimeControl",
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
				jalaSunShroom = Instantiate<TowerDefensePlantJalaSunShroom>("res://Asset/Anime/Character/Plant/Chapter2/JalaSunShroom/Scene/TowerDefensePlantJalaSunShroom.tscn");
				Check(GodotObject.IsInstanceValid(jalaSunShroom) && jalaSunShroom.config?.name == "PlantJalaSunShroom", "The fixture must load the real Jalapeno Sun-shroom scene.");
				if (!GodotObject.IsInstanceValid(jalaSunShroom))
				{
					goto end_IL_007b;
				}
				jalaSunShroom.inGame = true;
				jalaSunShroom.gridPos = new Vector2I(3, 2);
				jalaSunShroom.GlobalPosition = new Vector2(500f, 300f);
				control.characterNode.AddChild(jalaSunShroom, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				manager.CharacterRegister(jalaSunShroom);
				ProduceComponent produce = jalaSunShroom.componentManager?.GetRuntime<ProduceComponent>();
				Check(produce != null && !produce.IsReleased && produce.produceType == "JalaSun" && produce.num == 15, "The real Jalapeno Sun-shroom must expose its authored JalaSun production runtime.");
				sunGod = Instantiate<TowerDefensePlantSunGodBean>("res://Asset/Anime/Character/Plant/Diamond/SunGodBean/Scene/TowerDefensePlantSunGodBean.tscn");
				Check(GodotObject.IsInstanceValid(sunGod) && sunGod.config?.name == "PlantSunGodBean", "The fixture must load the real Sun God Bean scene.");
				if (!GodotObject.IsInstanceValid(sunGod))
				{
					goto end_IL_007b;
				}
				sunGod.inGame = false;
				sunGod.gridPos = new Vector2I(2, 2);
				sunGod.GlobalPosition = new Vector2(400f, 300f);
				control.characterNode.AddChild(sunGod, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				manager.CharacterRegister(sunGod);
				Check(manager.GetCampFriendly(TowerDefenseEnum.CHARACTER_CAMP.PLANT).Contains(jalaSunShroom), "Sun God Bean must see the real Jalapeno Sun-shroom as a friendly production target.");
				int beforeCount = GetTree().GetNodeCountInGroup("JalapenoSun");
				produce.timer = 0f;
				sunGod.Explode();
				Check(produce.timer >= produce.produceInterval, "Sun God Bean must advance the Jalapeno Sun-shroom to its immediate production tick.");
				produce.PhysicsProcess(0.0, TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
				await WaitFrames(2);
				Array<Node> nodesInGroup = GetTree().GetNodesInGroup("JalapenoSun");
				Check(nodesInGroup.Count == beforeCount + 1, "Sun God Bean must summon one real Jalapeno Sun drop from the Jalapeno Sun-shroom.");
				bool condition = false;
				foreach (Node item in nodesInGroup)
				{
					if (item is TowerDefenseSunJalapeno { sunNum: var sunNum } && sunNum == produce?.num)
					{
						condition = true;
						break;
					}
				}
				Check(condition, "The summoned Jalapeno Sun must preserve the mushroom's authored production amount.");
				goto end_IL_0060;
				end_IL_007b:;
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
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
			if (GodotObject.IsInstanceValid(sunGod) && !sunGod.IsQueuedForDeletion())
			{
				sunGod.QueueFree();
			}
			if (GodotObject.IsInstanceValid(jalaSunShroom) && !jalaSunShroom.IsQueuedForDeletion())
			{
				jalaSunShroom.QueueFree();
			}
			await WaitFrames(3);
			ObjectManager.Instance?.Clear();
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
		}
		Finish();
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
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[BugOverviewSunGodJalapenoSunRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 8;
		GD.Print($"SUN_GOD_JALAPENO_SUN_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
