using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGloomMagneticLilyPadRuntimeTest : Node
{
	private const string LilyPadPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres";

	private const string LilyPadScenePath = "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Scene/TowerDefensePlantLilyPad.tscn";

	private const string GloomPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballGloomShroom.tres";

	private const string GloomScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/GloomShroom/TowerDefenseZombieFootballGloomShroom.tscn";

	private const string MagneticPacketPath = "res://Asset/Anime/Character/Zombie/Chapter4/ZombieMagnetic/Packet/ZombieMagnetic.tres";

	private const string MagneticScenePath = "res://Asset/Anime/Character/Zombie/Chapter4/ZombieMagnetic/Scene/TowerDefenseZombieMagnetic.tscn";

	private static readonly Vector2I GloomGrid = new Vector2I(4, 2);

	private static readonly Vector2I MagneticGrid = new Vector2I(4, 3);

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
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		GloomMagneticLilyPadRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00c9;
				}
				RegisterRealFixtures();
				control = new GloomMagneticLilyPadRuntimeControlStub
				{
					Name = "GloomMagneticLilyPadRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				await VerifyScenario<TowerDefenseZombieFootballGloomShroom>(control, GloomGrid, "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballGloomShroom.tres", "ZombieFootballGloomShroom", "Gloom-shroom Football Zombie");
				await VerifyScenario<TowerDefenseZombieMagnetic>(control, MagneticGrid, "res://Asset/Anime/Character/Zombie/Chapter4/ZombieMagnetic/Packet/ZombieMagnetic.tres", "ZombieMagnetic", "Magnetic Zombie");
				goto end_IL_00b7;
				end_IL_00c9:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGloomMagneticLilyPadRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 18;
		GD.Print($"GLOOM_MAGNETIC_LILYPAD_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyScenario<TZombie>(GloomMagneticLilyPadRuntimeControlStub control, Vector2I grid, string zombiePacketPath, string expectedConfigName, string label) where TZombie : TowerDefenseZombie
	{
		TowerDefensePlantLilyPad lilyPad = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres")?.Plant(grid, playAudio: false) as TowerDefensePlantLilyPad;
		await WaitFrames(4);
		TZombie zombie = LoadPacket(zombiePacketPath)?.Plant(grid, playAudio: false) as TZombie;
		await WaitFrames(6);
		Check(GodotObject.IsInstanceValid(lilyPad) && lilyPad.config?.name == "PlantLilyPad" && GodotObject.IsInstanceValid(zombie) && zombie.config?.name == expectedConfigName, label + " scenario must use the real character and Lily Pad scenes.");
		if (GodotObject.IsInstanceValid(lilyPad) && GodotObject.IsInstanceValid(zombie))
		{
			lilyPad.ProcessMode = ProcessModeEnum.Disabled;
			zombie.ProcessMode = ProcessModeEnum.Disabled;
			control.isGameRunning = true;
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(grid);
			Check(GodotObject.IsInstanceValid(mapCell) && mapCell.isWater && mapCell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER) && lilyPad.cell == mapCell && zombie.cell == mapCell, label + " and its Lily Pad must occupy the same real water cell.");
			GroundHeightComponent groundHeightComponent = zombie.groundHeightComponent;
			AttackComponent attack = zombie.attackComponent;
			BugOverviewGloomMagneticLilyPadRuntimeTest bugOverviewGloomMagneticLilyPadRuntimeTest = this;
			int condition;
			if (groundHeightComponent != null && !groundHeightComponent.IsReleased && attack != null && !attack.IsReleased)
			{
				WaterInteractionComponent waterInteractionComponent = zombie.waterInteractionComponent;
				condition = ((waterInteractionComponent != null && !waterInteractionComponent.IsReleased) ? 1 : 0);
			}
			else
			{
				condition = 0;
			}
			bugOverviewGloomMagneticLilyPadRuntimeTest.Check((byte)condition != 0, label + " must expose active ground-height, water, and bite runtimes.");
			if (groundHeightComponent != null && !groundHeightComponent.IsReleased && attack != null && !attack.IsReleased)
			{
				groundHeightComponent.DetectEnvironment();
				groundHeightComponent.BatchUpdate(1.0);
				Check(zombie.inWater && Mathf.IsEqualApprox((float)zombie.groundHeight, 0f - (float)zombie.waterHeight) && Mathf.IsEqualApprox((float)zombie.z, 0f - (float)zombie.waterHeight), $"{label} must use its authored submerged height instead of crossing water on the land plane; inWater={zombie.inWater}, ground={zombie.groundHeight}, z={zombie.z}, waterHeight={zombie.waterHeight}.");
				Check((zombie.instance.maskFlags & 1) != 0 && zombie.CanCollision(lilyPad.instance.maskFlags) && lilyPad.CanCollision(zombie.instance.collisionFlags), label + " must retain mutual ground collision with the low Lily Pad while swimming.");
				attack.groundRight = 1000.0;
				attack.alive = true;
				attack.SetAlive(alive: true);
				attack.target = null;
				bool flag = attack.CanAttackOnce();
				Check(flag && attack.target == lilyPad, label + " must acquire the real Lily Pad instead of ignoring it and continuing to move.");
				zombie.Walk();
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.WalkProcessing(0.016);
				await WaitFrames(1);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.attack" && attack.target == lilyPad, label + " must leave walking and enter its authored bite state at the Lily Pad.");
				double currentHitPoint = lilyPad.GetCurrentHitPoint();
				zombie.startAttack = true;
				zombie.AttackProcessing(0.5);
				Check(lilyPad.GetCurrentHitPoint() < currentHitPoint, $"{label} must damage the Lily Pad after stopping; hp={lilyPad.GetCurrentHitPoint()}/{currentHitPoint}.");
				lilyPad.QueueFree();
				zombie.QueueFree();
				await WaitFrames(2);
			}
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				bool flag = j == GloomGrid.Y || j == MagneticGrid.Y;
				TowerDefenseCellConfig config = new TowerDefenseCellConfig
				{
					gridType = (flag ? new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.WATER,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					} : new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					})
				};
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(config);
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantLilyPad", "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Packet/PlantLilyPad.tres");
		RegisterPacket("ZombieFootballGloomShroom", "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballGloomShroom.tres");
		RegisterPacket("ZombieMagnetic", "res://Asset/Anime/Character/Zombie/Chapter4/ZombieMagnetic/Packet/ZombieMagnetic.tres");
		RegisterCharacter("PlantLilyPad", "res://Asset/Anime/Character/Plant/Chapter0/LilyPad/Scene/TowerDefensePlantLilyPad.tscn");
		RegisterCharacter("ZombieFootballGloomShroom", "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/GloomShroom/TowerDefenseZombieFootballGloomShroom.tscn");
		RegisterCharacter("ZombieMagnetic", "res://Asset/Anime/Character/Zombie/Chapter4/ZombieMagnetic/Scene/TowerDefenseZombieMagnetic.tscn");
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
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Ignore);
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
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Ignore);
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
			GD.PushError("[BugOverviewGloomMagneticLilyPadRuntimeTest] " + message);
		}
	}
}
