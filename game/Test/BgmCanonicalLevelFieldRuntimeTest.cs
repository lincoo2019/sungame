using Godot;
using Godot.Collections;

public partial class BgmCanonicalLevelFieldRuntimeTest : Node
{
	private static readonly string[] GemMatchLevelPaths = new string[4] { "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_1.tres", "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_1_D.tres", "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres", "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2_D.tres" };

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		Check(GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "TowerDefenseManager autoload must be available.");
		Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
		if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			GetTree().Quit(2);
			return;
		}
		TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig = ResourceLoader.Load<TowerDefenseBackgroundMusicConfig>("res://Asset/Config/BGM/Config/TowerDefenseFrontlawnNight.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
		Check(GodotObject.IsInstanceValid(towerDefenseBackgroundMusicConfig), "FrontlawnNight BGM configuration must load.");
		ResourceManager.Instance.BGMS["FrontlawnNight"] = towerDefenseBackgroundMusicConfig;
		string[] gemMatchLevelPaths = GemMatchLevelPaths;
		foreach (string levelPath in gemMatchLevelPaths)
		{
			VerifyGemMatchLevel(levelPath);
		}
		VerifyHistoricalFeatureField();
		VerifyHistoricalOnlineLevel();
		bool flag = _failures == 0 && _checks == 32;
		GD.Print($"BGM_CANONICAL_LEVEL_FIELD_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyGemMatchLevel(string levelPath)
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(levelPath, null, ResourceLoader.CacheMode.IgnoreDeep);
		Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig), levelPath + " must load as TowerDefenseLevelConfig.");
		if (GodotObject.IsInstanceValid(towerDefenseLevelConfig))
		{
			towerDefenseLevelConfig.Init();
			Dictionary value;
			bool flag = towerDefenseLevelConfig.featureData.TryGetValue(new StringName("BGM"), out value);
			Check(flag, levelPath + " must contain a BGM feature.");
			if (flag)
			{
				Check(value.GetValueOrDefault("BackgroundMusic", "").AsString() == "FrontlawnNight", levelPath + " must use FrontlawnNight through BackgroundMusic.");
				Check(!value.ContainsKey("BGMName"), levelPath + " must not retain the historical BGM field.");
				TowerDefenseBattleFeatureBGM towerDefenseBattleFeatureBGM = new TowerDefenseBattleFeatureBGM();
				towerDefenseBattleFeatureBGM.Init(value);
				Check(GodotObject.IsInstanceValid(towerDefenseBattleFeatureBGM.backgroundMusicConfig), levelPath + " must resolve its BGM resource.");
				Check(towerDefenseBattleFeatureBGM.backgroundMusicConfig?.flag1 == "Moongrains", levelPath + " must resolve FrontlawnNight to Moongrains.");
				towerDefenseBattleFeatureBGM.Destroy();
			}
		}
	}

	private void VerifyHistoricalFeatureField()
	{
		Dictionary data = new Dictionary { ["BGMName"] = "FrontlawnNight" };
		TowerDefenseBattleFeatureBGM towerDefenseBattleFeatureBGM = new TowerDefenseBattleFeatureBGM();
		towerDefenseBattleFeatureBGM.Init(data);
		Check(GodotObject.IsInstanceValid(towerDefenseBattleFeatureBGM.backgroundMusicConfig), "Historical online BGMName must remain readable.");
		Check(towerDefenseBattleFeatureBGM.backgroundMusicConfig?.flag1 == "Moongrains", "Historical online BGMName must resolve to Moongrains.");
		towerDefenseBattleFeatureBGM.Destroy();
	}

	private void VerifyHistoricalOnlineLevel()
	{
		Json json = ResourceLoader.Load<Json>("res://Core/Save/OnlineLevel/OnlineLevel-4.json", null, ResourceLoader.CacheMode.IgnoreDeep);
		Check(GodotObject.IsInstanceValid(json), "Historical online level JSON must load.");
		Dictionary value;
		bool flag = new TowerDefenseLevelConfig
		{
			data = json
		}.featureData.TryGetValue(new StringName("BGM"), out value);
		Check(flag, "Historical top-level online BGM must create a BGM feature.");
		Check(flag && value.GetValueOrDefault("BackgroundMusic", "").AsString() == "Frontlawn", "Historical top-level online BGM must normalize to BackgroundMusic.");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BgmCanonicalLevelFieldRuntimeTest] " + message);
		}
	}
}
