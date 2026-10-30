using System;
using Godot;

public partial class BugOverviewGarCobCannonChallenge2EntryRuntimeTest : Node
{
	private const string LevelPath = "res://Asset/Config/Level/TowerDefense/Challenge/Gold/Challenge_Level18_2.tres";

	private const string BattleScenePath = "res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
			Check(GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "TowerDefenseManager autoload must be available.");
			Check(GodotObject.IsInstanceValid(GameSaveManager.Instance), "GameSaveManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance))
			{
				return;
			}
			GameSaveManager.Instance.EnsureLoaded();
			if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
			{
				GameSaveManager.Instance.SetUserCurrent("GarCobChallenge2Probe");
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
			Check(resourcesLoaded, "Production resources must finish loading before the level entry probe.");
			if (!resourcesLoaded)
			{
				return;
			}
			TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Challenge/Gold/Challenge_Level18_2.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig), "Gar Cob Cannon challenge 2 must load as a real level resource.");
			Check(towerDefenseLevelConfig != null && towerDefenseLevelConfig.featureData?.ContainsKey("Map") == true && towerDefenseLevelConfig.featureData["Map"].GetValueOrDefault("MapName", "").AsString() == "UndeadVillageHalfLawn", "Challenge 2 must retain its UndeadVillageHalfLawn map feature.");
			Check(towerDefenseLevelConfig != null && towerDefenseLevelConfig.featureData?.ContainsKey("BGM") == true && towerDefenseLevelConfig.featureData["BGM"].GetValueOrDefault("BackgroundMusic", "").AsString() == "FrontlawnNight", "Challenge 2 must retain its FrontlawnNight BGM feature.");
			if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
			{
				return;
			}
			TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate(), "The production battle scene must load and instantiate.");
			if (!GodotObject.IsInstanceValid(packedScene) || !packedScene.CanInstantiate())
			{
				return;
			}
			TowerDefenseControlNew battle = packedScene.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(battle), "Challenge 2 must create the real battle controller.");
			if (!GodotObject.IsInstanceValid(battle))
			{
				return;
			}
			AddChild(battle, forceReadableName: false, InternalMode.Disabled);
			for (int frame = 0; frame < 900; frame++)
			{
				if (battle.isGameRunning)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Check(battle.isGameRunning, "Challenge 2 must reach GameRunning instead of hanging during entry.");
			Check(battle.IsInsideTree() && !battle.IsQueuedForDeletion(), "Challenge 2 must remain alive after its complete entry window.");
			Check(GodotObject.IsInstanceValid(TowerDefenseManager.GetMapFeature()), "Challenge 2 must initialize a real map feature instead of a black screen.");
			void OnLoadOver()
			{
				resourcesLoaded = true;
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[GarCobCannonChallenge2Entry] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"GAR_COB_CANNON_CHALLENGE2_ENTRY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[GarCobCannonChallenge2Entry] " + message);
		}
	}
}
