using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BungiCarryVisualInspection : Node2D
{
	private const string CrewPacketPath = "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Packet/ZombieCrew.tres";

	private const string CrewScenePath = "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn";

	private const string SnorklePacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres";

	private const string SnorkleScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Base/TowerDefenseZombieSnorkle.tscn";

	private static readonly Vector2I DryGrid = new Vector2I(4, 2);

	private static readonly Vector2I WaterGrid = new Vector2I(7, 3);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private TowerDefenseManager _manager;

	private TowerDefenseControlNew _previousControl;

	private Vector2 _previousGridBegin;

	private Vector2 _previousGridSize;

	private Vector2I _previousGridNum;

	private double _previousTimeScale;

	private BungiCarryVisualInspectionControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private Node2D _characterNode;

	private TowerDefenseZombieBungiSpawn _dryCarrier;

	private TowerDefenseZombieBungiSpawn _waterCarrier;

	private Label _statusLabel;

	private Label _cycleLabel;

	private bool _initialized;

	private bool _cleanupStarted;

	private bool _pausedAtRise;

	public override async void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		_manager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(_manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			GD.PushError("[BungiCarryVisualInspection] Required autoloads are unavailable.");
			return;
		}
		RememberGlobalState();
		_previousTimeScale = Engine.TimeScale;
		Engine.TimeScale = 0.25;
		TowerDefenseProjectileRegistry.Init();
		CreateBackdrop();
		CreateHud();
		RegisterRealFixtures();
		SetupBattleFixture();
		await WaitPhysicsFrames(4);
		_initialized = true;
		SpawnPair();
	}

	public override void _Process(double delta)
	{
		if (_initialized)
		{
			_statusLabel.Text = $"timeScale={Engine.TimeScale:F2}\nDRY  {Describe(_dryCarrier)}\nWATER {Describe(_waterCarrier)}";
			if (!_pausedAtRise && IsMidRise(_dryCarrier) && IsMidRise(_waterCarrier))
			{
				_pausedAtRise = true;
				Engine.TimeScale = 0.0;
				_cycleLabel.Text += "  |  PAUSED MID-RISE - inspect rope/body, SPACE resumes";
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: not false, Echo: false, PhysicalKeycode: var physicalKeycode })
		{
			switch (physicalKeycode)
			{
			case Key.Space:
				Engine.TimeScale = ((Engine.TimeScale < 0.05) ? 0.25 : 0.0);
				GetViewport().SetInputAsHandled();
				break;
			case Key.Escape:
				GetTree().Quit();
				GetViewport().SetInputAsHandled();
				break;
			}
		}
	}

	public override void _ExitTree()
	{
		Cleanup();
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(0f, 0f, 1080f, 600f), new Color(0.025f, 0.04f, 0.025f));
		DrawRect(new Rect2(0f, 95f, 1080f, 250f), new Color(0.12f, 0.26f, 0.1f));
		DrawRect(new Rect2(0f, 345f, 1080f, 255f), new Color(0.03f, 0.2f, 0.34f));
		DrawLine(new Vector2(0f, 345f), new Vector2(1080f, 345f), new Color(0.55f, 0.85f, 1f), 3f);
		DrawLandingMarker(DryGrid, new Color(1f, 0.9f, 0.25f));
		DrawLandingMarker(WaterGrid, new Color(0.25f, 0.95f, 1f));
	}

	private void DrawLandingMarker(Vector2I grid, Color color)
	{
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(grid);
		DrawCircle(mapCellPlantPos, 36f, new Color(color, 0.15f), filled: false, 4f);
		DrawLine(mapCellPlantPos - new Vector2(45f, 0f), mapCellPlantPos + new Vector2(45f, 0f), color, 2f);
		DrawLine(mapCellPlantPos - new Vector2(0f, 45f), mapCellPlantPos + new Vector2(0f, 45f), color, 2f);
	}

	private void CreateBackdrop()
	{
		base.ZIndex = -4096;
		QueueRedraw();
	}

	private void CreateHud()
	{
		CanvasLayer canvasLayer = new CanvasLayer
		{
			Layer = 1000
		};
		AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
		Label label = new Label
		{
			Position = new Vector2(18f, 12f),
			Text = "BUNGI CARRY VISUAL INSPECTION  |  0.25x speed"
		};
		label.AddThemeFontSizeOverride("font_size", 24);
		canvasLayer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		Label label2 = new Label
		{
			Position = new Vector2(20f, 48f),
			Text = "SPACE: pause/resume   ESC: quit   Yellow=dry, Cyan=water"
		};
		label2.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
		canvasLayer.AddChild(label2, forceReadableName: false, InternalMode.Disabled);
		_cycleLabel = new Label
		{
			Position = new Vector2(20f, 76f)
		};
		_cycleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.2f));
		canvasLayer.AddChild(_cycleLabel, forceReadableName: false, InternalMode.Disabled);
		ColorRect node = new ColorRect
		{
			Position = new Vector2(612f, 92f),
			Size = new Vector2(455f, 94f),
			Color = new Color(0f, 0f, 0f, 0.72f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		canvasLayer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_statusLabel = new Label
		{
			Position = new Vector2(624f, 101f),
			Text = "waiting for real characters..."
		};
		_statusLabel.AddThemeFontSizeOverride("font_size", 16);
		canvasLayer.AddChild(_statusLabel, forceReadableName: false, InternalMode.Disabled);
	}

	private void RememberGlobalState()
	{
		_previousControl = _manager.currentControl;
		_previousGridBegin = _manager.gridBeginPos;
		_previousGridSize = _manager.gridSize;
		_previousGridNum = _manager.gridNum;
	}

	private void SetupBattleFixture()
	{
		_control = new BungiCarryVisualInspectionControlStub
		{
			Name = "BungiCarryVisualInspectionControl",
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_characterNode = new Node2D
		{
			Name = "CharacterNode",
			ZIndex = 0
		};
		_control.AddChild(_characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = _characterNode;
		_manager.currentControl = _control;
		_manager.gridBeginPos = new Vector2(20f, 100f);
		_manager.gridSize = new Vector2(120f, 160f);
		_manager.gridNum = new Vector2I(9, 3);
		_mapControl = new TowerDefenseMapControl
		{
			Name = "MapControl"
		};
		_mapFeature = CreateMapFeature(_mapControl, _manager.gridNum);
		_mapFeature.control = _control;
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
	}

	private void SpawnPair()
	{
		if (_initialized && GodotObject.IsInstanceValid(_characterNode))
		{
			_cycleLabel.Text = "single clean run: real ZombieCrew + real ZombieSnorkle";
			_dryCarrier = _manager.BungiSpawn("ZombieCrew", DryGrid) as TowerDefenseZombieBungiSpawn;
			_waterCarrier = _manager.BungiSpawn("ZombieSnorkle", WaterGrid) as TowerDefenseZombieBungiSpawn;
			if (!GodotObject.IsInstanceValid(_dryCarrier) || !GodotObject.IsInstanceValid(_waterCarrier))
			{
				GD.PushError("[BungiCarryVisualInspection] A real bungee carrier failed to spawn.");
			}
		}
	}

	private void ClearCharacters()
	{
		foreach (Node child in _characterNode.GetChildren())
		{
			if (GodotObject.IsInstanceValid(child))
			{
				child.QueueFree();
			}
		}
		_dryCarrier = null;
		_waterCarrier = null;
	}

	private static string Describe(TowerDefenseZombieBungiSpawn carrier)
	{
		if (!GodotObject.IsInstanceValid(carrier))
		{
			return "finished";
		}
		string value = carrier.CurrentStateHandle?.StableId ?? "<none>";
		TowerDefenseCharacter character = carrier.character;
		if (character != null && GodotObject.IsInstanceValid(character))
		{
			float value2 = carrier.GetLogicalGlobalPosition().DistanceTo(character.GetLogicalGlobalPosition());
			double value3 = Math.Abs(carrier.z - character.z);
			return $"state={value,-26} posDelta={value2,7:F3}  zDelta={value3,7:F3}  carrierZ={carrier.z,7:F2} payloadZ={character.z,7:F2}";
		}
		return $"state={value}  carrierZ={carrier.z,7:F2}  payload=<creating>";
	}

	private static bool IsMidRise(TowerDefenseZombieBungiSpawn carrier)
	{
		if (GodotObject.IsInstanceValid(carrier) && carrier.CurrentStateHandle?.StableId == "zombie.bungi_spawn.rise" && carrier.z >= 220.0)
		{
			return carrier.z <= 420.0;
		}
		return false;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = new Vector2(20f, 100f),
			gridSize = new Vector2(120f, 160f),
			plantOffset = 90.0
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				if (j == WaterGrid.Y)
				{
					towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.WATER,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					};
				}
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(towerDefenseCellConfig);
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("ZombieCrew", "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Packet/ZombieCrew.tres");
		RegisterCharacter("ZombieCrew", "res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn");
		RegisterPacket("ZombieSnorkle", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkle.tres");
		RegisterCharacter("ZombieSnorkle", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Base/TowerDefenseZombieSnorkle.tscn");
	}

	private void RegisterPacket(string key, string path)
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
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
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

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Cleanup()
	{
		if (!_cleanupStarted)
		{
			_cleanupStarted = true;
			_initialized = false;
			if (GodotObject.IsInstanceValid(_characterNode))
			{
				ClearCharacters();
			}
			if (GodotObject.IsInstanceValid(_manager))
			{
				_manager.currentControl = _previousControl;
				_manager.gridBeginPos = _previousGridBegin;
				_manager.gridSize = _previousGridSize;
				_manager.gridNum = _previousGridNum;
			}
			_mapFeature?.Destroy();
			RestoreRealFixtures();
			Engine.TimeScale = _previousTimeScale;
		}
	}
}
