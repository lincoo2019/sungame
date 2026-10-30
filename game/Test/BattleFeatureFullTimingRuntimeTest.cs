using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BattleFeatureFullTimingRuntimeTest : Node
{
	private static readonly StringName RequiredName = "TimingProbeRequired";

	private static readonly StringName OptionalName = "TimingProbeOptional";

	private static readonly StringName OwnerName = "TimingProbeOwner";

	private static readonly StringName SiblingName = "TimingProbeSibling";

	private static readonly StringName TutorialName = "TimingProbeTutorial";

	private static readonly StringName ProcessName = "TimingProbeProcess";

	private static readonly string[] FeatureOrder = new string[4] { "Required", "Optional", "Owner", "Sibling" };

	private int _checks;

	private int _failures;

	private BattleFeatureFullTimingRuntimeControlStub _control;

	private MainButton _pauseButton;

	private SpriteBrightButton _optionButton;

	private CheckBox _speedCheckBox;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		TowerDefenseLevelBaseConfig previousLevel = manager?.currentLevelConfig;
		TowerDefenseLevelConfig level = null;
		TowerDefenseZombie probeZombie = null;
		TowerDefenseCharacterInstance probeInstance = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00a3;
				}
				RegisterProbeGraph();
				await VerifyTutorialStartsLast(manager);
				await VerifyNewLevelInitialization(manager);
				BattleFeatureFullTimingProbeLog.Reset();
				level = CreateProbeLevel();
				SetupControl(manager, level);
				bool condition = _control.OldLevelInit(level);
				Check(condition, "The production OldLevelInit path must build the probe graph.");
				Check(_control.featureDictionary.Keys.Select((StringName key) => key.ToString()).SequenceEqual(new string[4]
				{
					RequiredName.ToString(),
					OptionalName.ToString(),
					OwnerName.ToString(),
					SiblingName.ToString()
				}), "Required and configured-optional dependencies must initialize before their owner.");
				Check(_control.process is BattleFeatureFullTimingProbeProcess, "The production SetProcess path must install the probe Process after its Feature dependencies.");
				ExpectExact("Init/OnReady", 0, new string[10] { "Init:Required", "Init:Optional", "Init:Owner", "Init:Sibling", "Init:Process", "OnReady:Required", "OnReady:Optional", "OnReady:Owner", "OnReady:Sibling", "OnReady:Process" });
				await VerifyAsyncPhase("GameInit", _control.GameInitEntered);
				await VerifyProgressAwarePhase("GameInit", "GameInitFromProgress", (TowerDefenseBattleComponentBase component) => component.GameInit(), (TowerDefenseBattleComponentBase component) => component.GameInitFromProgress());
				await VerifyAsyncPhase("GameEntry", _control.GameEntryEntered);
				await VerifyAsyncPhase("GameReady", _control.GameReadyEntered);
				await VerifyAsyncPhase("GameStart", _control.GameRunningEntered);
				await VerifyProgressAwarePhase("GameStart", "GameStartFromProgress", (TowerDefenseBattleComponentBase component) => component.GameStart(), (TowerDefenseBattleComponentBase component) => component.GameStartFromProgress());
				Check(_control.isGameRunning && !_control.isInit, "GameRunning must publish its state before Feature GameStart dispatch.");
				int count = BattleFeatureFullTimingProbeLog.Entries.Count;
				_control.GameRunningProcessing(1.0 / 60.0);
				ExpectExact("per-frame", count, new string[5] { "Process:Required", "Process:Optional", "Process:Owner", "Process:Sibling", "PhysicsProcess:Process" });
				int count2 = BattleFeatureFullTimingProbeLog.Entries.Count;
				_control.GameFailEntered();
				ExpectExact("GameFail", count2, new string[5] { "GameFail:Required", "GameFail:Optional", "GameFail:Owner", "GameFail:Sibling", "GameFail:Process" });
				probeInstance = new TowerDefenseCharacterInstance();
				probeZombie = new TowerDefenseZombie
				{
					instance = probeInstance
				};
				int count3 = BattleFeatureFullTimingProbeLog.Entries.Count;
				MethodInfo method = typeof(TowerDefenseControlNew).GetMethod("HandleZombieEnterHouse", BindingFlags.Instance | BindingFlags.NonPublic);
				Check(method != null, "The runtime fixture must reach the production ZombieEnterHouse dispatcher.");
				method?.Invoke(_control, new object[1] { probeZombie });
				ExpectExact("ZombieEnterHouse", count3, new string[5] { "ZombieEnterHouse:Required", "ZombieEnterHouse:Optional", "ZombieEnterHouse:Owner", "ZombieEnterHouse:Sibling", "ZombieEnterHouse:Process" });
				Check(_control.featureDictionary.Values.All((TowerDefenseBattleFeature feature) => feature.SaveFeature().Count == 0 && feature.SyncSerialize().Count == 0) && _control.process.SaveProcess().Count == 0 && _control.process.SyncSerialize().Count == 0, "Stateless timing probes must not invent progress or network state.");
				int count4 = BattleFeatureFullTimingProbeLog.Entries.Count;
				_control.Free();
				_control = null;
				ExpectExact("Destroy", count4, new string[5] { "Destroy:Process", "Destroy:Sibling", "Destroy:Owner", "Destroy:Optional", "Destroy:Required" });
				goto end_IL_0078;
				end_IL_00a3:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BattleFeatureFullTimingRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0078;
			}
			return;
			end_IL_0078:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.Free();
			}
			_control = null;
			if (GodotObject.IsInstanceValid(probeZombie))
			{
				probeZombie.Free();
			}
			probeInstance?.Dispose();
			FreeDetachedUi();
			UnregisterProbeGraph();
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.currentLevelConfig = previousLevel;
			}
			level?.Dispose();
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"BATTLE_FEATURE_FULL_TIMING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyAsyncPhase(string phase, Action dispatch)
	{
		int start = BattleFeatureFullTimingProbeLog.Entries.Count;
		dispatch();
		Check(await WaitUntil(() => BattleFeatureFullTimingProbeLog.Entries.Skip(start).Contains(phase + ".end:Process"), 120), phase + " must reach the Process after all Feature callbacks.");
		ExpectExact(phase, start, BuildPhaseExpectation(phase));
	}

	private async Task VerifyProgressAwarePhase(string freshPhase, string progressPhase, Func<TowerDefenseBattleComponentBase, Task> fresh, Func<TowerDefenseBattleComponentBase, Task> fromProgress)
	{
		int start = BattleFeatureFullTimingProbeLog.Entries.Count;
		MethodInfo method = typeof(TowerDefenseControlNew).GetMethod("InvokeLifecycleWithProgress", BindingFlags.Instance | BindingFlags.NonPublic);
		Check(method != null, "The runtime fixture must reach the production progress-aware lifecycle dispatcher.");
		if (!(method == null))
		{
			Task task = method.Invoke(_control, new object[2] { fresh, fromProgress }) as Task;
			Check(task != null, "The production progress-aware lifecycle dispatcher must return an awaitable Task.");
			if (task != null)
			{
				await task;
				ExpectExact(progressPhase, start, BuildProgressPhaseExpectation(freshPhase, progressPhase));
			}
		}
	}

	private static string[] BuildPhaseExpectation(string phase)
	{
		List<string> list = new List<string>();
		foreach (string item in FeatureOrder.Append("Process"))
		{
			list.Add(phase + ".begin:" + item);
			list.Add(phase + ".end:" + item);
		}
		return list.ToArray();
	}

	private static string[] BuildProgressPhaseExpectation(string freshPhase, string progressPhase)
	{
		List<string> list = new List<string>();
		foreach (string item in FeatureOrder.Append("Process"))
		{
			string text = ((item == "Optional") ? freshPhase : progressPhase);
			list.Add(text + ".begin:" + item);
			list.Add(text + ".end:" + item);
		}
		return list.ToArray();
	}

	private void ExpectExact(string label, int start, IReadOnlyList<string> expected)
	{
		string[] array = BattleFeatureFullTimingProbeLog.Entries.Skip(start).Take(expected.Count).ToArray();
		Check(array.SequenceEqual(expected), $"{label} order mismatch. expected=[{string.Join(", ", expected)}] actual=[{string.Join(", ", array)}]");
	}

	private async Task VerifyNewLevelInitialization(TowerDefenseManager manager)
	{
		TowerDefenseLevelNewConfig towerDefenseLevelNewConfig = CreateProbeNewLevel();
		BattleFeatureFullTimingProbeLog.Reset();
		try
		{
			SetupControl(manager, towerDefenseLevelNewConfig);
			bool condition = _control.NewLevelInit(towerDefenseLevelNewConfig);
			Check(condition, "The production NewLevelInit path must build the probe graph.");
			Check(_control.featureDictionary.Keys.Select((StringName key) => key.ToString()).SequenceEqual(new string[4]
			{
				RequiredName.ToString(),
				OptionalName.ToString(),
				OwnerName.ToString(),
				SiblingName.ToString()
			}), "NewLevelInit must use the same dependency-first Feature order as OldLevelInit.");
			ExpectExact("NewLevel Init/OnReady", 0, new string[10] { "Init:Required", "Init:Optional", "Init:Owner", "Init:Sibling", "Init:Process", "OnReady:Required", "OnReady:Optional", "OnReady:Owner", "OnReady:Sibling", "OnReady:Process" });
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.Free();
			}
			_control = null;
			FreeDetachedUi();
			towerDefenseLevelNewConfig.Dispose();
			await WaitFrames(1);
		}
	}

	private async Task VerifyTutorialStartsLast(TowerDefenseManager manager)
	{
		TowerDefenseLevelConfig level = CreateTutorialFirstLevel();
		BattleFeatureFullTimingProbeLog.Reset();
		try
		{
			SetupControl(manager, level);
			bool condition = _control.OldLevelInit(level);
			Check(condition, "The tutorial-order probe graph must initialize.");
			Check(_control.featureDictionary.Keys.FirstOrDefault() == TutorialName, "The fixture must place Tutorial before the other configured Features.");
			BattleFeatureFullTimingProbeLog.Reset();
			_control.GameRunningEntered();
			Check(await WaitUntil(() => BattleFeatureFullTimingProbeLog.Entries.Contains("GameStart.end:Tutorial"), 120), "Tutorial content must start last without blocking the shared GameStart lifecycle.");
			ExpectExact("Tutorial-last GameStart", 0, new string[12]
			{
				"GameStart.begin:Sibling", "GameStart.end:Sibling", "GameStart.begin:Required", "GameStart.end:Required", "GameStart.begin:Optional", "GameStart.end:Optional", "GameStart.begin:Owner", "GameStart.end:Owner", "GameStart.begin:Process", "GameStart.end:Process",
				"GameStart.begin:Tutorial", "GameStart.end:Tutorial"
			});
			Check(TutorialManager.Instance.currentTutoroal == (_control.GetFeature(TutorialName) as BattleFeatureFullTimingProbeTutorialFeature)?.config, "The pending tutorial fixture must still be active after GameStart returns.");
			int count = BattleFeatureFullTimingProbeLog.Entries.Count;
			_control.GameRunningProcessing(1.0 / 60.0);
			ExpectExact("Tutorial-active per-frame", count, new string[5] { "Process:Sibling", "Process:Required", "Process:Optional", "Process:Owner", "PhysicsProcess:Process" });
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.Free();
			}
			_control = null;
			FreeDetachedUi();
			level.Dispose();
			await WaitFrames(1);
		}
	}

	private void SetupControl(TowerDefenseManager manager, TowerDefenseLevelBaseConfig level)
	{
		_pauseButton = new MainButton();
		_optionButton = new SpriteBrightButton();
		_speedCheckBox = new CheckBox();
		_control = new BattleFeatureFullTimingRuntimeControlStub
		{
			Name = "BattleFeatureFullTimingRuntimeControl",
			levelConfig = level,
			isInit = true,
			isGameRunning = false,
			buttonPause = _pauseButton,
			optionButton = _optionButton,
			checkBox2X = _speedCheckBox
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		manager.currentLevelConfig = level;
	}

	private static TowerDefenseLevelConfig CreateProbeLevel()
	{
		return new TowerDefenseLevelConfig
		{
			data = null,
			name = "BattleFeatureFullTimingProbe",
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>
			{
				[OwnerName] = ProbeData("Owner"),
				[RequiredName] = ProbeData("Required"),
				[OptionalName] = ProbeData("Optional", canLoadProgress: false),
				[SiblingName] = ProbeData("Sibling")
			},
			processName = ProcessName,
			processData = ProbeData("Process")
		};
	}

	private static TowerDefenseLevelConfig CreateTutorialFirstLevel()
	{
		return new TowerDefenseLevelConfig
		{
			data = null,
			name = "BattleFeatureTutorialLastProbe",
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>
			{
				[TutorialName] = ProbeData("Tutorial"),
				[SiblingName] = ProbeData("Sibling"),
				[OwnerName] = ProbeData("Owner"),
				[RequiredName] = ProbeData("Required"),
				[OptionalName] = ProbeData("Optional", canLoadProgress: false)
			},
			processName = ProcessName,
			processData = ProbeData("Process")
		};
	}

	private static TowerDefenseLevelNewConfig CreateProbeNewLevel()
	{
		return new TowerDefenseLevelNewConfig
		{
			data = null,
			name = "BattleFeatureFullTimingNewProbe",
			featureData = new Godot.Collections.Dictionary<StringName, Dictionary>
			{
				[OwnerName] = ProbeData("Owner"),
				[RequiredName] = ProbeData("Required"),
				[OptionalName] = ProbeData("Optional", canLoadProgress: false),
				[SiblingName] = ProbeData("Sibling")
			},
			processName = ProcessName,
			processData = ProbeData("Process")
		};
	}

	private static Dictionary ProbeData(string id, bool canLoadProgress = true)
	{
		return new Dictionary
		{
			["Id"] = id,
			["CanLoadProgress"] = canLoadProgress
		};
	}

	private static void RegisterProbeGraph()
	{
		TowerDefenseBattleRegistry.RegisterFeature(RequiredName, new BattleFeatureFullTimingProbeFeature());
		TowerDefenseBattleRegistry.RegisterFeature(OptionalName, new BattleFeatureFullTimingProbeFeature());
		TowerDefenseBattleRegistry.RegisterFeature(OwnerName, new BattleFeatureFullTimingProbeFeature());
		TowerDefenseBattleRegistry.RegisterFeature(SiblingName, new BattleFeatureFullTimingProbeFeature());
		TowerDefenseBattleRegistry.RegisterFeature(TutorialName, new BattleFeatureFullTimingProbeTutorialFeature(), 200);
		TowerDefenseBattleRegistry.SetFeatureDependence(RequiredName, new Array<StringName>());
		TowerDefenseBattleRegistry.SetFeatureDependence(OptionalName, new Array<StringName>());
		TowerDefenseBattleRegistry.SetFeatureDependence(OwnerName, new Array<StringName> { RequiredName }, new Array<StringName> { OptionalName });
		TowerDefenseBattleRegistry.SetFeatureDependence(SiblingName, new Array<StringName>());
		TowerDefenseBattleRegistry.SetFeatureDependence(TutorialName, new Array<StringName>());
		TowerDefenseBattleRegistry.RegisterProcess(ProcessName, new BattleFeatureFullTimingProbeProcess());
		TowerDefenseBattleRegistry.SetProcessDependence(ProcessName, new Array<StringName> { OwnerName });
	}

	private static void UnregisterProbeGraph()
	{
		StringName[] array = new StringName[5] { RequiredName, OptionalName, OwnerName, SiblingName, TutorialName };
		foreach (StringName key in array)
		{
			if (TowerDefenseBattleRegistry.BattleFeatureDictionary.Remove(key, out var value))
			{
				TowerDefenseBattleDependenceData dependenceData = value.dependenceData;
				value.Dispose();
				dependenceData?.Dispose();
			}
		}
		if (TowerDefenseBattleRegistry.BattleProcessDictionary.Remove(ProcessName, out var value2))
		{
			TowerDefenseBattleDependenceData dependenceData2 = value2.dependenceData;
			value2.Dispose();
			dependenceData2?.Dispose();
		}
	}

	private void FreeDetachedUi()
	{
		if (GodotObject.IsInstanceValid(_pauseButton))
		{
			_pauseButton.Free();
		}
		if (GodotObject.IsInstanceValid(_optionButton))
		{
			_optionButton.Free();
		}
		if (GodotObject.IsInstanceValid(_speedCheckBox))
		{
			_speedCheckBox.Free();
		}
		_pauseButton = null;
		_optionButton = null;
		_speedCheckBox = null;
	}

	private async Task<bool> WaitUntil(Func<bool> predicate, int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (predicate())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return predicate();
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
			GD.PushError("[BattleFeatureFullTimingRuntimeTest] " + message);
		}
	}
}
