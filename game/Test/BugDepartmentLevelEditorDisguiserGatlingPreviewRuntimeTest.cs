using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentLevelEditorDisguiserGatlingPreviewRuntimeTest : Node
{
	private const string EditorScenePath = "res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn";

	private const string MapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private const string DisguiserPacketPath = "res://Asset/Anime/Character/Plant/Star/DisguiserGatling/Packet/PlantDisguiserGatling.tres";

	private const string DisguiserScenePath = "res://Asset/Anime/Character/Plant/Star/DisguiserGatling/Scene/TowerDefensePlantDisguiserGatling.tscn";

	private const string GatlingItemPacketPath = "res://Asset/Anime/Character/Item/GatlingTX/Packet/ItemGatlingTX.tres";

	private const string GatlingItemScenePath = "res://Asset/Anime/Character/Item/GatlingTX/Scene/TowerDefenseItemGatlingTX.tscn";

	private static readonly Vector2I PreviewGrid = new Vector2I(4, 2);

	private static readonly Vector2I BattleGrid = new Vector2I(5, 2);

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly HashSet<ulong> _observedDisguisers = new HashSet<ulong>();

	private int _previewComponentObservers;

	private int _previewMainComponentEntries;

	private int _battleComponentObservers;

	private int _battleMainComponentEntries;

	private int _componentProbeAttachmentFailures;

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
		LevelEditorDisguiserPreviewControlStub control = null;
		LevelEditorMapEditor editor = null;
		TowerDefenseMapConfig mapConfig = null;
		TowerDefensePlantDisguiserGatling battlePlant = null;
		int gatlingItemEntries = 0;
		int realGatlingItemEntries = 0;
		int previewItemEntriesBeforeBattle = -1;
		int battleDestroyItemEntries = -1;
		int battleDestroyRealItemEntries = -1;
		bool previewComponentRunningObserved = false;
		try
		{
			_ = 8;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(Global.Instance) && GodotObject.IsInstanceValid(SceneManager.Instance), "Required gameplay and editor autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(SceneManager.Instance))
				{
					goto end_IL_0173;
				}
				RegisterRealFixtures();
				Global.Instance.isEditor = true;
				SceneManager.Instance.currentScene = "LevelEditorStage";
				editor = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<LevelEditorMapEditor>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(editor) && !editor.isolatedPreviewMode && Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage", "The scenario must instantiate the real visibility-driven LevelEditorMapEditor in LevelEditorStage.");
				if (!GodotObject.IsInstanceValid(editor))
				{
					goto end_IL_0173;
				}
				TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig();
				towerDefenseLevelConfig.preSpawnList.Add(new TowerDefenseLevelPreSpawnConfig
				{
					packetName = "PlantDisguiserGatling",
					gridPos = PreviewGrid
				});
				editor.levelConfig = towerDefenseLevelConfig;
				control = new LevelEditorDisguiserPreviewControlStub
				{
					Name = "LevelEditorDisguiserPreviewControl",
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
				control.characterNode.ChildEnteredTree += delegate(Node child)
				{
					if (child is TowerDefenseCharacter towerDefenseCharacter)
					{
						if (towerDefenseCharacter is TowerDefensePlantDisguiserGatling disguiser)
						{
							ObserveMainComponentEntries(disguiser);
						}
						if (!(towerDefenseCharacter.config?.name != "ItemGatlingTX"))
						{
							gatlingItemEntries++;
							if (towerDefenseCharacter is TowerDefenseItemGatlingTX)
							{
								realGatlingItemEntries++;
							}
							towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
						}
					}
				};
				AddChild(editor, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				mapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(mapConfig) && mapFeature.MapInit(mapConfig), "The real Frontlawn map must initialize in the LevelEditorMapEditor.");
				if (!GodotObject.IsInstanceValid(mapConfig) || !GodotObject.IsInstanceValid(mapFeature.config))
				{
					goto end_IL_0173;
				}
				manager.gridBeginPos = mapFeature.config.gridBeginPos;
				manager.gridSize = mapFeature.config.gridSize;
				manager.gridNum = mapFeature.config.gridNum;
				editor.Save(isSave: false);
				await WaitFrames(5);
				TowerDefensePlantDisguiserGatling towerDefensePlantDisguiserGatling = FindDisguiser(editor.characterNode);
				previewComponentRunningObserved |= GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling) && towerDefensePlantDisguiserGatling.componentRunning;
				Check(GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling), "The first visible editor pass must create the real pre-placed Disguiser Gatling.");
				Check(GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling) && !towerDefensePlantDisguiserGatling.inGame && towerDefensePlantDisguiserGatling.editorPreviewMode && !towerDefensePlantDisguiserGatling.componentRunning, "The preview must enter the tree already excluded from gameplay Component state.");
				Check(GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling) && towerDefensePlantDisguiserGatling.CurrentStateHandle?.StableId == "plant.plant", "The preview must remain in the authored Plant state rather than enter Component.");
				Check(_previewComponentObservers == 1 && _componentProbeAttachmentFailures == 0 && _previewMainComponentEntries == 0, "The initial preview must have an active main-state probe and enter Component exactly zero times.");
				Check(gatlingItemEntries == 0, "Initial preview creation must not generate GatlingTX support attackers.");
				Global.Instance.isEditor = false;
				SceneManager.Instance.currentScene = "";
				for (int cycle = 1; cycle <= 3; cycle++)
				{
					editor.Visible = false;
					await WaitFrames(5);
					Check(FindDisguiser(editor.characterNode) == null, $"Hidden editor cycle {cycle} must clear its preview character.");
					Check(gatlingItemEntries == 0, $"Hidden editor cycle {cycle} must not run DestroySet or create GatlingTX.");
					Check(_previewMainComponentEntries == 0, $"Hidden editor cycle {cycle} must preserve exactly zero preview Component entries across all prior frames.");
					editor.Visible = true;
					await WaitFrames(5);
					towerDefensePlantDisguiserGatling = FindDisguiser(editor.characterNode);
					previewComponentRunningObserved |= GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling) && towerDefensePlantDisguiserGatling.componentRunning;
					Check(GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling) && !towerDefensePlantDisguiserGatling.inGame && towerDefensePlantDisguiserGatling.editorPreviewMode && !towerDefensePlantDisguiserGatling.componentRunning, $"Visible editor cycle {cycle} must recreate a passive real preview.");
					Check(GodotObject.IsInstanceValid(towerDefensePlantDisguiserGatling) && towerDefensePlantDisguiserGatling.CurrentStateHandle?.StableId == "plant.plant", $"Visible editor cycle {cycle} must remain in the authored Plant state.");
					Check(_previewComponentObservers == cycle + 1 && _componentProbeAttachmentFailures == 0 && _previewMainComponentEntries == 0, $"Visible editor cycle {cycle} must attach its probe before state entry and keep the lifetime Component-entry total at zero.");
				}
				editor.ClearCharacter();
				await WaitFrames(5);
				previewItemEntriesBeforeBattle = gatlingItemEntries;
				Check(gatlingItemEntries == 0 && FindDisguiser(editor.characterNode) == null, "Explicit LevelEditorMapEditor cleanup must remain side-effect free.");
				Check(_previewComponentObservers == 4 && _previewMainComponentEntries == 0 && _componentProbeAttachmentFailures == 0, "All four real editor preview instances must be observed for their full lifetime with exactly zero Component entries.");
				battlePlant = LoadPacket("res://Asset/Anime/Character/Plant/Star/DisguiserGatling/Packet/PlantDisguiserGatling.tres")?.Plant(BattleGrid, playAudio: false, noLimit: true) as TowerDefensePlantDisguiserGatling;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(battlePlant) && battlePlant.inGame && !battlePlant.editorPreviewMode && !battlePlant.skipDestroySet, "The normal packet path must still create a real gameplay Disguiser Gatling.");
				Check(GodotObject.IsInstanceValid(battlePlant) && Math.Abs(battlePlant.hpNextInterval - battlePlant.instance.hitpoints / 6.0) < 0.001, "The real gameplay plant must retain its authored six-support death schedule.");
				if (!GodotObject.IsInstanceValid(battlePlant))
				{
					goto end_IL_0173;
				}
				Check(_battleComponentObservers == 1 && _battleMainComponentEntries == 0 && _componentProbeAttachmentFailures == 0, "The gameplay control must attach the same Component-entry probe before the comparison transition.");
				battlePlant.Component();
				await WaitFrames(1);
				Check(_battleMainComponentEntries == 1 && battlePlant.componentRunning && battlePlant.CurrentStateHandle?.StableId == "character.component", "The real gameplay comparison must record exactly one main Component entry when explicitly requested.");
				battlePlant.Idle();
				await WaitFrames(1);
				int entriesBeforeBattleDestroy = gatlingItemEntries;
				int realEntriesBeforeBattleDestroy = realGatlingItemEntries;
				battlePlant.Destroy();
				await WaitFrames(5);
				battleDestroyItemEntries = gatlingItemEntries - entriesBeforeBattleDestroy;
				battleDestroyRealItemEntries = realGatlingItemEntries - realEntriesBeforeBattleDestroy;
				Check(battlePlant.over && battlePlant.hpNext < 0.0, "Normal battle destruction must execute the real Disguiser DestroySet.");
				Check(battleDestroyItemEntries == 6 && battleDestroyRealItemEntries == 6, "Normal battle DestroySet must still create exactly six real ItemGatlingTX attackers.");
				goto end_IL_0144;
				end_IL_0173:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentLevelEditorDisguiserGatlingPreviewRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0144;
			}
			return;
			end_IL_0144:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(battlePlant) && !battlePlant.IsQueuedForDeletion())
			{
				battlePlant.QueueFree();
			}
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
			RestoreRealFixtures();
			Global.Instance.isEditor = previousEditor;
			SceneManager.Instance.currentScene = previousScene;
			mapConfig?.Dispose();
			await WaitFrames(5);
		}
		bool flag = _failures == 0 && _checks == 34;
		GD.Print("LEVEL_EDITOR_DISGUISER_GATLING_PREVIEW_DIAGNOSTICS " + $"previewComponentRunningObserved={previewComponentRunningObserved} " + $"previewComponentObservers={_previewComponentObservers} " + $"previewMainComponentEntries={_previewMainComponentEntries} " + $"battleComponentObservers={_battleComponentObservers} " + $"battleMainComponentEntries={_battleMainComponentEntries} " + $"componentProbeAttachmentFailures={_componentProbeAttachmentFailures} " + $"previewItemsBeforeBattle={previewItemEntriesBeforeBattle} " + $"battleDestroyItems={battleDestroyItemEntries} " + $"battleDestroyRealItems={battleDestroyRealItemEntries}");
		GD.Print($"LEVEL_EDITOR_DISGUISER_GATLING_PREVIEW_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void ObserveMainComponentEntries(TowerDefensePlantDisguiserGatling disguiser)
	{
		if (GodotObject.IsInstanceValid(disguiser) && _observedDisguisers.Add(disguiser.GetInstanceId()))
		{
			if (disguiser.IsNodeReady())
			{
				AttachProbe();
			}
			else
			{
				disguiser.Ready += AttachProbe;
			}
		}
		void AttachProbe()
		{
			StateHandle stateById = disguiser.GetStateById("character.component");
			if (stateById == null)
			{
				_componentProbeAttachmentFailures++;
			}
			else if (disguiser.editorPreviewMode)
			{
				_previewComponentObservers++;
				stateById.Entered += delegate
				{
					_previewMainComponentEntries++;
				};
			}
			else
			{
				_battleComponentObservers++;
				stateById.Entered += delegate
				{
					_battleMainComponentEntries++;
				};
			}
		}
	}

	private static TowerDefensePlantDisguiserGatling FindDisguiser(Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefensePlantDisguiserGatling towerDefensePlantDisguiserGatling && !towerDefensePlantDisguiserGatling.IsQueuedForDeletion())
			{
				return towerDefensePlantDisguiserGatling;
			}
		}
		return null;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		Register("PlantDisguiserGatling", "res://Asset/Anime/Character/Plant/Star/DisguiserGatling/Packet/PlantDisguiserGatling.tres", "res://Asset/Anime/Character/Plant/Star/DisguiserGatling/Scene/TowerDefensePlantDisguiserGatling.tscn");
		Register("ItemGatlingTX", "res://Asset/Anime/Character/Item/GatlingTX/Packet/ItemGatlingTX.tres", "res://Asset/Anime/Character/Item/GatlingTX/Scene/TowerDefenseItemGatlingTX.tscn");
	}

	private void Register(string key, string packetPath, string scenePath)
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

	private void RestoreRealFixtures()
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
			GD.PushError("[BugDepartmentLevelEditorDisguiserGatlingPreviewRuntimeTest] " + message);
		}
	}
}
