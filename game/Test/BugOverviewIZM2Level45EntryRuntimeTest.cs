using System;
using Godot;

public partial class BugOverviewIZM2Level45EntryRuntimeTest : Node
{
	private const string LevelPath = "res://Asset/Config/Level/TowerDefense/IZM2/Chapter4/IZM2_Level4_5.tres";

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
			Check(GodotObject.IsInstanceValid(Global.Instance), "Global autoload must be available.");
			if (!GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance) || !GodotObject.IsInstanceValid(Global.Instance))
			{
				return;
			}
			GameSaveManager.Instance.EnsureLoaded();
			if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
			{
				GameSaveManager.Instance.SetUserCurrent("IZM2Level45EntryProbe");
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
			Check(resourcesLoaded, "Production resources must finish loading before entering 4-5.");
			if (!resourcesLoaded)
			{
				return;
			}
			TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/IZM2/Chapter4/IZM2_Level4_5.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig), "IZM2 4-5 normal mode must load as a real level resource.");
			Check(towerDefenseLevelConfig?.name == "IZM2_Level4_5", "The loaded level must be IZM2_Level4_5; actual=" + towerDefenseLevelConfig?.name + ".");
			if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
			{
				return;
			}
			Global.Instance.enterLevelMode = "LevelChoose";
			Global.Instance.currentLevelId = 4;
			TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate(), "The production battle scene must load and instantiate.");
			if (!GodotObject.IsInstanceValid(packedScene) || !packedScene.CanInstantiate())
			{
				return;
			}
			TowerDefenseControlNew battle = packedScene.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(battle), "IZM2 4-5 must create the real battle controller.");
			if (!GodotObject.IsInstanceValid(battle))
			{
				return;
			}
			AddChild(battle, forceReadableName: false, InternalMode.Disabled);
			for (int frame = 0; frame < 1500; frame++)
			{
				if (battle.isGameRunning)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Check(battle.isGameRunning, "IZM2 4-5 must reach GameRunning instead of remaining on a black screen.");
			Check(battle.IsInsideTree() && !battle.IsQueuedForDeletion(), "IZM2 4-5 battle controller must remain alive after entry.");
			Check(battle.process is TowerDefenseBattleProcessIZM2, "IZM2 4-5 must create the IZM2 process; actual=" + battle.process?.GetType().Name + ".");
			Check(GodotObject.IsInstanceValid(TowerDefenseManager.GetMapFeature()), "IZM2 4-5 must initialize the real map feature.");
			Check(battle.featureDictionary.ContainsKey("ConveyorBelt"), "IZM2 4-5 must initialize its production ConveyorBelt feature.");
			Check(GodotObject.IsInstanceValid(battle.levelControl), "IZM2 4-5 must initialize the production level presentation.");
			void OnLoadOver()
			{
				resourcesLoaded = true;
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[IZM2Level45Entry] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"IZM2_LEVEL45_ENTRY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[IZM2Level45Entry] " + message);
		}
	}
}
