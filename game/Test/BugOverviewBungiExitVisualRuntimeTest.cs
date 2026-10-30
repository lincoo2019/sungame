using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewBungiExitVisualRuntimeTest : Node
{
	private const string BungiPacketPath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres";

	private const string BungiScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	private BungiExitVisualRuntimeControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private TowerDefenseZombieBungi _bungi;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
			Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				throw new InvalidOperationException("Required autoloads are unavailable.");
			}
			RegisterRealFixtures();
			SetupBattleFixture(manager);
			await VerifyRealBungiExitVisualLifecycle();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewBungiExitVisualRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_bungi))
			{
				_bungi.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			_mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"BUNGI_EXIT_VISUAL_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void SetupBattleFixture(TowerDefenseManager manager)
	{
		_control = new BungiExitVisualRuntimeControlStub
		{
			Name = "BungiExitVisualRuntimeControl",
			isGameRunning = false,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		manager.gridBeginPos = Vector2.Zero;
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		_mapControl = new TowerDefenseMapControl
		{
			Name = "MapControl"
		};
		_mapFeature = CreateMapFeature(_mapControl, manager.gridNum);
		_mapFeature.control = _control;
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
	}

	private async Task VerifyRealBungiExitVisualLifecycle()
	{
		_bungi = (ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig)?.Plant(TestGrid, playAudio: false) as TowerDefenseZombieBungi;
		await WaitFrames(4);
		Check(GodotObject.IsInstanceValid(_bungi) && _bungi.config?.name == "ZombieBungi", "The exit regression must instantiate the real ZombieBungi packet and scene.");
		BugOverviewBungiExitVisualRuntimeTest bugOverviewBungiExitVisualRuntimeTest = this;
		TowerDefenseZombieBungi bungi = _bungi;
		bugOverviewBungiExitVisualRuntimeTest.Check(bungi != null && bungi.StateMachine?.IsInitialized == true, "The real Bungi state machine must initialize before the rise transition is exercised.");
		if (!GodotObject.IsInstanceValid(_bungi))
		{
			throw new InvalidOperationException("The real Bungi did not spawn.");
		}
		Sprite2D bungeeTarget = _bungi.GetNodeOrNull<Sprite2D>("%BungeeTarget");
		Check(GodotObject.IsInstanceValid(bungeeTarget), "The real Bungi scene must expose its authored BungeeTarget sprite.");
		if (!GodotObject.IsInstanceValid(bungeeTarget))
		{
			throw new InvalidOperationException("The real BungeeTarget sprite is unavailable.");
		}
		_bungi.z = 0.0;
		_bungi.groundHeight = 0.0;
		_bungi.isGround = true;
		_bungi.canBlock = true;
		_bungi.skipBungeeTarget = false;
		bungeeTarget.Visible = true;
		bungeeTarget.Position = new Vector2(0f, 15f);
		Check(Mathf.IsZeroApprox((float)_bungi.z) && bungeeTarget.Visible && Mathf.IsEqualApprox(bungeeTarget.Position.Y, 15f), "The departure fixture must start the Bungi body and target at the landed pose.");
		_bungi.SendStateEvent("ToRise");
		await WaitSeconds(0.65);
		bool flag = GodotObject.IsInstanceValid(_bungi) && !_bungi.IsQueuedForDeletion();
		bool flag2 = GodotObject.IsInstanceValid(bungeeTarget);
		Check(flag && flag2, "The Bungi body and target must both remain alive during the rise tween.");
		double num = (flag ? _bungi.z : (0.0 / 0.0));
		float num2 = (flag2 ? bungeeTarget.Position.Y : (0f / 0f));
		Check(num > 1.0 && num < 599.0, $"The Bungi body must be between landed and exited heights mid-rise; z={num}.");
		Check(num2 < 14f && num2 > -584f, $"BungeeTarget must be between landed and exited positions mid-rise; y={num2}.");
		Check(Mathf.Abs((float)num - (15f - num2)) <= 3f, $"BungeeTarget and body must leave in lockstep; z={num}, targetY={num2}.");
		Check(flag && GodotObject.IsInstanceValid(_bungi.spriteGroup) && Mathf.Abs(_bungi.spriteGroup.Position.Y + (float)num) <= 1f, "The real SpriteGroup must visually follow the tweened Bungi height.");
		await WaitSeconds(1.05);
		await WaitFrames(3);
		bool condition = !GodotObject.IsInstanceValid(_bungi) || _bungi.IsQueuedForDeletion();
		Check(condition, "The real Bungi node must be released after the 1.5-second rise finishes.");
		Check(!ContainsBungi(_control.characterNode), "CharacterNode must not retain a Bungi body after the completed exit.");
		Check(!ContainsNodeNamed(_control.characterNode, new StringName("BungeeTarget")), "CharacterNode must not retain the BungeeTarget sprite after the completed exit.");
		await WaitSeconds(1.0);
		await WaitFrames(2);
		Check(!ContainsBungi(_control.characterNode) && !ContainsNodeNamed(_control.characterNode, new StringName("BungeeTarget")), "No body or target texture may reappear as a seconds-long residual after exit.");
	}

	private static bool ContainsBungi(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		foreach (Node child in root.GetChildren())
		{
			if (child is TowerDefenseZombieBungi && GodotObject.IsInstanceValid(child))
			{
				return true;
			}
			if (ContainsBungi(child))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsNodeNamed(Node root, StringName name)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		foreach (Node child in root.GetChildren())
		{
			if (GodotObject.IsInstanceValid(child) && child.Name == name)
			{
				return true;
			}
			if (ContainsNodeNamed(child, name))
			{
				return true;
			}
		}
		return false;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
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
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
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
		RegisterPacket("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres");
		RegisterCharacter("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
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

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
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
			GD.PushError("[BugOverviewBungiExitVisualRuntimeTest] " + message);
		}
	}
}
