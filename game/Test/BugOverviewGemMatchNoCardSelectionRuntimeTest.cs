using System;
using Godot;

public partial class BugOverviewGemMatchNoCardSelectionRuntimeTest : Node
{
	private static readonly string[] LevelPaths = new string[4] { "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_1.tres", "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_1_D.tres", "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres", "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2_D.tres" };

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseLevelBaseConfig currentLevelConfig = instance?.currentLevelConfig;
		try
		{
			Check(GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(instance))
			{
				return;
			}
			string[] levelPaths = LevelPaths;
			foreach (string levelPath in levelPaths)
			{
				VerifyLevel(instance, levelPath);
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewGemMatchNoCardSelectionRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.currentLevelConfig = currentLevelConfig;
			}
		}
		bool flag = _failures == 0 && _checks == 29;
		GD.Print($"GEM_MATCH_NO_CARD_SELECTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyLevel(TowerDefenseManager manager, string levelPath)
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(levelPath, null, ResourceLoader.CacheMode.IgnoreDeep);
		Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig), levelPath + " must load as a TowerDefenseLevelConfig.");
		if (GodotObject.IsInstanceValid(towerDefenseLevelConfig))
		{
			Check(towerDefenseLevelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE, levelPath + " must persist the cardless SeedBank enum.");
			towerDefenseLevelConfig.Init();
			Check(towerDefenseLevelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE, levelPath + " must parse its authored SeedBank method as NOONE.");
			Check(towerDefenseLevelConfig.featureData.ContainsKey(new StringName("GemMatch")), levelPath + " must remain a GemMatch level.");
			Check(!towerDefenseLevelConfig.featureData.ContainsKey(new StringName("PacketBank")), levelPath + " must not configure the interactive card picker.");
			Check(towerDefenseLevelConfig.featureData.TryGetValue(new StringName("SeedBank"), out var value) && value.GetValueOrDefault("Method", "").AsString() == "NOONE", levelPath + " must expose NOONE to the runtime SeedBank consumer.");
			manager.currentLevelConfig = towerDefenseLevelConfig;
			TowerDefenseLevelSeedBankConfig towerDefenseLevelSeedBankConfig = new TowerDefenseLevelSeedBankConfig();
			towerDefenseLevelSeedBankConfig.Init(value);
			Check(towerDefenseLevelSeedBankConfig.method == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE && towerDefenseLevelSeedBankConfig.packetList.Count == 0, levelPath + " must resolve to a cardless runtime SeedBank.");
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewGemMatchNoCardSelectionRuntimeTest] " + message);
		}
	}
}
