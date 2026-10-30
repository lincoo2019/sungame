using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentLevelEditorPumpkinLayeringRuntimeTest : Node
{
	private const string EditorScenePath = "res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn";

	private const string MapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private static readonly (string Key, string PacketPath, string ScenePath, Vector2I Grid)[] Fixtures = new(string, string, string, Vector2I)[5]
	{
		("PlantPumpkin", "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Packet/PlantPumpkin.tres", "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Scene/TowerDefensePlantPumpkin.tscn", new Vector2I(2, 2)),
		("PlantPumpkinFire", "res://Asset/Anime/Character/Plant/Chapter3/PumpkinFire/Packet/PlantPumpkinFire.tres", "res://Asset/Anime/Character/Plant/Chapter3/PumpkinFire/Scene/TowerDefensePlantPumpkinFire.tscn", new Vector2I(3, 2)),
		("PlantPumpkinFireIce", "res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Packet/PlantPumpkinFireIce.tres", "res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn", new Vector2I(4, 2)),
		("PlantPumpkinFort", "res://Asset/Anime/Character/Plant/Cover/PumpkinFort/Packet/PlantPumpkinFort.tres", "res://Asset/Anime/Character/Plant/Cover/PumpkinFort/Scene/TowerDefensePlantPumpkinFort.tscn", new Vector2I(5, 2)),
		("PlantPumpkinPea", "res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Packet/PlantPumpkinPea.tres", "res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Scene/TowerDefensePlantPumpkinPea.tscn", new Vector2I(6, 2))
	};

	private const string SunflowerKey = "PlantSunFlower";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

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
		TowerDefenseLevelBaseConfig previousLevelConfig = manager?.currentLevelConfig;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousEditor = Global.IsEditor;
		string previousScene = SceneManager.CurrentScene;
		LevelEditorPumpkinLayeringControlStub control = null;
		LevelEditorMapEditor editor = null;
		TowerDefenseMapConfig mapConfig = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(Global.Instance) && GodotObject.IsInstanceValid(SceneManager.Instance), "Required gameplay and editor autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(SceneManager.Instance))
				{
					goto end_IL_00fe;
				}
				RegisterFixture("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
				(string, string, string, Vector2I)[] fixtures = Fixtures;
				for (int i = 0; i < fixtures.Length; i++)
				{
					(string, string, string, Vector2I) tuple = fixtures[i];
					RegisterFixture(tuple.Item1, tuple.Item2, tuple.Item3);
				}
				Global.Instance.isEditor = true;
				SceneManager.Instance.currentScene = "LevelEditorStage";
				editor = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<LevelEditorMapEditor>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(editor) && Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage", "The scenario must instantiate the real LevelEditorMapEditor in LevelEditorStage.");
				if (!GodotObject.IsInstanceValid(editor))
				{
					goto end_IL_00fe;
				}
				TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig();
				fixtures = Fixtures;
				for (int i = 0; i < fixtures.Length; i++)
				{
					(string, string, string, Vector2I) tuple2 = fixtures[i];
					towerDefenseLevelConfig.preSpawnList.Add(new TowerDefenseLevelPreSpawnConfig
					{
						packetName = "PlantSunFlower",
						gridPos = tuple2.Item4
					});
					Array<TowerDefenseLevelPreSpawnConfig> preSpawnList = towerDefenseLevelConfig.preSpawnList;
					TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig();
					(towerDefenseLevelPreSpawnConfig.packetName, _, _, towerDefenseLevelPreSpawnConfig.gridPos) = tuple2;
					preSpawnList.Add(towerDefenseLevelPreSpawnConfig);
				}
				editor.levelConfig = towerDefenseLevelConfig;
				control = new LevelEditorPumpkinLayeringControlStub
				{
					Name = "LevelEditorPumpkinLayeringControl",
					isInit = true,
					isGameRunning = true,
					levelConfig = towerDefenseLevelConfig
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = editor.GetNode<Node2D>("%CharacterNode");
				TowerDefenseMapControl node = editor.GetNode<TowerDefenseMapControl>("%TowerDefenseMapControl");
				TowerDefenseBattleFeatureMap mapFeature = (node.mapFeature = new TowerDefenseBattleFeatureMap
				{
					mapControl = node,
					control = control
				});
				control.featureDictionary[new StringName("Map")] = mapFeature;
				manager.currentControl = control;
				manager.currentLevelConfig = towerDefenseLevelConfig;
				AddChild(editor, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				mapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(mapConfig) && mapFeature.MapInit(mapConfig), "The real Frontlawn map must initialize in the LevelEditorMapEditor.");
				if (!GodotObject.IsInstanceValid(mapConfig) || !GodotObject.IsInstanceValid(mapFeature.config))
				{
					goto end_IL_00fe;
				}
				manager.gridBeginPos = mapFeature.config.gridBeginPos;
				manager.gridSize = mapFeature.config.gridSize;
				manager.gridNum = mapFeature.config.gridNum;
				editor.Save(isSave: false);
				await WaitFrames(6);
				fixtures = Fixtures;
				for (int i = 0; i < fixtures.Length; i++)
				{
					(string, string, string, Vector2I) tuple4 = fixtures[i];
					CheckLayering(editor.characterNode, tuple4.Item1, tuple4.Item4);
				}
				goto end_IL_00ec;
				end_IL_00fe:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentLevelEditorPumpkinLayeringRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00ec;
			}
			return;
			end_IL_00ec:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(editor))
			{
				foreach (Node child in editor.characterNode.GetChildren())
				{
					if (child is TowerDefenseCharacter && !child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
				editor.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.currentLevelConfig = previousLevelConfig;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreFixtures();
			Global.Instance.isEditor = previousEditor;
			SceneManager.Instance.currentScene = previousScene;
			mapConfig?.Dispose();
			await WaitFrames(5);
		}
		bool flag = _failures == 0 && _checks == 28;
		GD.Print($"LEVEL_EDITOR_PUMPKIN_LAYERING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void CheckLayering(Node parent, string coverKey, Vector2I grid)
	{
		TowerDefenseCharacter towerDefenseCharacter = FindCharacter(parent, "PlantSunFlower", grid);
		TowerDefenseCharacter towerDefenseCharacter2 = FindCharacter(parent, coverKey, grid);
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter), $"{coverKey} must retain the inner Sunflower at {grid}.");
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter2), $"The LevelEditor must create the real {coverKey} preview at {grid}.");
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter2) && !towerDefenseCharacter2.inGame && towerDefenseCharacter2.editorPreviewMode, coverKey + " must remain a passive editor preview.");
		Node2D node2D = towerDefenseCharacter2?.sprite?.GetNodeOrNull<Node2D>("Node2D/Back");
		Check(GodotObject.IsInstanceValid(node2D) && node2D.ZAsRelative && node2D.ZIndex == -2, $"{coverKey} Back must preserve its authored relative ZIndex -2 in LevelEditor; got valid={GodotObject.IsInstanceValid(node2D)}, relative={node2D?.ZAsRelative}, z={node2D?.ZIndex}.");
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter2) && GodotObject.IsInstanceValid(node2D) && towerDefenseCharacter2.ZIndex + node2D.ZIndex < towerDefenseCharacter.ZIndex && towerDefenseCharacter2.ZIndex > towerDefenseCharacter.ZIndex, $"{coverKey} must render Back behind and Front ahead of the inner plant; back={towerDefenseCharacter2?.ZIndex + node2D?.ZIndex}, inner={towerDefenseCharacter?.ZIndex}, front={towerDefenseCharacter2?.ZIndex}.");
	}

	private static TowerDefenseCharacter FindCharacter(Node parent, string configName, Vector2I grid)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter && !towerDefenseCharacter.IsQueuedForDeletion() && towerDefenseCharacter.config?.name == configName && towerDefenseCharacter.gridPos == grid)
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private void RegisterFixture(string key, string packetPath, string scenePath)
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
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value2))
		{
			_previousCharacters[key] = value2;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(packetPath, null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreFixtures()
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
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugDepartmentLevelEditorPumpkinLayeringRuntimeTest] " + message);
		}
	}
}
