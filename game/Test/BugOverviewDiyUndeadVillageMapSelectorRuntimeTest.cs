using System;
using Godot;

public partial class BugOverviewDiyUndeadVillageMapSelectorRuntimeTest : Node
{
	private static readonly string[] MapKeys = new string[4] { "UndeadVillageLawn", "UndeadVillageHalfLawn", "UndeadVillageEmpty", "UndeadVillageBroken" };

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		LevelEditorInformationEditor editor = null;
		try
		{
			Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				return;
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
			Check(resourcesLoaded, "Production resources must finish loading.");
			if (!resourcesLoaded)
			{
				return;
			}
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/InformationEditor/LevelEditorInformationEditor.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate(), "The production information editor must load and instantiate.");
			if (!GodotObject.IsInstanceValid(packedScene) || !packedScene.CanInstantiate())
			{
				return;
			}
			editor = packedScene.Instantiate<LevelEditorInformationEditor>(PackedScene.GenEditState.Disabled);
			AddChild(editor, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			OptionButton node = editor.GetNode<OptionButton>("%MapOptionButton");
			string[] mapKeys = MapKeys;
			foreach (string text in mapKeys)
			{
				Check(ResourceManager.Instance.MAPS.TryGetValue(text, out var value) && value is TowerDefenseMapConfig, text + " must load as a real map config.");
				if (!(value is TowerDefenseMapConfig towerDefenseMapConfig))
				{
					continue;
				}
				Check(editor.mapDictionary.ContainsKey(towerDefenseMapConfig.translate) && editor.mapDictionary[towerDefenseMapConfig.translate].AsString() == text, "The production selector must contain " + text + ".");
				bool condition = false;
				for (int j = 0; j < node.ItemCount; j++)
				{
					if (node.GetItemText(j) == towerDefenseMapConfig.translate)
					{
						condition = true;
						break;
					}
				}
				Check(condition, "The visible option list must contain " + towerDefenseMapConfig.translate + ".");
			}
			void OnLoadOver()
			{
				resourcesLoaded = true;
			}
		}
		catch (Exception value2)
		{
			_failures++;
			GD.PushError($"[DiyUndeadVillageMapSelector] Unexpected exception: {value2}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(editor))
			{
				editor.QueueFree();
			}
			bool flag = _failures == 0 && _checks == 15;
			GD.Print($"DIY_UNDEAD_VILLAGE_MAP_SELECTOR_RESULT passed={flag} checks={_checks} failures={_failures}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[DiyUndeadVillageMapSelector] " + message);
		}
	}
}
