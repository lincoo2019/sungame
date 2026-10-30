using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGargantuarImmatureMineSmashRuntimeTest : Node
{
	private readonly struct MineScenario(string name, string packetPath, string scenePath, Vector2I grid)
	{
		public readonly string Name = name;

		public readonly string PacketPath = packetPath;

		public readonly string ScenePath = scenePath;

		public readonly Vector2I Grid = grid;
	}

	private const string GargantuarPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres";

	private const string GargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn";

	private const string NormalZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string PotatoMinePacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres";

	private const string PotatoMineScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn";

	private const string SunMinePacketPath = "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Packet/PlantSunMine.tres";

	private const string SunMineScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Scene/TowerDefensePlantSunMine.tscn";

	private const string MagnetMinePacketPath = "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Packet/PlantMagnetMine.tres";

	private const string MagnetMineScenePath = "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Scene/TowerDefensePlantMagnetMine.tscn";

	private const string ImppultPacketPath = "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Packet/ZombieImppult.tres";

	private const string ImppultScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Scene/TowerDefenseZombieImppult.tscn";

	private static readonly Vector2I SunMineGrid = new Vector2I(4, 2);

	private static readonly Vector2I MagnetMineGrid = new Vector2I(4, 3);

	private static readonly Vector2I PotatoMineGrid = new Vector2I(7, 2);

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<TowerDefenseCharacter> _spawned = new List<TowerDefenseCharacter>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		bool verifyImmediateContact = Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify-immediate-contact") >= 0;
		bool verifySkillProtection = Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify-skill-protection") >= 0;
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		GargantuarImmatureMineSmashControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 25;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0163;
				}
				RegisterRealFixtures();
				control = new GargantuarImmatureMineSmashControlStub
				{
					Name = "GargantuarImmatureMineSmashControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM,
						izmManager = new TowerDefenseLevelIZMManagerConfig()
					}
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
				Check(manager.IsIZMMode(), "The fixture must run through the production IZM mode check.");
				if (verifySkillProtection)
				{
					await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
					await RunSkillProtectionScenario(control, "Chapter0/PotatoMine", "PotatoMine");
					await RunSkillProtectionScenario(control, "Chapter0/Squash", "Squash");
					await RunSkillProtectionScenario(control, "Chapter4/IceSquash", "IceSquash");
					await RunSkillProtectionScenario(control, "Chapter6/SquashKing", "SquashKing");
					await RunSkillProtectionScenario(control, "Star/SquashCandy", "SquashCandy");
					await RunSkillProtectionScenario(control, "Star/SquashVase", "SquashVase");
					await RunSkillProtectionScenario(control, "Chapter6/GloomSquash", "GloomSquash");
					await RunSkillProtectionScenario(control, "Chapter6/WallnutSquash", "WallnutSquash");
					await RunSkillProtectionScenario(control, "Chapter0/CherryBomb", "CherryBomb");
					await RunSkillProtectionScenario(control, "Chapter0/Jalapeno", "Jalapeno");
					await RunSkillProtectionScenario(control, "Chapter0/DoomShroom", "DoomShroom");
					await RunSkillProtectionScenario(control, "Chapter0/IceShroom", "IceShroom");
					await RunSkillProtectionScenario(control, "Other/JalaJoker", "JalaJoker", izm: true);
					await RunMineRiseContactScenario(control);
				}
				else if (!verifyImmediateContact)
				{
					await RunScenario(new MineScenario("PlantSunMine", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Packet/PlantSunMine.tres", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Scene/TowerDefensePlantSunMine.tscn", SunMineGrid));
					await RunScenario(new MineScenario("PlantMagnetMine", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Packet/PlantMagnetMine.tres", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Scene/TowerDefensePlantMagnetMine.tscn", MagnetMineGrid));
					await RunArmedScenario(new MineScenario("PlantSunMine", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Packet/PlantSunMine.tres", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Scene/TowerDefensePlantSunMine.tscn", SunMineGrid + Vector2I.Right));
					await RunArmedScenario(new MineScenario("PlantMagnetMine", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Packet/PlantMagnetMine.tres", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Scene/TowerDefensePlantMagnetMine.tscn", MagnetMineGrid + Vector2I.Right));
					await RunImppultArmedScenario(new MineScenario("PlantSunMine", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Packet/PlantSunMine.tres", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Scene/TowerDefensePlantSunMine.tscn", SunMineGrid + Vector2I.Right * 2));
					await RunImppultArmedScenario(new MineScenario("PlantMagnetMine", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Packet/PlantMagnetMine.tres", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Scene/TowerDefensePlantMagnetMine.tscn", MagnetMineGrid + Vector2I.Right * 2));
					await RunArmedPotatoTriggerScenario();
				}
				else
				{
					await RunImmediatePotatoContactScenario(control, "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", izm: false, naturalApproach: true);
					await RunImmediatePotatoContactScenario(control, "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn", izm: false, naturalApproach: true);
					await RunImmediatePotatoContactScenario(control, "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", izm: true);
					await RunImmediatePotatoContactScenario(control, "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn", izm: true);
				}
				goto end_IL_00ef;
				end_IL_0163:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGargantuarImmatureMineSmashRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00ef;
			}
			return;
			end_IL_00ef:;
		}
		finally
		{
			ReleaseSpawned();
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
		bool flag = _failures == 0 && (verifySkillProtection ? (_checks >= 50) : (_checks == (verifyImmediateContact ? 45 : 65)));
		string value2 = (verifySkillProtection ? "PLANT_SKILL_PROTECTION_RESULT" : (verifyImmediateContact ? "POTATO_IMMEDIATE_CONTACT_RESULT" : "GARGANTUAR_IMMATURE_MINE_SMASH_RESULT"));
		GD.Print($"{value2} passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunSkillProtectionScenario(TowerDefenseControlNew control, string folder, string plantName, bool izm = false)
	{
		ReleaseSpawned();
		((TowerDefenseLevelConfig)control.levelConfig).finishMethod = (izm ? TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM : TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE);
		await WaitFrames(3);
		string text = "res://Asset/Anime/Character/Plant/" + folder;
		string text2 = ((plantName == "DoomShroom") ? "DoomShroom" : ("Plant" + plantName));
		TowerDefensePlant plant = (await SpawnCharacter(text + "/Packet/" + text2 + ".tres", text + "/Scene/TowerDefensePlant" + plantName + ".tscn", PotatoMineGrid, occupyPlantCell: true)) as TowerDefensePlant;
		Check(GodotObject.IsInstanceValid(plant), plantName + " 必须由正式场景生成。");
		if (!GodotObject.IsInstanceValid(plant))
		{
			return;
		}
		plant.ProcessMode = ProcessModeEnum.Disabled;
		PotatoComponent potato = plant.componentManager.GetRuntime<PotatoComponent>();
		SquashComponent squash = plant.componentManager.GetRuntime<SquashComponent>();
		ExplodeComponent explode = plant.componentManager.GetRuntime<ExplodeComponent>();
		if (squash != null)
		{
			await WaitFrames(40);
		}
		TowerDefenseZombie zombie = (await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", PotatoMineGrid + Vector2I.Right, occupyPlantCell: false, plant.GetLogicalGlobalPosition() + new Vector2(150f, 0f))) as TowerDefenseZombie;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		zombie.SetLogicalGlobalPosition(plant.GetLogicalGlobalPosition() + new Vector2(35f, 0f));
		zombie.gridPos = PotatoMineGrid;
		zombie.InvalidateHitBoxBounds();
		AttackComponent attackComponent = zombie.attackComponent;
		if (potato != null || squash != null || izm)
		{
			Check(attackComponent.CanAttackOnce() && attackComponent.target == plant, plantName + " 发动前必须能被正常索敌。");
			double hitpoints = plant.instance.hitpoints;
			attackComponent.target = plant;
			attackComponent.AttackExecute(1.0);
			Check(plant.instance.hitpoints < hitpoints, plantName + " 发动前允许真实啃咬；我是僵尸模式的首口伤害仍能启动灰烬。");
		}
		if (potato != null)
		{
			potato.autoExplodeOnCharge = false;
			potato.ReadyCharge();
			Check(potato.isCharge && potato.rise, "土豆雷必须进入正式已出土状态。");
		}
		if (squash != null)
		{
			if (plant is TowerDefensePlantGloomSquash towerDefensePlantGloomSquash)
			{
				towerDefensePlantGloomSquash.ToSquash();
			}
			if (plant is TowerDefensePlantWallnutSquash towerDefensePlantWallnutSquash)
			{
				towerDefensePlantWallnutSquash.ToSquash();
			}
			squash.Execute(zombie);
			Check(squash.running, plantName + " 必须通过正式技能入口发动。");
		}
		if (explode != null && potato == null)
		{
			Check(explode.StateMachine.CurrentStateHandle.StableId == "explode.explode", plantName + " 必须在正式灰烬发动状态。");
		}
		double protectedHp = plant.instance.hitpoints;
		attackComponent.target = plant;
		attackComponent.AttackExecute(10.0);
		attackComponent.AttackDpsExecute(1.0, 10.0);
		Check(Math.Abs(plant.instance.hitpoints - protectedHp) < 0.001, plantName + " 发动后必须挡住之前缓存的单次和持续啃咬。");
		attackComponent.target = null;
		Check(!attackComponent.CanAttackOnce(), plantName + " 发动后不能重新被啃咬索敌。");
		Check(!plant.instance.invincible && plant.instance.canBeCollection && plant.HasHitBox && plant.instance.maskFlags != 0, plantName + " 发动后必须保留真实子弹受击资格。");
		if (squash != null)
		{
			squash.StateMachine.SendEvent(squash.jumpStateEvent);
			Check(squash.StateMachine.CurrentStateHandle.StableId == "squash.jump" && plant.HasHitBox && plant.instance.maskFlags != 0, plantName + " 起跳后仍必须保留子弹碰撞盒。");
		}
		await CaptureSkillProtection(plantName, "active", plant);
		TowerDefenseProjectileConfig projectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Pea/PeaDefault.tres", null, ResourceLoader.CacheMode.Reuse);
		BulletField instance = BulletField.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			instance = new BulletField();
			control.characterNode.AddChild(instance, forceReadableName: false, InternalMode.Disabled);
		}
		BulletFieldSpawnOverrides bulletFieldSpawnOverrides = new BulletFieldSpawnOverrides
		{
			baseDamageOverride = protectedHp + 100.0
		};
		Vector2 pos = plant.WorldHitRect.GetCenter() + new Vector2(70f, 0f);
		Vector2 velocity = new Vector2(-300f, 0f);
		int maskFlags = plant.instance.maskFlags;
		BulletFieldSpawnOverrides overrides = bulletFieldSpawnOverrides;
		int bulletIndex;
		bool condition = FireComponent.TryCreateProjectilePositionByConfig(zombie, null, 0.0, pos, velocity, projectileConfig, out bulletIndex, maskFlags, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, default(Vector2), overrides);
		Check(condition, plantName + " 必须生成一颗真实敌方豌豆。");
		for (int frame = 0; frame < 45; frame++)
		{
			if (!GodotObject.IsInstanceValid(plant))
			{
				break;
			}
			if (plant.die)
			{
				break;
			}
			if (plant.isDestroy)
			{
				break;
			}
			await WaitFrames(1);
		}
		Check(!GodotObject.IsInstanceValid(plant) || plant.die || plant.isDestroy, plantName + " 发动后必须被真实敌方子弹打死，不能仅扣至零血后继续保活。");
		await CaptureSkillProtection(plantName, "shot", plant);
		GD.Print($"PLANT_SKILL_PROTECTION plant={plantName} izm={izm} hpBefore={protectedHp} bullet={bulletIndex}");
	}

	private async Task RunMineRiseContactScenario(TowerDefenseControlNew control)
	{
		ReleaseSpawned();
		((TowerDefenseLevelConfig)control.levelConfig).finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE;
		await WaitFrames(3);
		TowerDefensePlant towerDefensePlant = (await SpawnCharacter("res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn", PotatoMineGrid, occupyPlantCell: true)) as TowerDefensePlant;
		towerDefensePlant.ProcessMode = ProcessModeEnum.Disabled;
		PotatoComponent potato = towerDefensePlant.componentManager.GetRuntime<PotatoComponent>();
		Vector2 position = towerDefensePlant.GetLogicalGlobalPosition();
		TowerDefenseZombie zombie = (await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", PotatoMineGrid, occupyPlantCell: false, position + new Vector2(35f, 0f))) as TowerDefenseZombie;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		TowerDefenseCharacter towerDefenseCharacter = await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", PotatoMineGrid + Vector2I.Down, occupyPlantCell: false, position + new Vector2(200f, 76f));
		towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
		potato.attackComponent.target = towerDefenseCharacter;
		potato.attackComponent.timer = 10.0;
		potato.attackComponent.checkIntrevalNow = 100;
		int explosions = 0;
		double zombieHp = zombie.instance.hitpoints;
		potato.explodeComponent.OnExplodeStart += delegate
		{
			explosions++;
		};
		zombie.ProcessMode = ProcessModeEnum.Inherit;
		potato.ReadyCharge();
		Check(potato.over && explosions == 1, "贴身僵尸必须在出土当次引爆地雷，过期目标和攻击冷却都不能拖延。");
		await WaitFrames(3);
		Check(!GodotObject.IsInstanceValid(zombie) || zombie.die || zombie.instance.hitpoints < zombieHp, "出土当次起爆必须真实伤害贴身僵尸。");
	}

	private async Task CaptureSkillProtection(string plantName, string phase, TowerDefensePlant plant)
	{
		string directory = OS.GetEnvironment("PLANT_SKILL_CAPTURE_DIR");
		if (string.IsNullOrEmpty(directory) || DisplayServer.GetName() == "headless")
		{
			return;
		}
		Directory.CreateDirectory(directory);
		Label label = new Label
		{
			Text = $"{plantName} / {phase}\nBite protection during skill; enemy pea damage enabled\nHP: {(GodotObject.IsInstanceValid(plant) ? plant.instance.hitpoints : 0.0)}",
			Position = new Vector2(36f, 32f)
		};
		label.AddThemeFontSizeOverride("font_size", 24);
		AddChild(label, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		image.SavePng(Path.Combine(directory, plantName + "-" + phase + ".png"));
		label.QueueFree();
	}

	private async Task RunImmediatePotatoContactScenario(TowerDefenseControlNew control, string zombiePacketPath, string zombieScenePath, bool izm, bool naturalApproach = false)
	{
		ReleaseSpawned();
		((TowerDefenseLevelConfig)control.levelConfig).finishMethod = (izm ? TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM : TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE);
		await WaitFrames(3);
		TowerDefensePlant mine = (await SpawnCharacter("res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn", PotatoMineGrid, occupyPlantCell: true)) as TowerDefensePlant;
		PotatoComponent potato = mine?.componentManager?.GetRuntime<PotatoComponent>();
		BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest = this;
		int condition;
		if (potato != null && !potato.IsReleased)
		{
			AttackComponent attackComponent = potato.attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				ExplodeComponent explodeComponent = potato.explodeComponent;
				condition = ((explodeComponent != null && !explodeComponent.IsReleased) ? 1 : 0);
				goto IL_01bc;
			}
		}
		condition = 0;
		goto IL_01bc;
		IL_01bc:
		bugOverviewGargantuarImmatureMineSmashRuntimeTest.Check((byte)condition != 0, "Contact timing must use the real Potato Mine component chain.");
		if ((potato?.IsReleased ?? true) || (potato.attackComponent?.IsReleased ?? true) || (potato.explodeComponent?.IsReleased ?? true))
		{
			return;
		}
		if (!izm)
		{
			Check(!potato.rise && !potato.isCharge && potato.StateMachine.CurrentStateHandle.StableId == "potato.ready", "A newly planted Potato Mine must still wait for its normal arming phase.");
			potato.ReadyRise();
			for (int frame = 0; frame < 120; frame++)
			{
				if (potato.isCharge)
				{
					break;
				}
				await WaitFrames(1);
			}
		}
		Check(potato.rise && potato.isCharge && potato.StateMachine.CurrentStateHandle.StableId == "potato.charge", "The production state machine must fully arm the Potato Mine before contact.");
		Vector2 minePosition = mine.GetLogicalGlobalPosition();
		TowerDefenseZombie zombie = (await SpawnCharacter(zombiePacketPath, zombieScenePath, PotatoMineGrid + Vector2I.Right * 2, occupyPlantCell: false, minePosition + new Vector2(180f, 0f))) as TowerDefenseZombie;
		BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest2 = this;
		int condition2;
		if (GodotObject.IsInstanceValid(zombie))
		{
			AttackComponent attackComponent2 = zombie.attackComponent;
			condition2 = ((attackComponent2 != null && !attackComponent2.IsReleased) ? 1 : 0);
		}
		else
		{
			condition2 = 0;
		}
		bugOverviewGargantuarImmatureMineSmashRuntimeTest2.Check((byte)condition2 != 0, "Contact timing must use a real biting or smashing zombie.");
		if (!GodotObject.IsInstanceValid(zombie) || (zombie.attackComponent?.IsReleased ?? true))
		{
			return;
		}
		AttackComponent attackComponent3 = potato.attackComponent;
		attackComponent3.timer = 0.0;
		potato.ChargeProcessing(1.0 / 60.0);
		Check(!potato.over, "An armed Potato Mine must not explode before the zombie reaches its contact shape.");
		attackComponent3.timer = attackComponent3.attackInterval;
		attackComponent3.checkIntrevalNow = attackComponent3.checkIntreval;
		int explosionCount = 0;
		double mineHealthBefore = mine.instance.hitpoints;
		double mineHealthAtExplosion = 0.0 / 0.0;
		potato.explodeComponent.OnExplodeStart += delegate
		{
			explosionCount++;
			mineHealthAtExplosion = mine.instance.hitpoints;
		};
		double zombieHealthBefore = zombie.instance.hitpoints;
		zombie.gridPos = PotatoMineGrid + Vector2I.Down;
		zombie.SetLogicalGlobalPosition(minePosition + new Vector2(35f, 0f));
		zombie.InvalidateHitBoxBounds();
		potato.ChargeProcessing(1.0 / 60.0);
		Check(!potato.over && explosionCount == 0, "Immediate contact checks must still reject zombies in another lane.");
		zombie.gridPos = PotatoMineGrid;
		if (naturalApproach)
		{
			zombie.SetLogicalGlobalPosition(minePosition + new Vector2(90f, 0f));
		}
		zombie.InvalidateHitBoxBounds();
		if (naturalApproach)
		{
			for (int frame = 0; frame < 180; frame++)
			{
				if (potato.over)
				{
					break;
				}
				if (!GodotObject.IsInstanceValid(mine))
				{
					break;
				}
				if (mine.isDestroy)
				{
					break;
				}
				await WaitFrames(1);
			}
		}
		else
		{
			potato.ChargeProcessing(1.0 / 60.0);
		}
		Check(potato.over && explosionCount == 1, naturalApproach ? "A naturally approaching zombie must trigger exactly one contact explosion." : "Contact must trigger exactly one explosion in the same callback even while both attack cooldowns are positive.");
		Check(!GodotObject.IsInstanceValid(mine) || mine.isDestroy || mine.IsQueuedForDeletion(), "The triggered Potato Mine must leave the battlefield immediately, before a bite or smash can resolve.");
		Check(Math.Abs(mineHealthAtExplosion - mineHealthBefore) < 0.001, "Immediate detonation must preserve the mine's health until it is consumed by its own explosion.");
		potato.ChargeProcessing(1.0 / 60.0);
		Check(explosionCount == 1, "Repeated contact callbacks must not detonate the same Potato Mine twice.");
		await WaitFrames(3);
		Check(!GodotObject.IsInstanceValid(zombie) || zombie.instance.hitpoints < zombieHealthBefore || zombie.die || zombie.nearDie, "Immediate detonation must deliver real explosion damage to the contacting zombie.");
		GD.Print($"POTATO_IMMEDIATE_CONTACT izm={izm} natural={naturalApproach} zombie={zombiePacketPath} explosions={explosionCount} mineHealth={mineHealthAtExplosion}/{mineHealthBefore}");
	}

	private async Task RunArmedPotatoTriggerScenario()
	{
		ReleaseSpawned();
		await WaitFrames(3);
		TowerDefensePlant mine = (await SpawnCharacter("res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn", PotatoMineGrid, occupyPlantCell: true)) as TowerDefensePlant;
		Check(GodotObject.IsInstanceValid(mine) && mine.config?.name == "PlantPotatoMine", "The real Potato Mine must spawn for the Gargantuar trigger scenario.");
		PotatoComponent potato = mine?.componentManager?.GetRuntime<PotatoComponent>();
		BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest = this;
		int condition;
		if (potato != null && !potato.IsReleased)
		{
			AttackComponent attackComponent = potato.attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				ExplodeComponent explodeComponent = potato.explodeComponent;
				condition = ((explodeComponent != null && !explodeComponent.IsReleased) ? 1 : 0);
				goto IL_01c4;
			}
		}
		condition = 0;
		goto IL_01c4;
		IL_01c4:
		bugOverviewGargantuarImmatureMineSmashRuntimeTest.Check((byte)condition != 0, "The real Potato Mine trigger, attack, and explosion runtimes must be active.");
		if (!GodotObject.IsInstanceValid(mine) || (potato?.IsReleased ?? true) || (potato.attackComponent?.IsReleased ?? true) || (potato.explodeComponent?.IsReleased ?? true))
		{
			return;
		}
		potato.rise = true;
		potato.ChargeEntered();
		Check(potato.isCharge && potato.autoExplodeOnCharge && mine.instance.invincibleSmash, "The real Potato Mine must be armed and protected from direct smash deletion.");
		Vector2 globalPosition = mine.GlobalPosition;
		TowerDefenseZombieGargantuar gargantuar = (await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn", PotatoMineGrid, occupyPlantCell: false, globalPosition + new Vector2(45f, 0f))) as TowerDefenseZombieGargantuar;
		Check(GodotObject.IsInstanceValid(gargantuar) && gargantuar.config?.name == "ZombieGargantuar", "The real Gargantuar must spawn for the Potato Mine trigger scenario.");
		BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest2 = this;
		int condition2;
		if (gargantuar != null && gargantuar.attackComponent?.IsReleased == false)
		{
			GargantuarSmashComponent gargantuarSmashComponent = gargantuar.gargantuarSmashComponent;
			condition2 = ((gargantuarSmashComponent != null && !gargantuarSmashComponent.IsReleased) ? 1 : 0);
		}
		else
		{
			condition2 = 0;
		}
		bugOverviewGargantuarImmatureMineSmashRuntimeTest2.Check((byte)condition2 != 0, "The real Gargantuar smash chain must be active for the Potato Mine trigger scenario.");
		if (!GodotObject.IsInstanceValid(gargantuar) || (gargantuar.attackComponent?.IsReleased ?? true) || (gargantuar.gargantuarSmashComponent?.IsReleased ?? true))
		{
			return;
		}
		await WaitFrames(10);
		if (GodotObject.IsInstanceValid(mine) && !mine.IsQueuedForDeletion())
		{
			gargantuar.attackComponent.target = mine;
			gargantuar.Attack();
			await WaitFrames(1);
			for (int smashIndex = 0; smashIndex < 3; smashIndex++)
			{
				if (!GodotObject.IsInstanceValid(mine))
				{
					break;
				}
				if (mine.IsQueuedForDeletion())
				{
					break;
				}
				gargantuar.gargantuarSmashComponent.SmashAttack();
				gargantuar.AnimeCompleted(gargantuar.attackAnimeClip);
				await WaitFrames(2);
			}
		}
		Check(potato.over, "The armed Potato Mine must enter its one-shot explosion state when the Gargantuar reaches it.");
		Check(!GodotObject.IsInstanceValid(mine) || mine.IsQueuedForDeletion() || mine.die || mine.nearDie || mine.isDestroy, "The triggered Potato Mine must leave the battlefield after exploding.");
		Check(!GodotObject.IsInstanceValid(gargantuar.attackComponent.target) || gargantuar.attackComponent.target != mine, "The Gargantuar must release the exploded Potato Mine instead of repeating the smash forever.");
	}

	private async Task RunImppultArmedScenario(MineScenario scenario)
	{
		TowerDefensePlant mine = (await SpawnCharacter(scenario.PacketPath, scenario.ScenePath, scenario.Grid, occupyPlantCell: true)) as TowerDefensePlant;
		Check(GodotObject.IsInstanceValid(mine) && mine.config?.name == scenario.Name, "Imppult armed " + scenario.Name + " must spawn through its real packet and scene.");
		PotatoComponent potatoComponent = mine?.componentManager?.GetRuntime<PotatoComponent>();
		Check(potatoComponent != null && !potatoComponent.IsReleased, "Imppult armed " + scenario.Name + " must expose the real PotatoComponent runtime.");
		if (GodotObject.IsInstanceValid(mine) && !(potatoComponent?.IsReleased ?? true))
		{
			potatoComponent.autoExplodeOnCharge = false;
			potatoComponent.rise = true;
			potatoComponent.ChargeEntered();
			Check(potatoComponent.isCharge && potatoComponent.rise && mine.instance.invincibleSmash, "Imppult armed " + scenario.Name + " must own smash invincibility after rise and charge.");
			TowerDefenseZombieImppult towerDefenseZombieImppult = (await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Packet/ZombieImppult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Scene/TowerDefenseZombieImppult.tscn", scenario.Grid, occupyPlantCell: false)) as TowerDefenseZombieImppult;
			Check(GodotObject.IsInstanceValid(towerDefenseZombieImppult) && towerDefenseZombieImppult.config?.name == "ZombieImppult", "The real ZombieImppult must spawn for armed " + scenario.Name + ".");
			CatapultComponent catapultComponent = towerDefenseZombieImppult?.componentManager?.GetRuntime<CatapultComponent>();
			AttackComponent attackComponent = towerDefenseZombieImppult?.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
			Check(catapultComponent != null && !catapultComponent.IsReleased && catapultComponent.Alive && attackComponent != null && !attackComponent.IsReleased && attackComponent.Alive && attackComponent.attackType == "Smash", "The real ZombieImppult contact-smash component chain must be active for " + scenario.Name + ".");
			if (GodotObject.IsInstanceValid(towerDefenseZombieImppult) && catapultComponent != null && !catapultComponent.IsReleased && catapultComponent.Alive && attackComponent != null && !attackComponent.IsReleased && attackComponent.Alive)
			{
				towerDefenseZombieImppult.gridPos = scenario.Grid;
				towerDefenseZombieImppult.GlobalPosition = mine.GlobalPosition + new Vector2(35f, 0f);
				attackComponent.target = mine;
				Check(attackComponent.GetTarget() == mine, "Armed " + scenario.Name + " must be the acquired real ZombieImppult contact target.");
				double hitpointsBefore = mine.instance.hitpoints;
				attackComponent.target = mine;
				catapultComponent.PhysicsProcess(0.0, TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
				await WaitFrames(2);
				Check(Math.Abs(mine.instance.hitpoints - hitpointsBefore) < 0.001 && !mine.nearDie && !mine.die && !mine.isDestroy, "Armed " + scenario.Name + " must survive the real ZombieImppult contact SmashAttackCell impact.");
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(scenario.Grid);
				Check(GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Contains(mine), "Armed " + scenario.Name + " must remain registered in its real map cell after ZombieImppult contact.");
			}
		}
	}

	private async Task RunArmedScenario(MineScenario scenario)
	{
		TowerDefensePlant mine = (await SpawnCharacter(scenario.PacketPath, scenario.ScenePath, scenario.Grid, occupyPlantCell: true)) as TowerDefensePlant;
		Check(GodotObject.IsInstanceValid(mine) && mine.config?.name == scenario.Name, "Armed " + scenario.Name + " must spawn through its real packet and scene.");
		PotatoComponent potatoComponent = mine?.componentManager?.GetRuntime<PotatoComponent>();
		Check(potatoComponent != null && !potatoComponent.IsReleased, "Armed " + scenario.Name + " must expose the real PotatoComponent runtime.");
		if (GodotObject.IsInstanceValid(mine) && !(potatoComponent?.IsReleased ?? true))
		{
			potatoComponent.autoExplodeOnCharge = false;
			potatoComponent.rise = true;
			potatoComponent.ChargeEntered();
			Check(potatoComponent.isCharge && potatoComponent.rise && mine.instance.invincibleSmash, "Armed " + scenario.Name + " must enable smash invincibility only after the real rise and charge states.");
			TowerDefenseZombieGargantuar towerDefenseZombieGargantuar = (await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn", scenario.Grid, occupyPlantCell: false)) as TowerDefenseZombieGargantuar;
			Check(GodotObject.IsInstanceValid(towerDefenseZombieGargantuar) && towerDefenseZombieGargantuar.config?.name == "ZombieGargantuar", "The real Gargantuar must spawn for armed " + scenario.Name + ".");
			BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest = this;
			int condition;
			if (towerDefenseZombieGargantuar != null && towerDefenseZombieGargantuar.attackComponent?.IsReleased == false)
			{
				GargantuarSmashComponent gargantuarSmashComponent = towerDefenseZombieGargantuar.gargantuarSmashComponent;
				condition = ((gargantuarSmashComponent != null && !gargantuarSmashComponent.IsReleased) ? 1 : 0);
			}
			else
			{
				condition = 0;
			}
			bugOverviewGargantuarImmatureMineSmashRuntimeTest.Check((byte)condition != 0, "The real Gargantuar smash chain must be active for armed " + scenario.Name + ".");
			if (GodotObject.IsInstanceValid(towerDefenseZombieGargantuar) && !(towerDefenseZombieGargantuar.attackComponent?.IsReleased ?? true) && !(towerDefenseZombieGargantuar.gargantuarSmashComponent?.IsReleased ?? true))
			{
				towerDefenseZombieGargantuar.gridPos = scenario.Grid;
				towerDefenseZombieGargantuar.GlobalPosition = mine.GlobalPosition + new Vector2(45f, 0f);
				towerDefenseZombieGargantuar.attackComponent.target = mine;
				Check(towerDefenseZombieGargantuar.attackComponent.GetTarget() == mine, "Armed " + scenario.Name + " must be the acquired real Gargantuar smash target.");
				double hitpointsBefore = mine.instance.hitpoints;
				towerDefenseZombieGargantuar.attackComponent.target = mine;
				towerDefenseZombieGargantuar.gargantuarSmashComponent.SmashAttack();
				await WaitFrames(2);
				Check(Math.Abs(mine.instance.hitpoints - hitpointsBefore) < 0.001 && !mine.nearDie && !mine.die && !mine.isDestroy, "Armed " + scenario.Name + " must survive the real Gargantuar SmashAttackCell impact.");
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(scenario.Grid);
				Check(GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Contains(mine), "Armed " + scenario.Name + " must remain registered in its real map cell after Gargantuar smash.");
			}
		}
	}

	private async Task RunScenario(MineScenario scenario)
	{
		TowerDefensePlant mine = (await SpawnCharacter(scenario.PacketPath, scenario.ScenePath, scenario.Grid, occupyPlantCell: true)) as TowerDefensePlant;
		Check(GodotObject.IsInstanceValid(mine) && mine.config?.name == scenario.Name, scenario.Name + " must spawn through its real packet and scene.");
		PotatoComponent potatoComponent = mine?.componentManager?.GetRuntime<PotatoComponent>();
		Check(potatoComponent != null && !potatoComponent.IsReleased, scenario.Name + " must expose the real PotatoComponent runtime.");
		if (GodotObject.IsInstanceValid(mine) && !(potatoComponent?.IsReleased ?? true))
		{
			potatoComponent.autoExplodeOnCharge = false;
			potatoComponent.rise = false;
			potatoComponent.ChargeEntered();
			Check(potatoComponent.isCharge && !potatoComponent.rise, scenario.Name + " must reproduce the IZM immature charge state.");
			Check(!mine.instance.invincibleSmash, scenario.Name + " must not keep smash invincibility before rise.");
			TowerDefenseZombieGargantuar gargantuar = (await SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn", scenario.Grid, occupyPlantCell: false)) as TowerDefenseZombieGargantuar;
			Check(GodotObject.IsInstanceValid(gargantuar) && gargantuar.config?.name == "ZombieGargantuar", "The real Gargantuar must spawn for " + scenario.Name + ".");
			BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest = this;
			int condition;
			if (gargantuar != null && gargantuar.attackComponent?.IsReleased == false)
			{
				GargantuarSmashComponent gargantuarSmashComponent = gargantuar.gargantuarSmashComponent;
				condition = ((gargantuarSmashComponent != null && !gargantuarSmashComponent.IsReleased) ? 1 : 0);
			}
			else
			{
				condition = 0;
			}
			bugOverviewGargantuarImmatureMineSmashRuntimeTest.Check((byte)condition != 0, "The real Gargantuar smash chain must be active for " + scenario.Name + ".");
			if (GodotObject.IsInstanceValid(gargantuar) && !(gargantuar.attackComponent?.IsReleased ?? true) && !(gargantuar.gargantuarSmashComponent?.IsReleased ?? true))
			{
				gargantuar.gridPos = scenario.Grid;
				gargantuar.GlobalPosition = mine.GlobalPosition + new Vector2(45f, 0f);
				gargantuar.attackComponent.target = mine;
				TowerDefenseCharacter target = gargantuar.attackComponent.GetTarget();
				Check(target == mine, scenario.Name + " must be the acquired real Gargantuar smash target.");
				gargantuar.attackComponent.target = mine;
				gargantuar.Attack();
				await WaitFrames(1);
				Check(gargantuar.CurrentStateHandle?.StableId == "zombie.attack" && gargantuar.sprite?.clip == gargantuar.attackAnimeClip, scenario.Name + " must enter the authored Smash animation before the blowback race.");
				gargantuar.gargantuarSmashComponent.SmashAttack();
				gargantuar.BlowBack(1.0, 1.0);
				await WaitFrames(2);
				Check(mine.instance.hitpoints <= 0.0 || mine.nearDie || mine.die || mine.isDestroy, scenario.Name + " must be crushed while it is still immature.");
				BugOverviewGargantuarImmatureMineSmashRuntimeTest bugOverviewGargantuarImmatureMineSmashRuntimeTest2 = this;
				BlowBackComponent blowBackComponent = gargantuar.blowBackComponent;
				bugOverviewGargantuarImmatureMineSmashRuntimeTest2.Check(blowBackComponent != null && !blowBackComponent.IsReleased && gargantuar.blowBackComponent.blowBack, scenario.Name + " must keep the production BlowBack lease active after the smash impact.");
				await WaitFrames(2);
				Check(gargantuar.CurrentStateHandle?.StableId == "zombie.walk" && gargantuar.sprite?.clip != gargantuar.attackAnimeClip && !GodotObject.IsInstanceValid(gargantuar.attackComponent.target), scenario.Name + " must leave Smash immediately after blowback interrupts the immature mine impact.");
			}
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			isNight = true,
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y)
		});
		for (int i = 1; i <= gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
		towerDefenseBattleFeatureMap.rect = new Rect2(new Vector2(-100f, -100f), new Vector2(1200f, 700f));
		return towerDefenseBattleFeatureMap;
	}

	private async Task<TowerDefenseCharacter> SpawnCharacter(string packetPath, string scenePath, Vector2I grid, bool occupyPlantCell, Vector2? globalPositionOverride = null)
	{
		TowerDefensePacketConfig packet = LoadPacket(packetPath);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(packet) || !GodotObject.IsInstanceValid(packedScene))
		{
			return null;
		}
		TowerDefenseCharacter character = packedScene.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(character))
		{
			return null;
		}
		character.packet = packet;
		character.gridPos = grid;
		character.cost = packet.characterConfig?.cost ?? 0;
		character.GlobalPosition = globalPositionOverride ?? TowerDefenseManager.GetMapCellPlantPos(grid);
		if (GodotObject.IsInstanceValid(character.cell))
		{
			character.groundHeight = character.cell.GetGroundHeight();
			character.z = character.groundHeight;
		}
		TowerDefenseManager.GetCharacterNode().AddChild(character, forceReadableName: false, InternalMode.Disabled);
		_spawned.Add(character);
		await WaitFrames(5);
		if (!GodotObject.IsInstanceValid(character))
		{
			return null;
		}
		if (occupyPlantCell)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(grid);
			if (GodotObject.IsInstanceValid(mapCell) && !mapCell.characterList.Contains(character))
			{
				mapCell.CharacterPlant(packet, character, noLimit: true);
			}
		}
		TowerDefenseManager.Instance.CharacterRegister(character);
		character.InvalidateHitBoxBounds();
		return character;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		Register("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn");
		Register("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		Register("PlantPotatoMine", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn");
		Register("PlantSunMine", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Packet/PlantSunMine.tres", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Scene/TowerDefensePlantSunMine.tscn");
		Register("PlantMagnetMine", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Packet/PlantMagnetMine.tres", "res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Scene/TowerDefensePlantMagnetMine.tscn");
		Register("ZombieImppult", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Packet/ZombieImppult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Scene/TowerDefenseZombieImppult.tscn");
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

	private void ReleaseSpawned()
	{
		foreach (TowerDefenseCharacter item in _spawned)
		{
			if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
			{
				item.QueueFree();
			}
		}
		_spawned.Clear();
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
			GD.PushError("[BugOverviewGargantuarImmatureMineSmashRuntimeTest] " + message);
		}
	}
}
