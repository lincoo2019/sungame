using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class HypnoDoomShroom6FullEntryPerformanceRuntimeTest : Node
{
	private const string BattleScenePath = "res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn";

	private const string ResultPrefix = "HYPNODOOM6_FULL_ENTRY_RESULT ";

	private static readonly System.Collections.Generic.Dictionary<string, string> LevelPaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["diamond2_4"] = "res://Asset/Config/Level/TowerDefense/Challenge/Diamond/Challenge_Level_Diamond2_4.tres",
		["diamond2_5"] = "res://Asset/Config/Level/TowerDefense/Challenge/Diamond/Challenge_Level_Diamond2_5.tres",
		["diamond2_6"] = "res://Asset/Config/Level/TowerDefense/Challenge/Diamond/Challenge_Level_Diamond2_6.tres",
		["diamond2_6_d"] = "res://Asset/Config/Level/TowerDefense/Challenge/Diamond/Challenge_Level_Diamond2_6_D.tres"
	};

	public override async void _Ready()
	{
		string caseName = ReadCaseName();
		if (!LevelPaths.TryGetValue(caseName, out var levelPath))
		{
			GD.PrintErr("HYPNODOOM6_FULL_ENTRY_FAILURE unknown_case=" + caseName);
			GetTree().Quit(2);
			return;
		}
		try
		{
			if (!GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance))
			{
				throw new InvalidOperationException("Required autoloads are unavailable.");
			}
			GameSaveManager.Instance.EnsureLoaded();
			if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
			{
				GameSaveManager.Instance.SetUserCurrent("PerformanceProbe");
			}
			bool resourcesLoaded = false;
			ResourceManager.Instance.OnLoadOver += OnLoadOver;
			try
			{
				ResourceManager.Instance.BeginLoad();
				for (int frame = 0; frame < 7200; frame++)
				{
					if (resourcesLoaded)
					{
						break;
					}
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
			finally
			{
				ResourceManager.Instance.OnLoadOver -= OnLoadOver;
			}
			if (!resourcesLoaded)
			{
				throw new TimeoutException("ResourceManager did not complete within 7200 frames.");
			}
			List<double> baselineFrames = await SampleFrames(30);
			Stopwatch stopwatch = Stopwatch.StartNew();
			TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(levelPath, null, ResourceLoader.CacheMode.IgnoreDeep);
			double levelLoadMs = stopwatch.Elapsed.TotalMilliseconds;
			if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
			{
				throw new InvalidOperationException("Failed to load level " + levelPath + ".");
			}
			TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
			stopwatch.Restart();
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			double battleSceneLoadMs = stopwatch.Elapsed.TotalMilliseconds;
			if (!GodotObject.IsInstanceValid(packedScene) || !packedScene.CanInstantiate())
			{
				throw new InvalidOperationException("Failed to load the real battle scene.");
			}
			stopwatch.Restart();
			TowerDefenseControlNew towerDefenseControlNew = packedScene.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
			double instantiateMs = stopwatch.Elapsed.TotalMilliseconds;
			if (!GodotObject.IsInstanceValid(towerDefenseControlNew))
			{
				throw new InvalidOperationException("Failed to instantiate the real battle scene.");
			}
			stopwatch.Restart();
			AddChild(towerDefenseControlNew, forceReadableName: false, InternalMode.Disabled);
			double addTreeReadyMs = stopwatch.Elapsed.TotalMilliseconds;
			List<double> values = await SampleFrames(180);
			Dictionary dictionary = new Dictionary
			{
				["case"] = caseName,
				["level_load_ms"] = levelLoadMs,
				["battle_scene_load_ms"] = battleSceneLoadMs,
				["instantiate_ms"] = instantiateMs,
				["add_tree_ready_ms"] = addTreeReadyMs,
				["baseline_p95_ms"] = Percentile(baselineFrames, 0.95),
				["baseline_max_ms"] = Maximum(baselineFrames),
				["entry_p95_ms"] = Percentile(values, 0.95),
				["entry_max_ms"] = Maximum(values),
				["entry_over_50ms"] = CountOver(values, 50.0),
				["entry_over_100ms"] = CountOver(values, 100.0)
			};
			GD.Print("HYPNODOOM6_FULL_ENTRY_RESULT " + Json.Stringify(dictionary));
			void OnLoadOver()
			{
				resourcesLoaded = true;
			}
		}
		catch (Exception value)
		{
			GD.PrintErr($"HYPNODOOM6_FULL_ENTRY_FAILURE case={caseName} error={value}");
			GetTree().Quit(2);
			return;
		}
		GetTree().Quit();
	}

	private async Task<List<double>> SampleFrames(int count)
	{
		List<double> samples = new List<double>(count);
		for (int index = 0; index < count; index++)
		{
			long start = Stopwatch.GetTimestamp();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			samples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
		}
		return samples;
	}

	private static double Percentile(List<double> values, double ratio)
	{
		if (values.Count == 0)
		{
			return 0.0;
		}
		List<double> list = new List<double>(values);
		list.Sort();
		int index = Math.Clamp((int)Math.Ceiling((double)list.Count * ratio) - 1, 0, list.Count - 1);
		return list[index];
	}

	private static double Maximum(List<double> values)
	{
		double num = 0.0;
		foreach (double value in values)
		{
			num = Math.Max(num, value);
		}
		return num;
	}

	private static int CountOver(List<double> values, double thresholdMs)
	{
		int num = 0;
		foreach (double value in values)
		{
			if (value > thresholdMs)
			{
				num++;
			}
		}
		return num;
	}

	private static string ReadCaseName()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			if (text.StartsWith("--probe-case=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--probe-case=".Length;
				return text2.Substring(length, text2.Length - length).Trim();
			}
		}
		return "diamond2_6";
	}
}
