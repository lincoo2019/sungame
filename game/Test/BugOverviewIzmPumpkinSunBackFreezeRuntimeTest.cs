using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewIzmPumpkinSunBackFreezeRuntimeTest : Node
{
	private const string PumpkinPacketPath = "res://Asset/Anime/Character/Plant/Chapter4/PumpkinSun/Packet/PlantPumpkinSun.tres";

	private const string PumpkinScenePath = "res://Asset/Anime/Character/Plant/Chapter4/PumpkinSun/Scene/TowerDefensePlantPumpkinSun.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(2, 2);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BugOverviewIzmPumpkinSunBackFreezeRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseBattleProcessIZM izmProcess = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_009d;
				}
				RegisterRealFixtures();
				control = new BugOverviewIzmPumpkinSunBackFreezeRuntimeControlStub
				{
					Name = "IzmPumpkinSunBackFreezeRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM,
						izmManager = new TowerDefenseLevelIZMManagerConfig()
					}
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
				izmProcess = new TowerDefenseBattleProcessIZM
				{
					control = control,
					mapFeature = mapFeature
				};
				izmProcess.Init(new Dictionary());
				control.process = izmProcess;
				Check(TowerDefenseManager.GetGameMethod() == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM, "The fixture must exercise the production IZM finish method.");
				Check(control.process == izmProcess && izmProcess.GetType() == typeof(TowerDefenseBattleProcessIZM), "The fixture must use a real TowerDefenseBattleProcessIZM instance.");
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter4/PumpkinSun/Packet/PlantPumpkinSun.tres");
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && towerDefensePacketConfig.characterConfig?.name == "PlantPumpkinSun", "The real PumpkinSun packet and character config must load.");
				TowerDefensePlantPumpkinSun pumpkin = towerDefensePacketConfig?.Plant(TestGrid, playAudio: false) as TowerDefensePlantPumpkinSun;
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(pumpkin), "The real PumpkinSun scene must spawn on the IZM map.");
				if (!GodotObject.IsInstanceValid(pumpkin))
				{
					goto end_IL_009d;
				}
				PumpkinSunSprite sprite = pumpkin.sprite as PumpkinSunSprite;
				AdobeAnimateSpriteBase back = sprite?.back;
				Check(GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(back), "PumpkinSun must expose its real front and Back Adobe animation nodes.");
				if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(back))
				{
					goto end_IL_009d;
				}
				BugOverviewIzmPumpkinSunBackFreezeRuntimeTest bugOverviewIzmPumpkinSunBackFreezeRuntimeTest = this;
				PuzzleShaderComponent puzzleShaderComponent = pumpkin.puzzleShaderComponent;
				bugOverviewIzmPumpkinSunBackFreezeRuntimeTest.Check(puzzleShaderComponent != null && !puzzleShaderComponent.IsReleased, "The real PumpkinSun PuzzleShaderComponent must be active.");
				Check(pumpkin.CurrentStateHandle?.StableId == "character.idle", "PumpkinSun must enter character.idle in IZM; current=" + pumpkin.CurrentStateHandle?.StableId + ".");
				Check(IsZero(pumpkin.timeScaleInit) && IsZero(pumpkin.timeScale), $"IZM idle must freeze the PumpkinSun character clock; init={pumpkin.timeScaleInit}, current={pumpkin.timeScale}.");
				Check(IsZero(sprite.timeScale) && IsZero(back.timeScale), $"IZM idle must freeze both front and Back animation clocks; front={sprite.timeScale}, back={back.timeScale}.");
				Check(PhasesMatch(sprite, back), $"IZM idle front and Back phases must match; front={DescribePhase(sprite)}, back={DescribePhase(back)}.");
				Check(!sprite.UsesRuntimeGpuClockInterpolation && !back.UsesRuntimeGpuClockInterpolation, "Zero IZM idle speeds must disable autonomous GPU clock interpolation for front and Back.");
				int firstIdleFrontFrame = sprite.frameIndex;
				double firstIdleFrontElapsed = sprite.elapsedTimer;
				int firstIdleBackFrame = back.frameIndex;
				double firstIdleBackElapsed = back.elapsedTimer;
				await WaitFrames(6);
				Check(PhaseUnchanged(sprite, firstIdleFrontFrame, firstIdleFrontElapsed) && PhaseUnchanged(back, firstIdleBackFrame, firstIdleBackElapsed), $"Both PumpkinSun layers must remain frozen throughout IZM idle; front={DescribePhase(sprite)}, back={DescribePhase(back)}.");
				pumpkin.Component();
				await WaitFrames(4);
				Check(pumpkin.CurrentStateHandle?.StableId == "character.component", "The production state event must leave IZM idle; current=" + pumpkin.CurrentStateHandle?.StableId + ".");
				Check(pumpkin.timeScaleInit > 0.0 && pumpkin.timeScale > 0.0, $"Leaving IZM idle must restore the character clock; init={pumpkin.timeScaleInit}, current={pumpkin.timeScale}.");
				Check(sprite.timeScale > 0.0 && ApproximatelyEqual(sprite.timeScale, back.timeScale), $"Leaving IZM idle must restore equal front and Back speeds; front={sprite.timeScale}, back={back.timeScale}.");
				Check(PhasesMatch(sprite, back), $"The moving front and Back layers must remain phase-locked; front={DescribePhase(sprite)}, back={DescribePhase(back)}.");
				Check(!PhaseUnchanged(sprite, firstIdleFrontFrame, firstIdleFrontElapsed), "The PumpkinSun animation must resume after leaving IZM idle.");
				pumpkin.Idle();
				await WaitFrames(4);
				Check(pumpkin.CurrentStateHandle?.StableId == "character.idle", "The production state event must return PumpkinSun to idle; current=" + pumpkin.CurrentStateHandle?.StableId + ".");
				Check(IsZero(pumpkin.timeScaleInit) && IsZero(pumpkin.timeScale), $"Re-entering IZM idle must freeze the character clock again; init={pumpkin.timeScaleInit}, current={pumpkin.timeScale}.");
				Check(IsZero(sprite.timeScale) && IsZero(back.timeScale), $"Re-entering IZM idle must freeze both animation layers again; front={sprite.timeScale}, back={back.timeScale}.");
				Check(PhasesMatch(sprite, back), $"The second IZM idle must stop on one shared phase; front={DescribePhase(sprite)}, back={DescribePhase(back)}.");
				int secondIdleFrontFrame = sprite.frameIndex;
				double secondIdleFrontElapsed = sprite.elapsedTimer;
				int secondIdleBackFrame = back.frameIndex;
				double secondIdleBackElapsed = back.elapsedTimer;
				await WaitFrames(6);
				Check(PhaseUnchanged(sprite, secondIdleFrontFrame, secondIdleFrontElapsed) && PhaseUnchanged(back, secondIdleBackFrame, secondIdleBackElapsed), $"Front and Back must stay frozen after the second idle entry; front={DescribePhase(sprite)}, back={DescribePhase(back)}.");
				Check(!sprite.UsesRuntimeGpuClockInterpolation && !back.UsesRuntimeGpuClockInterpolation, "The second IZM idle must also leave both autonomous GPU clocks disabled.");
				goto end_IL_007e;
				end_IL_009d:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewIzmPumpkinSunBackFreezeRuntimeTest] Unexpected exception: {value}");
				goto end_IL_007e;
			}
			return;
			end_IL_007e:;
		}
		finally
		{
			izmProcess?.Destroy();
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
			RestoreRealFixtures();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 25;
		GD.Print($"IZM_PUMPKIN_SUN_BACK_FREEZE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static bool IsZero(double value)
	{
		return Math.Abs(value) < 1E-06;
	}

	private static bool ApproximatelyEqual(double left, double right)
	{
		return Math.Abs(left - right) < 1E-06;
	}

	private static bool PhasesMatch(AdobeAnimateSpriteBase front, AdobeAnimateSpriteBase back)
	{
		if (front.frameIndex == back.frameIndex)
		{
			return ApproximatelyEqual(front.elapsedTimer, back.elapsedTimer);
		}
		return false;
	}

	private static bool PhaseUnchanged(AdobeAnimateSpriteBase sprite, int frame, double elapsed)
	{
		if (sprite.frameIndex == frame)
		{
			return ApproximatelyEqual(sprite.elapsedTimer, elapsed);
		}
		return false;
	}

	private static string DescribePhase(AdobeAnimateSpriteBase sprite)
	{
		return $"{sprite.frameIndex}+{sprite.elapsedTimer:F6}";
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPumpkinSun", "res://Asset/Anime/Character/Plant/Chapter4/PumpkinSun/Packet/PlantPumpkinSun.tres");
		RegisterCharacter("PlantPumpkinSun", "res://Asset/Anime/Character/Plant/Chapter4/PumpkinSun/Scene/TowerDefensePlantPumpkinSun.tscn");
	}

	private void RegisterPacket(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewIzmPumpkinSunBackFreezeRuntimeTest] " + message);
		}
	}
}
