using System;
using Godot;
using Godot.Collections;

public partial class BugOverviewSunCactusChallengeThreeTitleRuntimeTest : Node
{
	private const string LevelRegistryPath = "res://Asset/Config/Level/LevelResource.json";

	private const string TargetSaveKey = "Challenge_Level17_3";

	private const string ExpectedNormalPath = "res://Asset/Config/Level/TowerDefense/Challenge/Gold/Challenge_Level17_3.tres";

	private const string ExpectedDifficultPath = "res://Asset/Config/Level/TowerDefense/Challenge/Gold/Challenge_Level17_3_D.tres";

	private const string ProgressManagerPath = "res://Registry/Battle/Feature/Progress/Manager/TowerDefenseProgressManager.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		string previousLocale = TranslationServer.GetLocale();
		TowerDefenseLevelConfig normalConfig = null;
		TowerDefenseLevelConfig difficultConfig = null;
		TowerDefenseProgressManager normalProgress = null;
		TowerDefenseProgressManager difficultProgress = null;
		try
		{
			try
			{
				Json json = ResourceLoader.Load<Json>("res://Asset/Config/Level/LevelResource.json", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(json), "The production level registry must load.");
				if (!GodotObject.IsInstanceValid(json))
				{
					goto end_IL_0057;
				}
				Dictionary dictionary = FindLevelEntry(json.Data.AsGodotDictionary(), "Challenge_Level17_3");
				Check(dictionary.Count > 0, "The production challenge registry must contain Sun Cactus challenge 3.");
				if (dictionary.Count == 0)
				{
					goto end_IL_0057;
				}
				Check(dictionary.GetValueOrDefault("OpenKey", "").AsString() == "Challenge_Level17_2", "Challenge 3 must remain unlocked by completing challenge 2.");
				Dictionary dictionary2 = dictionary.GetValueOrDefault("Level", new Dictionary()).AsGodotDictionary();
				string text = dictionary2.GetValueOrDefault("Normal", "").AsString();
				string text2 = dictionary2.GetValueOrDefault("Difficult", "").AsString();
				Check(ResolveUidPath(text) == "res://Asset/Config/Level/TowerDefense/Challenge/Gold/Challenge_Level17_3.tres", "The registry Normal UID must resolve to the challenge-3 resource.");
				Check(ResolveUidPath(text2) == "res://Asset/Config/Level/TowerDefense/Challenge/Gold/Challenge_Level17_3_D.tres", "The registry Difficult UID must resolve to the challenge-3 resource.");
				normalConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(text, null, ResourceLoader.CacheMode.Ignore);
				difficultConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(text2, null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(normalConfig) && GodotObject.IsInstanceValid(difficultConfig), "Both production challenge-3 difficulties must load through their registry UIDs.");
				if (!GodotObject.IsInstanceValid(normalConfig) || !GodotObject.IsInstanceValid(difficultConfig))
				{
					goto end_IL_0057;
				}
				Check(normalConfig.name == "Challenge_Level17_3" && difficultConfig.name == "Challenge_Level17_3", "Both resources must identify themselves as challenge 3.");
				Check(normalConfig.levelNumber == 3 && difficultConfig.levelNumber == 3, "Both runtime resources must retain LevelNumber 3 instead of the default 1.");
				Check(ReadSourceLevelNumber(normalConfig) == 3 && ReadSourceLevelNumber(difficultConfig) == 3, "Both backing JSON resources must also retain LevelNumber 3.");
				Check(normalConfig.levelName == "CHALLENGE_LEVEL_NAME_17" && difficultConfig.levelName == normalConfig.levelName, "Both difficulties must use the shared Sun Cactus title template.");
				TranslationServer.SetLocale("zh");
				string normalTitle = BuildRuntimeTitle(normalConfig);
				string difficultTitle = BuildRuntimeTitle(difficultConfig);
				Check(normalTitle.Contains("3", StringComparison.Ordinal) && !normalTitle.Contains("{LevelNumber}", StringComparison.Ordinal), "The production title formatter must resolve Normal to challenge 3; got '" + normalTitle + "'.");
				Check(difficultTitle.Contains("3", StringComparison.Ordinal) && !difficultTitle.Contains("{LevelNumber}", StringComparison.Ordinal), "The production title formatter must resolve Difficult to challenge 3; got '" + difficultTitle + "'.");
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Progress/Manager/TowerDefenseProgressManager.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The production in-game progress UI must load.");
				if (!GodotObject.IsInstanceValid(packedScene))
				{
					goto end_IL_0057;
				}
				normalProgress = packedScene.Instantiate<TowerDefenseProgressManager>(PackedScene.GenEditState.Disabled);
				difficultProgress = packedScene.Instantiate<TowerDefenseProgressManager>(PackedScene.GenEditState.Disabled);
				AddChild(normalProgress, forceReadableName: false, InternalMode.Disabled);
				AddChild(difficultProgress, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				normalProgress.SetupUI("Normal", normalTitle);
				difficultProgress.SetupUI("Difficult", difficultTitle);
				Check(normalProgress.levelNameLabel.Text == normalTitle && normalProgress.levelNameLabel.Text.Contains("3", StringComparison.Ordinal), "The real Normal progress label must display Sun Cactus challenge 3.");
				Check(difficultProgress.levelNameLabel.Text == difficultTitle && difficultProgress.levelNameLabel.Text.Contains("3", StringComparison.Ordinal), "The real Difficult progress label must display Sun Cactus challenge 3.");
				goto end_IL_004e;
				end_IL_0057:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSunCactusChallengeThreeTitleRuntimeTest] Unexpected exception: {value}");
				goto end_IL_004e;
			}
			return;
			end_IL_004e:;
		}
		finally
		{
			TranslationServer.SetLocale(previousLocale);
			if (GodotObject.IsInstanceValid(normalProgress))
			{
				normalProgress.QueueFree();
			}
			if (GodotObject.IsInstanceValid(difficultProgress))
			{
				difficultProgress.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			normalConfig?.Dispose();
			difficultConfig?.Dispose();
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"SUN_CACTUS_CHALLENGE_THREE_TITLE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private string BuildRuntimeTitle(TowerDefenseLevelConfig config)
	{
		return Tr(config.levelName).Replace("{LevelNumber}", config.levelNumber.ToString(), StringComparison.Ordinal);
	}

	private static int ReadSourceLevelNumber(TowerDefenseLevelConfig config)
	{
		if (!GodotObject.IsInstanceValid(config?.data))
		{
			return -1;
		}
		return config.data.Data.AsGodotDictionary().GetValueOrDefault("LevelNumber", -1).AsInt32();
	}

	private static string ResolveUidPath(string uid)
	{
		long num = ResourceUid.TextToId(uid);
		if (num == -1 || !ResourceUid.HasId(num))
		{
			return "";
		}
		return ResourceUid.GetIdPath(num);
	}

	private static Dictionary FindLevelEntry(Dictionary registry, string saveKey)
	{
		foreach (Variant item in registry.GetValueOrDefault("Challenge", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Chapter", new Godot.Collections.Array())
			.AsGodotArray())
		{
			foreach (Variant item2 in item.AsGodotDictionary().GetValueOrDefault("Level", new Godot.Collections.Array()).AsGodotArray())
			{
				Dictionary dictionary = item2.AsGodotDictionary();
				if (dictionary.GetValueOrDefault("SaveKey", "").AsString() == saveKey)
				{
					return dictionary;
				}
			}
		}
		return new Dictionary();
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewSunCactusChallengeThreeTitleRuntimeTest] " + message);
		}
	}
}
