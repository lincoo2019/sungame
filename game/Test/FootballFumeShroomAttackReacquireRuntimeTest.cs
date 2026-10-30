using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class FootballFumeShroomAttackReacquireRuntimeTest : Node
{
	private const string WallnutPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private const string FumePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballFumeShroom.tres";

	private const string FumeScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/FumeShroom/TowerDefenseZombieFootballFumeShroom.tscn";

	private const string BlackFumePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballBlackFumeShroom.tres";

	private const string BlackFumeScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/BlackFumeShroom/TowerDefenseZombieFootballBlackFumeShroom.tscn";

	private const string ScreendoorPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalScreendoor.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ShieldArmorPacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Packet/ZombieShieldArmor.tres";

	private const string ShieldArmorScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Scene/TowerDefenseZombieShieldArmor.tscn";

	private const string TrashBinPacketPath = "res://Asset/Anime/Character/GraveStone/TrashBin/Packet/TrashBin.tres";

	private const string TrashBinScenePath = "res://Asset/Anime/Character/GraveStone/TrashBin/Scene/TowerDefenseTrashBin.tscn";

	private static readonly Vector2I ZombieGrid = new Vector2I(5, 2);

	private static readonly Vector2I TargetGrid = new Vector2I(4, 2);

	private static readonly Vector2I AreaZombieGrid = new Vector2I(8, 2);

	private static readonly Vector2I[] AreaHitGrids = new Vector2I[4]
	{
		new Vector2I(7, 2),
		new Vector2I(6, 2),
		new Vector2I(5, 2),
		new Vector2I(4, 2)
	};

	private static readonly Vector2I AreaOutOfRangeGrid = new Vector2I(3, 2);

	private static readonly Vector2I AreaWrongRowGrid = new Vector2I(7, 3);

	private const int ReplantCycles = 3;

	private const int ExpectedChecks = 75;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<Resource> _registeredResources = new List<Resource>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		FootballFumeShroomAttackReacquireControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 6;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00fa;
				}
				TowerDefenseProjectileRegistry.Init();
				TowerDefenseArmorRegistry.Init();
				RegisterRealFixtures();
				control = new FootballFumeShroomAttackReacquireControlStub
				{
					Name = "FootballFumeShroomAttackReacquireControl",
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
				BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
				await WaitFrames(2);
				Check(GodotObject.IsInstanceValid(bulletField) && BulletField.Instance == bulletField, "The production BulletField must be mounted for Fume-shroom projectiles.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					goto end_IL_00fa;
				}
				await VerifyScenario<TowerDefenseZombieFootballFumeShroom>(control, "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballFumeShroom.tres", "ZombieFootballFumeShroom", "Fume-shroom Football Zombie");
				await VerifyScenario<TowerDefenseZombieFootballBlackFumeShroom>(control, "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballBlackFumeShroom.tres", "ZombieFootballBlackFumeShroom", "Black Fume-shroom Football Zombie");
				await VerifyAreaDamageScenario<TowerDefenseZombieFootballFumeShroom>(control, bulletField, "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballFumeShroom.tres", "ZombieFootballFumeShroom", "Fume-shroom Football Zombie");
				await VerifyAreaDamageScenario<TowerDefenseZombieFootballBlackFumeShroom>(control, bulletField, "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballBlackFumeShroom.tres", "ZombieFootballBlackFumeShroom", "Black Fume-shroom Football Zombie");
				VerifyTemplateFastPathRangeOverride(bulletField);
				await VerifyArmorPenetrationBlockerScenario(control, bulletField);
				await VerifyPhysiquePenetrationBlockerScenario(control, bulletField);
				goto end_IL_00d3;
				end_IL_00fa:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[FootballFumeShroomAttackReacquireRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00d3;
			}
			return;
			end_IL_00d3:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(BulletField.Instance))
			{
				BulletField.Instance.ClearActiveBullets();
			}
			if (GodotObject.IsInstanceValid(control?.characterNode))
			{
				foreach (Node child in control.characterNode.GetChildren())
				{
					if (!child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
			}
			await WaitFrames(5);
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
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 75;
		GD.Print($"FOOTBALL_FUME_REACQUIRE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyScenario<TZombie>(FootballFumeShroomAttackReacquireControlStub control, string zombiePacketPath, string expectedConfigName, string label) where TZombie : TowerDefenseZombie
	{
		TZombie zombie = LoadPacket(zombiePacketPath)?.Plant(ZombieGrid, playAudio: false) as TZombie;
		await WaitFrames(6);
		Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == expectedConfigName, label + " scenario must use the real authored character scene.");
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		AttackComponent attackComponent = zombie.attackComponent;
		AttackComponent rangedAttack = zombie.componentManager.GetRuntime<AttackComponent>("character.attack.1");
		Check(attackComponent != null && !attackComponent.IsReleased && rangedAttack != null && !rangedAttack.IsReleased, label + " must expose both bite and ranged AttackComponent runtimes.");
		if (attackComponent == null || attackComponent.IsReleased || rangedAttack == null || rangedAttack.IsReleased)
		{
			zombie.QueueFree();
			await WaitFrames(2);
			return;
		}
		attackComponent.alive = false;
		attackComponent.SetAlive(alive: false);
		rangedAttack.alive = true;
		rangedAttack.SetAlive(alive: true);
		rangedAttack.groundRight = 1000.0;
		rangedAttack.timer = 0.0;
		rangedAttack.SendStateEvent("ToIdle");
		control.isGameRunning = true;
		int readyCount = 0;
		int overCount = 0;
		rangedAttack.OnAttackReady += OnReady;
		rangedAttack.OnAttackOver += OnOver;
		try
		{
			for (int cycle = 1; cycle <= 3; cycle++)
			{
				TowerDefensePlant wallnut = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(TargetGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				if (GodotObject.IsInstanceValid(wallnut))
				{
					wallnut.ProcessMode = ProcessModeEnum.Disabled;
				}
				Check(GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnut", $"{label} cycle {cycle} must plant a fresh real Wall-nut target.");
				if (GodotObject.IsInstanceValid(wallnut))
				{
					rangedAttack.target = null;
					rangedAttack.timer = 0.0;
					bool flag = rangedAttack.CanAttackOnce();
					Check(flag && rangedAttack.target == wallnut, $"{label} cycle {cycle} must acquire the newly planted target.");
					zombie.IdleProcessing(0.016);
					Check(readyCount == cycle && rangedAttack.StateMachine?.CurrentStateHandle?.StableId == "attack.attack", $"{label} cycle {cycle} must enter a new ranged attack; ready={readyCount}, state={rangedAttack.StateMachine?.CurrentStateHandle?.StableId}.");
					wallnut.ShovelDestroy();
					rangedAttack.AttackProcessing(0.016);
					Check(wallnut.isDestroy && overCount == cycle && rangedAttack.StateMachine?.CurrentStateHandle?.StableId == "attack.idle", $"{label} cycle {cycle} must close the interrupted attack before replanting; destroyed={wallnut.isDestroy}, over={overCount}, state={rangedAttack.StateMachine?.CurrentStateHandle?.StableId}.");
					await WaitFrames(4);
					if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
					{
						wallnut.QueueFree();
						await WaitFrames(2);
					}
				}
			}
		}
		finally
		{
			rangedAttack.OnAttackReady -= OnReady;
			rangedAttack.OnAttackOver -= OnOver;
		}
		zombie.QueueFree();
		await WaitFrames(3);
		void OnOver()
		{
			overCount++;
		}
		void OnReady()
		{
			readyCount++;
		}
	}

	private async Task VerifyAreaDamageScenario<TZombie>(FootballFumeShroomAttackReacquireControlStub control, BulletField bulletField, string zombiePacketPath, string expectedConfigName, string label) where TZombie : TowerDefenseZombie
	{
		List<TowerDefenseCharacter> spawnedCharacters = new List<TowerDefenseCharacter>();
		try
		{
			control.isGameRunning = false;
			TZombie zombie = LoadPacket(zombiePacketPath)?.Plant(AreaZombieGrid, playAudio: false) as TZombie;
			Track(zombie);
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.ProcessMode = ProcessModeEnum.Disabled;
			}
			await WaitFrames(6);
			Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == expectedConfigName, label + " area scenario must use the real authored character scene.");
			if (!GodotObject.IsInstanceValid(zombie))
			{
				return;
			}
			zombie.sprite.pause = true;
			AttackComponent attackComponent = zombie.attackComponent;
			AttackComponent runtime = zombie.componentManager.GetRuntime<AttackComponent>("character.attack.1");
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				attackComponent.alive = false;
				attackComponent.SetAlive(alive: false);
			}
			if (runtime != null && !runtime.IsReleased)
			{
				runtime.alive = false;
				runtime.SetAlive(alive: false);
				runtime.SendStateEvent("ToIdle");
			}
			TowerDefenseZombieNormal screendoor = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalScreendoor.tres")?.Plant(AreaHitGrids[0], playAudio: false) as TowerDefenseZombieNormal;
			TowerDefensePlant wallnut2 = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaHitGrids[1], playAudio: false) as TowerDefensePlant;
			TowerDefensePlant wallnut3 = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaHitGrids[2], playAudio: false) as TowerDefensePlant;
			TowerDefensePlant wallnut4 = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaHitGrids[3], playAudio: false) as TowerDefensePlant;
			TowerDefensePlant outOfRange = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaOutOfRangeGrid, playAudio: false) as TowerDefensePlant;
			TowerDefensePlant wrongRow = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaWrongRowGrid, playAudio: false) as TowerDefensePlant;
			TowerDefenseCharacter[] targets = new TowerDefenseCharacter[6] { screendoor, wallnut2, wallnut3, wallnut4, outOfRange, wrongRow };
			TowerDefenseCharacter[] array = targets;
			foreach (TowerDefenseCharacter towerDefenseCharacter in array)
			{
				Track(towerDefenseCharacter);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
				}
			}
			await WaitFrames(8);
			Check(System.Array.TrueForAll(targets, GodotObject.IsInstanceValid), label + " must spawn four in-range, one out-of-range, and one wrong-row real target.");
			if (System.Array.TrueForAll(targets, GodotObject.IsInstanceValid))
			{
				screendoor.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
				TowerDefenseArmorInstance screendoorArmor = screendoor.GetArmorFromName("Screendoor");
				Check(screendoor.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && screendoorArmor != null, label + " must target a plant-camp real Screendoor zombie with its class-II armor active.");
				TowerDefenseCharacter[] hitTargets = new TowerDefenseCharacter[4] { screendoor, wallnut2, wallnut3, wallnut4 };
				double[] hitpointsBefore = System.Array.ConvertAll(hitTargets, (TowerDefenseCharacter target) => target.instance.hitpoints);
				double armorHitpointsBefore = screendoorArmor?.hitPoints ?? (0.0 / 0.0);
				double outOfRangeHitpointsBefore = outOfRange.instance.hitpoints;
				double wrongRowHitpointsBefore = wrongRow.instance.hitpoints;
				control.isGameRunning = true;
				bulletField.ClearActiveBullets();
				ExecuteFumeShroomAttack(zombie);
				int puffIndex = bulletField.LastSpawnedIndex;
				bool flag = bulletField.ActiveCount == 1 && bulletField.IsBulletActive(puffIndex);
				Check(flag, $"{label} must emit exactly one active real Puff; active={bulletField.ActiveCount}, index={puffIndex}.");
				if (flag)
				{
					ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(puffIndex);
					int num = 1;
					int num2 = 4;
					Check(bulletDataRef.config?.name == "Puff" && IsApprox(bulletDataRef.damage, 20.0) && bulletDataRef.damageFlags == 2, $"{label} Puff must retain the real 20-damage HITBODY config; name={bulletDataRef.config?.name}, damage={bulletDataRef.damage}, flags={bulletDataRef.damageFlags}.");
					Check((bulletDataRef.fireMethodFlags & (num | num2)) == (num | num2) && bulletDataRef.penetrateNum == -1, $"{label} Puff must use unlimited penetration; flags={bulletDataRef.fireMethodFlags}, num={bulletDataRef.penetrateNum}.");
					Check(bulletDataRef.fireLength == 4 && Mathf.IsEqualApprox(bulletDataRef.checkDistance, 400f), $"{label} Puff must stop after four grid widths; fireLength={bulletDataRef.fireLength}, distance={bulletDataRef.checkDistance}.");
				}
				else
				{
					Check(condition: false, label + " Puff config could not be inspected because it was inactive.");
					Check(condition: false, label + " Puff penetration state could not be inspected because it was inactive.");
					Check(condition: false, label + " Puff range could not be inspected because it was inactive.");
				}
				Check(zombie.GetNode<GpuParticles2D>("%FireParticles").Emitting, label + " attack must restart the authored fire particles.");
				await WaitFrames(180);
				for (int num3 = 0; num3 < hitTargets.Length; num3++)
				{
					double num4 = hitpointsBefore[num3] - hitTargets[num3].instance.hitpoints;
					Check(IsApprox(num4, 20.0), $"{label} must damage in-range cell {AreaHitGrids[num3]} exactly once; damage={num4}.");
				}
				Check(screendoorArmor != null && IsApprox(screendoorArmor.hitPoints, armorHitpointsBefore), $"{label} must bypass Screendoor armor without damaging it; before={armorHitpointsBefore}, after={screendoorArmor?.hitPoints}.");
				Check(IsApprox(outOfRange.instance.hitpoints, outOfRangeHitpointsBefore), label + " must not damage the fifth forward cell.");
				Check(IsApprox(wrongRow.instance.hitpoints, wrongRowHitpointsBefore), label + " must not damage a target on another row.");
				Check(!bulletField.IsBulletActive(puffIndex) && bulletField.ActiveCount == 0, $"{label} Puff must despawn after its four-cell travel; active={bulletField.ActiveCount}.");
				return;
			}
		}
		finally
		{
			bulletField.ClearActiveBullets();
			foreach (TowerDefenseCharacter item in spawnedCharacters)
			{
				if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
				{
					item.QueueFree();
				}
			}
			await WaitFrames(6);
		}
		void Track(TowerDefenseCharacter character)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				spawnedCharacters.Add(character);
			}
		}
	}

	private void VerifyTemplateFastPathRangeOverride(BulletField bulletField)
	{
		bulletField.ClearActiveBullets();
		TowerDefenseProjectileCreateData projectileData = new TowerDefenseProjectileCreateData("Puff");
		BulletFieldSpawnOverrides bulletFieldSpawnOverrides = new BulletFieldSpawnOverrides
		{
			fireLengthOverride = 4f
		};
		Vector2 pos = new Vector2(750f, 114f);
		Vector2 velocity = new Vector2(-100f, 0f);
		BulletFieldSpawnOverrides overrides = bulletFieldSpawnOverrides;
		FireComponent.CreateProjectilePosition(null, null, 0.0, pos, velocity, projectileData, -1, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, default(Vector2), overrides);
		int lastSpawnedIndex = bulletField.LastSpawnedIndex;
		bool flag = bulletField.ActiveCount == 1 && bulletField.IsBulletActive(lastSpawnedIndex);
		Check(flag, "The cached Puff template path must emit one active BulletField projectile.");
		if (flag)
		{
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(lastSpawnedIndex);
			Check(bulletDataRef.fireLength == 4 && Mathf.IsEqualApprox(bulletDataRef.checkDistance, 400f), $"The cached Puff template path must preserve the four-cell range override; fireLength={bulletDataRef.fireLength}, distance={bulletDataRef.checkDistance}.");
		}
		else
		{
			Check(condition: false, "The cached Puff template range could not be inspected because it was inactive.");
		}
		bulletField.ClearActiveBullets();
	}

	private async Task VerifyArmorPenetrationBlockerScenario(FootballFumeShroomAttackReacquireControlStub control, BulletField bulletField)
	{
		List<TowerDefenseCharacter> spawnedCharacters = new List<TowerDefenseCharacter>();
		try
		{
			control.isGameRunning = false;
			TowerDefenseZombieFootballFumeShroom zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballFumeShroom.tres")?.Plant(AreaZombieGrid, playAudio: false) as TowerDefenseZombieFootballFumeShroom;
			Track(zombie);
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.ProcessMode = ProcessModeEnum.Disabled;
			}
			await WaitFrames(6);
			Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieFootballFumeShroom", "Armor blocker scenario must use the real Fume-shroom Football Zombie.");
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.sprite.pause = true;
				TowerDefenseZombieShieldArmor blocker = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Packet/ZombieShieldArmor.tres")?.Plant(AreaHitGrids[0], playAudio: false) as TowerDefenseZombieShieldArmor;
				TowerDefensePlant behind = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaHitGrids[1], playAudio: false) as TowerDefensePlant;
				Track(blocker);
				Track(behind);
				if (GodotObject.IsInstanceValid(blocker))
				{
					blocker.ProcessMode = ProcessModeEnum.Disabled;
					blocker.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
				}
				if (GodotObject.IsInstanceValid(behind))
				{
					behind.ProcessMode = ProcessModeEnum.Disabled;
				}
				await WaitFrames(8);
				TowerDefenseArmorInstance shield = blocker?.GetArmorFromName("Shield");
				int num = 1024;
				Check(GodotObject.IsInstanceValid(blocker) && GodotObject.IsInstanceValid(behind) && shield != null && (shield.armorMethodFlags & num) != 0, "Armor blocker scenario must use a real active CANT_PENETRATE Shield and a target behind it.");
				if (GodotObject.IsInstanceValid(blocker) && GodotObject.IsInstanceValid(behind))
				{
					double shieldHitpointsBefore = shield?.hitPoints ?? (0.0 / 0.0);
					double behindHitpointsBefore = behind.instance.hitpoints;
					control.isGameRunning = true;
					bulletField.ClearActiveBullets();
					ExecuteFumeShroomAttack(zombie);
					int puffIndex = bulletField.LastSpawnedIndex;
					Check(bulletField.ActiveCount == 1 && bulletField.IsBulletActive(puffIndex), "Armor blocker scenario must emit one active real Puff.");
					await WaitFrames(180);
					Check(shield != null && IsApprox(shieldHitpointsBefore - shield.hitPoints, 20.0), $"CANT_PENETRATE Shield must receive the Puff's 20 damage; before={shieldHitpointsBefore}, after={shield?.hitPoints}.");
					Check(!bulletField.IsBulletActive(puffIndex) && IsApprox(behind.instance.hitpoints, behindHitpointsBefore), "CANT_PENETRATE Shield must consume the Puff before it damages the target behind it.");
					return;
				}
				return;
			}
		}
		finally
		{
			bulletField.ClearActiveBullets();
			foreach (TowerDefenseCharacter item in spawnedCharacters)
			{
				if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
				{
					item.QueueFree();
				}
			}
			await WaitFrames(6);
		}
		void Track(TowerDefenseCharacter character)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				spawnedCharacters.Add(character);
			}
		}
	}

	private async Task VerifyPhysiquePenetrationBlockerScenario(FootballFumeShroomAttackReacquireControlStub control, BulletField bulletField)
	{
		List<TowerDefenseCharacter> spawnedCharacters = new List<TowerDefenseCharacter>();
		try
		{
			control.isGameRunning = false;
			TowerDefenseZombieFootballFumeShroom zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballFumeShroom.tres")?.Plant(AreaZombieGrid, playAudio: false) as TowerDefenseZombieFootballFumeShroom;
			Track(zombie);
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.ProcessMode = ProcessModeEnum.Disabled;
			}
			await WaitFrames(6);
			Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieFootballFumeShroom", "Physique blocker scenario must use the real Fume-shroom Football Zombie.");
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.sprite.pause = true;
				TowerDefenseGravestone blocker = LoadPacket("res://Asset/Anime/Character/GraveStone/TrashBin/Packet/TrashBin.tres")?.Plant(AreaHitGrids[0], playAudio: false) as TowerDefenseGravestone;
				TowerDefensePlant behind = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres")?.Plant(AreaHitGrids[1], playAudio: false) as TowerDefensePlant;
				Track(blocker);
				Track(behind);
				if (GodotObject.IsInstanceValid(blocker))
				{
					blocker.ProcessMode = ProcessModeEnum.Disabled;
					blocker.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
				}
				if (GodotObject.IsInstanceValid(behind))
				{
					behind.ProcessMode = ProcessModeEnum.Disabled;
				}
				await WaitFrames(8);
				int num = 1024;
				Check(GodotObject.IsInstanceValid(blocker) && GodotObject.IsInstanceValid(behind) && (blocker.instance.physiqueTypeFlags & num) != 0, "Physique blocker scenario must use a real CANT_PENETRATE Trash Bin and a target behind it.");
				if (GodotObject.IsInstanceValid(blocker) && GodotObject.IsInstanceValid(behind))
				{
					double blockerHitpointsBefore = blocker.instance.hitpoints;
					double behindHitpointsBefore = behind.instance.hitpoints;
					control.isGameRunning = true;
					bulletField.ClearActiveBullets();
					ExecuteFumeShroomAttack(zombie);
					int puffIndex = bulletField.LastSpawnedIndex;
					Check(bulletField.ActiveCount == 1 && bulletField.IsBulletActive(puffIndex), "Physique blocker scenario must emit one active real Puff.");
					await WaitFrames(180);
					Check(IsApprox(blockerHitpointsBefore - blocker.instance.hitpoints, 20.0), $"CANT_PENETRATE physique must receive the Puff's 20 damage; before={blockerHitpointsBefore}, after={blocker.instance.hitpoints}.");
					Check(!bulletField.IsBulletActive(puffIndex) && IsApprox(behind.instance.hitpoints, behindHitpointsBefore), "CANT_PENETRATE physique must consume the Puff before it damages the target behind it.");
					return;
				}
				return;
			}
		}
		finally
		{
			bulletField.ClearActiveBullets();
			foreach (TowerDefenseCharacter item in spawnedCharacters)
			{
				if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
				{
					item.QueueFree();
				}
			}
			await WaitFrames(6);
		}
		void Track(TowerDefenseCharacter character)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				spawnedCharacters.Add(character);
			}
		}
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
			mapConfig = towerDefenseMapConfig,
			rect = TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(towerDefenseMapConfig)
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig config = new TowerDefenseCellConfig
				{
					gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					}
				};
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(config);
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
		RegisterPacket("ZombieFootballFumeShroom", "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballFumeShroom.tres");
		RegisterPacket("ZombieFootballBlackFumeShroom", "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootballBlackFumeShroom.tres");
		RegisterPacket("ZombieNormalScreendoor", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalScreendoor.tres");
		RegisterPacket("ZombieShieldArmor", "res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Packet/ZombieShieldArmor.tres");
		RegisterPacket("TrashBin", "res://Asset/Anime/Character/GraveStone/TrashBin/Packet/TrashBin.tres");
		RegisterCharacter("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
		RegisterCharacter("ZombieFootballFumeShroom", "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/FumeShroom/TowerDefenseZombieFootballFumeShroom.tscn");
		RegisterCharacter("ZombieFootballBlackFumeShroom", "res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/BlackFumeShroom/TowerDefenseZombieFootballBlackFumeShroom.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterCharacter("ZombieShieldArmor", "res://Asset/Anime/Character/Zombie/Chapter3/ZombieShieldArmor/Scene/TowerDefenseZombieShieldArmor.tscn");
		RegisterCharacter("TrashBin", "res://Asset/Anime/Character/GraveStone/TrashBin/Scene/TowerDefenseTrashBin.tscn");
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
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(towerDefensePacketConfig);
		instance.TOWERDEFENSE_PACKETS[key] = towerDefensePacketConfig;
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
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(packedScene);
		instance.TOWERDEFENSE_CHARCATERS[key] = packedScene;
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

	private static bool IsApprox(double actual, double expected)
	{
		return Math.Abs(actual - expected) <= 0.001;
	}

	private static void ExecuteFumeShroomAttack(TowerDefenseZombie zombie)
	{
		if (!(zombie is TowerDefenseZombieFootballFumeShroom towerDefenseZombieFootballFumeShroom))
		{
			if (!(zombie is TowerDefenseZombieFootballBlackFumeShroom towerDefenseZombieFootballBlackFumeShroom))
			{
				throw new InvalidOperationException("Unsupported Fume-shroom zombie type: " + (zombie?.GetType().Name ?? "<null>"));
			}
			towerDefenseZombieFootballBlackFumeShroom.FumeShroomAttack();
		}
		else
		{
			towerDefenseZombieFootballFumeShroom.FumeShroomAttack();
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FootballFumeShroomAttackReacquireRuntimeTest] " + message);
		}
	}
}
