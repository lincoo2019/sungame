using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class DoomShroomExplosionCrashRuntimeTest : Node
{
	private const string DoomPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/DoomShroom/Packet/DoomShroom.tres";

	private const string DoomScenePath = "res://Asset/Anime/Character/Plant/Chapter0/DoomShroom/Scene/TowerDefensePlantDoomShroom.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string CraterPacketPath = "res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres";

	private const string CraterScenePath = "res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn";

	private const int ZombieCount = 48;

	private static readonly Vector2I DoomGrid = new Vector2I(4, 3);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<TowerDefenseZombie> _zombies = new List<TowerDefenseZombie>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		DoomShroomExplosionCrashRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00d5;
				}
				RegisterRealFixtures();
				control = new DoomShroomExplosionCrashRuntimeControlStub
				{
					Name = "DoomShroomExplosionCrashRuntimeControl",
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
				TowerDefensePlantDoomShroom doomShroom = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/DoomShroom/Packet/DoomShroom.tres")?.Plant(DoomGrid, playAudio: false) as TowerDefensePlantDoomShroom;
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(doomShroom), "The real Doom-shroom fixture must spawn.");
				if (!GodotObject.IsInstanceValid(doomShroom))
				{
					goto end_IL_00d5;
				}
				Vector2 globalPosition = doomShroom.GlobalPosition;
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
				for (int i = 0; i < 48; i++)
				{
					TowerDefenseZombie towerDefenseZombie = packedScene?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
					if (!GodotObject.IsInstanceValid(towerDefenseZombie))
					{
						break;
					}
					int num = i % 8 - 4;
					int num2 = i / 8 - 3;
					towerDefenseZombie.Name = $"DoomExplosionTarget{i}";
					towerDefenseZombie.inGame = false;
					towerDefenseZombie.editorPreviewMode = true;
					towerDefenseZombie.gridPos = DoomGrid;
					towerDefenseZombie.GlobalPosition = globalPosition + new Vector2((float)num * 35f, (float)num2 * 28f);
					control.characterNode.AddChild(towerDefenseZombie, forceReadableName: false, InternalMode.Disabled);
					_zombies.Add(towerDefenseZombie);
				}
				await WaitFrames(10);
				for (int j = 0; j < _zombies.Count; j++)
				{
					manager.CharacterRegister(_zombies[j]);
				}
				Check(_zombies.Count == 48, $"The crash regression must create {48} real zombies; created={_zombies.Count}.");
				Check(_zombies.TrueForAll((TowerDefenseZombie zombie) => GodotObject.IsInstanceValid(zombie) && zombie.HasHitBox && zombie.IsHitBoxMonitorable), "Every real zombie must expose an active explosion hitbox.");
				ExplodeComponent explodeComponent = doomShroom.componentManager?.GetRuntime<ExplodeComponent>("character.explode");
				Check(explodeComponent != null && !explodeComponent.IsReleased && explodeComponent.explodeEvent.Count > 0 && GodotObject.IsInstanceValid(explodeComponent.explodeEffect), "The real Doom-shroom must bind its authored explosion runtime.");
				if (explodeComponent == null || explodeComponent.IsReleased)
				{
					goto end_IL_00d5;
				}
				TowerDefenseCellInstance doomCell = TowerDefenseManager.GetMapCell(DoomGrid);
				Check(GodotObject.IsInstanceValid(doomCell), "The explosion fixture must expose the Doom-shroom map cell.");
				doomShroom.cell = null;
				explodeComponent.Explode();
				for (int frame = 0; frame < 120; frame++)
				{
					if (!_zombies.Exists((TowerDefenseZombie zombie) => GodotObject.IsInstanceValid(zombie) && !zombie.isDestroy))
					{
						break;
					}
					await WaitFrames(1);
				}
				int count = _zombies.FindAll((TowerDefenseZombie zombie) => !GodotObject.IsInstanceValid(zombie) || zombie.isDestroy).Count;
				Check(count == 48, $"The Doom-shroom blast must finish all real targets without a runtime crash; destroyed={count}/{48}.");
				Check(GodotObject.IsInstanceValid(doomCell) && doomCell.characterList.Exists((TowerDefenseCharacter character) => GodotObject.IsInstanceValid(character) && character is TowerDefenseCrater), "Crater creation must recover the map cell when the exploding character does not cache one.");
				Check(GetTree() != null && !GetTree().IsPaused(), "The scene tree must remain responsive after the explosion burst.");
				goto end_IL_00be;
				end_IL_00d5:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[DoomShroomExplosionCrashRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00be;
			}
			return;
			end_IL_00be:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				for (int num3 = 0; num3 < _zombies.Count; num3++)
				{
					TowerDefenseZombie towerDefenseZombie2 = _zombies[num3];
					if (GodotObject.IsInstanceValid(towerDefenseZombie2))
					{
						manager.CharacterUnregister(towerDefenseZombie2);
					}
				}
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"DOOM_SHROOM_EXPLOSION_CRASH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantDoomShroom", "res://Asset/Anime/Character/Plant/Chapter0/DoomShroom/Packet/DoomShroom.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterPacket("CraterDayGround", "res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres");
		RegisterCharacter("PlantDoomShroom", "res://Asset/Anime/Character/Plant/Chapter0/DoomShroom/Scene/TowerDefensePlantDoomShroom.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterCharacter("CraterDayGround", "res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn");
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[DoomShroomExplosionCrashRuntimeTest] " + message);
		}
	}
}
