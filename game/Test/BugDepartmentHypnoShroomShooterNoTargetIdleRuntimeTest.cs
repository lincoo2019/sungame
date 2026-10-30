using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentHypnoShroomShooterNoTargetIdleRuntimeTest : Node
{
	private const string ShooterPacketPath = "res://Asset/Anime/Character/Plant/Diamond/HypnoShroomShooter/Packet/PlantHypnoShroomShooter.tres";

	private const string ShooterScenePath = "res://Asset/Anime/Character/Plant/Diamond/HypnoShroomShooter/Scene/TowerDefensePlantHypnoShroomShooter.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string CupidConfigPath = "res://Asset/Config/Projectile/Cupid/CupidDefault.tres";

	private static readonly Vector2I ShooterGrid = new Vector2I(2, 2);

	private static readonly Vector2I ZombieGrid = new Vector2I(6, 2);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousProjectiles = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly HashSet<string> _missingProjectiles = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BugDepartmentHypnoShroomShooterNoTargetIdleControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		FireComponent fire = null;
		int readyCount = 0;
		int volleyCount = 0;
		FireComponent.FireReadyEventHandler readyHandler = delegate
		{
			readyCount++;
		};
		FireComponent.FireVolleyEventHandler volleyHandler = delegate
		{
			volleyCount++;
		};
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00d5;
				}
				RegisterRealFixtures();
				control = new BugDepartmentHypnoShroomShooterNoTargetIdleControlStub
				{
					Name = "HypnoShroomShooterNoTargetIdleControl",
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
				TowerDefensePlantHypnoShroomShooter shooter = LoadPacket("res://Asset/Anime/Character/Plant/Diamond/HypnoShroomShooter/Packet/PlantHypnoShroomShooter.tres")?.Plant(ShooterGrid, playAudio: false) as TowerDefensePlantHypnoShroomShooter;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(shooter), "The real Cupid Hypno-shroom Shooter must spawn.");
				if (!GodotObject.IsInstanceValid(shooter))
				{
					goto end_IL_00d5;
				}
				shooter.ProcessMode = ProcessModeEnum.Disabled;
				fire = shooter.componentManager.GetRuntime<FireComponent>("character.fire");
				Check(fire != null && !fire.IsReleased, "The real shooter FireComponent must be active.");
				if (fire == null || fire.IsReleased)
				{
					goto end_IL_00d5;
				}
				fire.OnFireReady += readyHandler;
				fire.OnFireVolley += volleyHandler;
				Check(fire.checkUse, "Cupid Hypno-shroom Shooter must gate attack animation and firing on a live target.");
				FireComponentCheckConfig check = ((fire.fireCheckList.Count == 1) ? fire.fireCheckList[0] : null);
				TowerDefenseProjectileCreateData cupid = check?.projectile?.GetProjectile();
				Check(GodotObject.IsInstanceValid(check) && GodotObject.IsInstanceValid(cupid) && cupid.projectileName == new StringName("Cupid"), "The real shooter must retain its Cupid target check.");
				Check(fire.fireProjectileList.Count == 3 && fire.fireProjectileList[0].offsetLine == -1 && fire.fireProjectileList[1].offsetLine == 0 && fire.fireProjectileList[2].offsetLine == 1, "The real shooter must retain one Cupid shot for each of the front three rows.");
				Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.idle", "The shooter fire state must begin idle; current=" + fire.StateMachine?.CurrentStateHandle?.StableId + ".");
				if (!GodotObject.IsInstanceValid(cupid) || fire.fireProjectileList.Count != 3)
				{
					goto end_IL_00d5;
				}
				control.isGameRunning = true;
				fire.timer = 0f;
				fire.checkIntreval = 0;
				Check(!fire.CanFireCheckOnce(cupid), "With no zombies, the real Cupid check must reject firing.");
				string text = fire.sprite?.clip;
				int num = fire.sprite?.frameIndex ?? (-1);
				fire.IdleProcessing(0.0);
				Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.idle", "With no zombies, the shooter must remain in fire.idle; current=" + fire.StateMachine?.CurrentStateHandle?.StableId + ".");
				Check(readyCount == 0, $"With no zombies, attack preparation must not start; readyCount={readyCount}.");
				Check(volleyCount == 0, $"With no zombies, no Cupid volley may be emitted; volleyCount={volleyCount}.");
				BugDepartmentHypnoShroomShooterNoTargetIdleRuntimeTest bugDepartmentHypnoShroomShooterNoTargetIdleRuntimeTest = this;
				int condition;
				if (fire.sprite?.clip == text)
				{
					AdobeAnimateSprite sprite = fire.sprite;
					condition = ((sprite != null && sprite.frameIndex == num) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugDepartmentHypnoShroomShooterNoTargetIdleRuntimeTest.Check((byte)condition != 0, $"With no zombies, the plant animation must not restart or twitch; before={text}:{num}, after={fire.sprite?.clip}:{fire.sprite?.frameIndex}.");
				fire.SendStateEvent(fire.idleStateEvent, allowWhenInactive: true);
				shooter.Idle();
				readyCount = 0;
				volleyCount = 0;
				TowerDefenseZombieNormal zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(ZombieGrid, playAudio: false) as TowerDefenseZombieNormal;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(zombie), "A real normal zombie must spawn in front of the shooter.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_00d5;
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				fire.timer = 0f;
				fire.checkIntreval = 0;
				Check(fire.CanFireCheckOnce(cupid), "A real zombie in the front three rows must satisfy the Cupid target check.");
				fire.IdleProcessing(0.0);
				Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack", "A real target must still enter fire.attack; current=" + fire.StateMachine?.CurrentStateHandle?.StableId + ".");
				Check(readyCount == 1, $"A real target must prepare exactly one attack; readyCount={readyCount}.");
				Check(fire.runningCheck == check && fire.runningCheckId == 0, "The attack must retain the authored Cupid target check.");
				fire.onlyEmitSignal = true;
				fire.AnimeEvent(fire.fireEventName, default(Variant));
				Check(volleyCount == 3, $"One real attack event must emit the authored three-row Cupid volley; volleyCount={volleyCount}.");
				goto end_IL_00c3;
				end_IL_00d5:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[HypnoShroomShooterNoTargetIdle] Unexpected exception: {value}");
				goto end_IL_00c3;
			}
			return;
			end_IL_00c3:;
		}
		finally
		{
			if (fire != null)
			{
				fire.OnFireReady -= readyHandler;
				fire.OnFireVolley -= volleyHandler;
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 19;
		GD.Print($"BUG_DEPARTMENT_HYPNO_SHROOM_NO_TARGET_IDLE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantHypnoShroomShooter", "res://Asset/Anime/Character/Plant/Diamond/HypnoShroomShooter/Packet/PlantHypnoShroomShooter.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantHypnoShroomShooter", "res://Asset/Anime/Character/Plant/Diamond/HypnoShroomShooter/Scene/TowerDefensePlantHypnoShroomShooter.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterProjectile("Cupid", "res://Asset/Config/Projectile/Cupid/CupidDefault.tres");
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

	private void RegisterProjectile(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.PROJECTILE_CONFIG.TryGetValue(key, out var value))
		{
			_previousProjectiles[key] = value;
		}
		else
		{
			_missingProjectiles.Add(key);
		}
		instance.PROJECTILE_CONFIG[key] = ResourceLoader.Load<TowerDefenseProjectileConfig>(path, null, ResourceLoader.CacheMode.Ignore);
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
		foreach (string missingProjectile in _missingProjectiles)
		{
			instance.PROJECTILE_CONFIG.Remove(missingProjectile);
		}
		foreach (KeyValuePair<string, Resource> previousProjectile in _previousProjectiles)
		{
			instance.PROJECTILE_CONFIG[previousProjectile.Key] = previousProjectile.Value;
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
			GD.PushError("[HypnoShroomShooterNoTargetIdle] " + message);
		}
	}
}
