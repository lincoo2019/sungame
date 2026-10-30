using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewReCactusDoubleVolleyRuntimeTest : Node
{
	private const string ReCactusPacketPath = "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Packet/PlantReCactus.tres";

	private const string ReCactusScenePath = "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Scene/TowerDefensePlantReCactus.tscn";

	private const string SwanPacketPath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres";

	private const string SwanScenePath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn";

	private const string SpikeConfigPath = "res://Asset/Config/Projectile/Spike/SpikeDefault.tres";

	private static readonly Vector2I ReCactusGrid = new Vector2I(2, 2);

	private static readonly Vector2I SwanGrid = new Vector2I(6, 2);

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
		BugOverviewReCactusDoubleVolleyControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_009e;
				}
				RegisterRealFixtures();
				control = new BugOverviewReCactusDoubleVolleyControlStub
				{
					Name = "ReCactusDoubleVolleyControl",
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
				TowerDefensePlantReCactus reCactus = LoadPacket("res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Packet/PlantReCactus.tres")?.Plant(ReCactusGrid, playAudio: false) as TowerDefensePlantReCactus;
				await WaitFrames(4);
				TowerDefenseZombieSwanRider swan = LoadPacket("res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres")?.Plant(SwanGrid, playAudio: false) as TowerDefenseZombieSwanRider;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(reCactus) && GodotObject.IsInstanceValid(swan), "Real ReCactus and SwanRider characters must spawn.");
				if (!GodotObject.IsInstanceValid(reCactus) || !GodotObject.IsInstanceValid(swan))
				{
					goto end_IL_009e;
				}
				swan.ProcessMode = ProcessModeEnum.Disabled;
				FireComponent fire = reCactus.componentManager.GetRuntime<FireComponent>("character.fire");
				Check(fire != null && !fire.IsReleased, "The real ReCactus FireComponent must be active.");
				if (fire == null || fire.IsReleased)
				{
					goto end_IL_009e;
				}
				Check(fire.fireNum == 2, $"ReCactus runtime must retain its authored two-shot burst; fireNum={fire.fireNum}.");
				Check(!fire.fireNumAtOnce, "ReCactus must use two animation events instead of spawning both shots at once.");
				Check(fire.fireCheckList.Count >= 2, "ReCactus must retain distinct air-first and normal target checks.");
				int airFlag = 2;
				FireComponentCheckConfig fireComponentCheckConfig = ((fire.fireCheckList.Count > 0) ? fire.fireCheckList[0] : null);
				TowerDefenseProjectileCreateData airProjectile = fireComponentCheckConfig?.projectile?.GetProjectile();
				Check(GodotObject.IsInstanceValid(fireComponentCheckConfig) && !fireComponentCheckConfig.useParentCollision && fireComponentCheckConfig.GetCollisionFlags() == airFlag, "The first ReCactus check must be the authored off-ground target path.");
				Check(GodotObject.IsInstanceValid(airProjectile) && airProjectile.projectileName == new StringName("Spike") && airProjectile.collisionFlags == airFlag, "The real air Spike must retain its authored off-ground trajectory data.");
				swan.FlyEntered();
				Check(swan.instance.maskFlags == airFlag, "SwanRider must switch to the real off-ground target channel.");
				control.isGameRunning = true;
				fire.timer = 0f;
				fire.checkIntreval = 0;
				Check(fire.CanFireCheckOnce(airProjectile, airFlag) && fire.firstCharacter == swan, "ReCactus must acquire the real flying SwanRider before firing.");
				double hitpointsBeforeVolley = swan.instance.hitpoints;
				int volleyCount = 0;
				List<int> fireIndices = new List<int>();
				List<int> volleyFrames = new List<int>();
				List<int> volleyAnimationFrames = new List<int>();
				int runtimeFrame = -1;
				int firstPostVolleyAnimationFrame = -1;
				int airborneIdleFrame = -1;
				double airborneIdleHeight = 0.0 / 0.0;
				fire.OnFireVolley += OnVolley;
				fire.runningCheck = fireComponentCheckConfig;
				fire.runningCheckId = 0;
				fire.currentFireNum = 0;
				fire.AttackEntered();
				int firstVolleyFrame = -1;
				for (int frame = 0; frame < 240; frame++)
				{
					runtimeFrame = frame;
					fire.AttackProcessing(1.0 / 60.0);
					reCactus.BatchUpdate(1.0 / 60.0);
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					if (airborneIdleFrame < 0 && reCactus.z > reCactus.groundHeight + 0.001 && reCactus.ySpeed >= 0.0 && reCactus.sprite.clip != "Jump")
					{
						airborneIdleFrame = frame;
						airborneIdleHeight = reCactus.z;
					}
					if (volleyCount > 0 && firstVolleyFrame < 0)
					{
						firstVolleyFrame = frame;
						firstPostVolleyAnimationFrame = fire.sprite.frameIndex;
					}
					if (firstVolleyFrame >= 0 && frame - firstVolleyFrame >= 50)
					{
						break;
					}
				}
				fire.OnFireVolley -= OnVolley;
				Check(firstVolleyFrame >= 0, "The real ReCactus attack state must fire at the flying target.");
				bool value = fire.CanFireCheckOnce(airProjectile, airFlag);
				Check(volleyCount == 2, $"One ReCactus attack must emit exactly two consecutive air shots; volleys={volleyCount}, currentFireNum={fire.currentFireNum}, target={value}, swanHp={swan.instance.hitpoints}, swanDie={swan.die}, clip={fire.sprite?.clip}, frame={fire.sprite?.frameIndex}, timeScale={fire.sprite?.timeScale}.");
				Check(fireIndices.Count == 2 && fireIndices[0] == 0 && fireIndices[1] == 1, "The two shots must belong to one fireNum=2 burst; indices=" + string.Join(',', fireIndices) + ".");
				Check(volleyFrames.Count == 2 && volleyFrames[1] > volleyFrames[0], "ReCactus must emit its second shot from a repeated attack animation event; volleyFrames=" + string.Join(',', volleyFrames) + ".");
				Check(volleyAnimationFrames.Count == 2 && firstPostVolleyAnimationFrame >= fire.sprite.clipRange.X && firstPostVolleyAnimationFrame < volleyAnimationFrames[0], $"The first fire event must immediately restart the attack animation; eventFrame={string.Join(',', volleyAnimationFrames)}, postVolleyFrame={firstPostVolleyAnimationFrame}, clipStart={fire.sprite.clipRange.X}.");
				Check(airborneIdleFrame < 0, $"ReCactus body must remain in Jump while z is still descending; earlyIdleFrame={airborneIdleFrame}, z={airborneIdleHeight}, ground={reCactus.groundHeight}, clip={reCactus.sprite.clip}.");
				await WaitFrames(120);
				Check(Math.Abs(hitpointsBeforeVolley - swan.instance.hitpoints - 40.0) < 0.001, $"Both real air Spikes must follow a valid trajectory and deal damage; before={hitpointsBeforeVolley}, after={swan.instance.hitpoints}.");
				List<double> settledHeights = new List<double>();
				List<int> settledAnimationFrames = new List<int>();
				List<string> settledAnimationClips = new List<string>();
				bool stayedInsideIdleClip = true;
				for (int frame = 0; frame < 24; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					settledHeights.Add(reCactus.z);
					settledAnimationFrames.Add(reCactus.sprite.frameIndex);
					settledAnimationClips.Add(reCactus.sprite.clip);
					stayedInsideIdleClip &= reCactus.sprite.clip == reCactus.idleAnimeClip && reCactus.sprite.frameIndex >= reCactus.sprite.clipRange.X && reCactus.sprite.frameIndex < reCactus.sprite.clipRange.Y;
				}
				Check(reCactus.isGround && Math.Abs(reCactus.ySpeed) < 0.001, $"ReCactus must settle its vertical physics after landing; isGround={reCactus.isGround}, z={reCactus.z}, ground={reCactus.groundHeight}, ySpeed={reCactus.ySpeed}.");
				Check(settledHeights.TrueForAll((double height) => Math.Abs(height - reCactus.groundHeight) < 0.001), "ReCactus must remain on one stable landing plane; heights=" + string.Join(',', settledHeights) + ".");
				Check(reCactus.sprite.clip != "Jump", $"ReCactus must leave the looping Jump clip after landing; clip={reCactus.sprite.clip}, frame={reCactus.sprite.frameIndex}.");
				Check(stayedInsideIdleClip, $"ReCactus must not twitch back into stale Jump frames after landing; idle={reCactus.idleAnimeClip}, clips={string.Join(',', settledAnimationClips)}, frames={string.Join(',', settledAnimationFrames)}, range={reCactus.sprite.clipRange}.");
				goto end_IL_007b;
				end_IL_009e:
				void OnVolley(ulong _)
				{
					volleyCount++;
					fireIndices.Add(fire.currentFireNum);
					volleyFrames.Add(runtimeFrame);
					volleyAnimationFrames.Add(fire.sprite.frameIndex);
					if (volleyCount == 2)
					{
						fire.alive = false;
					}
				}
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[ReCactusDoubleVolley] Unexpected exception: {value2}");
				goto end_IL_007b;
			}
			return;
			end_IL_007b:;
		}
		finally
		{
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
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"BUG_OVERVIEW_025B_RECACTUS_DOUBLE_VOLLEY_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("PlantReCactus", "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Packet/PlantReCactus.tres");
		RegisterPacket("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres");
		RegisterCharacter("PlantReCactus", "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Scene/TowerDefensePlantReCactus.tscn");
		RegisterCharacter("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn");
		RegisterProjectile("Spike", "res://Asset/Config/Projectile/Spike/SpikeDefault.tres");
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
			GD.PushError("[ReCactusDoubleVolley] " + message);
		}
	}
}
