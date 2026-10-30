using System;
using System.Collections.Generic;
using Godot;

public partial class BenchmarkSceneDispatcher : Node
{
	private const string ArgumentPrefix = "--benchmark-scene=";

	private static readonly IReadOnlyDictionary<string, string> AllowedScenes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["short"] = "res://Test/DamageHotPathWorkloadTest.tscn",
		["damage"] = "res://Test/DamageHotPathWorkloadTest.tscn",
		["characters"] = "res://Test/CharacterStressWorkloadTest.tscn",
		["bullets"] = "res://Test/BulletFieldWorkloadTest.tscn",
		["mod"] = "res://Test/ModRuntimeCompatibilityTest.tscn"
	};

	public override void _Ready()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			if (text.StartsWith("--benchmark-scene=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--benchmark-scene=".Length;
				string benchmarkName = text2.Substring(length, text2.Length - length).Trim();
				Callable.From(delegate
				{
					Dispatch(benchmarkName);
				}).CallDeferred();
				break;
			}
		}
	}

	private void Dispatch(string benchmarkName)
	{
		if (!AllowedScenes.TryGetValue(benchmarkName, out var value))
		{
			GD.PrintErr("[BenchmarkSceneDispatcher] Unknown benchmark scene '" + benchmarkName + "'.");
			GetTree().Quit(2);
			return;
		}
		PackedScene packedScene = GD.Load<PackedScene>(value);
		if (packedScene == null || !packedScene.CanInstantiate())
		{
			GD.PrintErr("[BenchmarkSceneDispatcher] Failed to load '" + value + "'.");
			GetTree().Quit(2);
			return;
		}
		Error error = GetTree().ChangeSceneToPacked(packedScene);
		GD.Print($"[BenchmarkSceneDispatcher] benchmark={benchmarkName} scene={value} error={error}");
		if (error != Error.Ok)
		{
			GetTree().Quit(2);
		}
	}
}
