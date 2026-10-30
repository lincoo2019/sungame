using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class SpikeRockShieldDamageRuntimeTest : Node
{
	private const string SpikeRockPacketPath = "res://Asset/Anime/Character/Plant/Cover/SpikeRock/Packet/PlantSpikeRock.tres";

	private const string ShieldPacketPath = "res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres";

	private const string GargantuarPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres";

	private const string ChomperPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalChomper.tres";

	private const string ZamboniPacketPath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres";

	private static readonly Vector2I TargetGrid = new Vector2I(5, 3);

	private readonly List<TowerDefenseCharacter> _spawned = new List<TowerDefenseCharacter>();

	private SpikeRockShieldDamageControl _control;

	private int _checks;

	private int _failures;

	private int _cases;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNum = manager.gridNum;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap map = null;
		try
		{
			_ = 21;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				_control = new SpikeRockShieldDamageControl
				{
					Name = "ShieldDamageControl",
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig(),
					characterNode = new Node2D()
				};
				AddChild(_control, forceReadableName: false, InternalMode.Disabled);
				_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = _control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					mapIceCap = new Node2D()
				};
				_control.AddChild(mapControl.mapIceCap, forceReadableName: false, InternalMode.Disabled);
				map = CreateMap(mapControl, manager.gridNum);
				map.control = _control;
				_control.featureDictionary[new StringName("Map")] = map;
				await RunCase("爆炸优先消耗护盾", 1000.0, 700.0, 450.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(300.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				});
				await RunCase("爆炸破盾后本体限伤", 100.0, 0.0, 400.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(300.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				});
				await RunCase("爆炸破盾后仅扣剩余伤害", 100.0, 0.0, 430.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(120.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				});
				await RunCase("爆炸恰好耗尽护盾", 300.0, 0.0, 450.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(300.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				});
				await RunCase("爆炸无盾仍限伤50", 0.0, 0.0, 400.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(300.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				});
				await RunCase("致命爆炸消耗一层", 2000.0, 1000.0, 450.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(1800.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				});
				await RunCase("直接粉碎消耗一层", 2000.0, 1000.0, 450.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.SmashHurt(10000.0, playSplatAudio: false);
				});
				await RunCase("粉碎无盾仍限伤50", 0.0, 0.0, 400.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.SmashHurt(10000.0, playSplatAudio: false);
				});
				await RunCase("普通伤害不受特殊限伤影响", 100.0, 0.0, 250.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.Hurt(300.0, playSplatAudio: false);
				});
				await RunCase("有限伤害先扣护盾", 1000.0, 700.0, 450.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.Hurt(300.0, playSplatAudio: false, default(Vector2), createDamagePart: true, 50.0);
				});
				await RunCase("有限伤害溢出仍限伤50", 100.0, 0.0, 400.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.Hurt(300.0, playSplatAudio: false, default(Vector2), createDamagePart: true, 50.0);
				});
				await RunCase("有限伤害只扣真实溢出", 100.0, 0.0, 430.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.Hurt(120.0, playSplatAudio: false, default(Vector2), createDamagePart: true, 50.0);
				});
				await RunCase("伤害倍率和固定减伤保持生效", 100.0, 0.0, 425.0, ApplyScaledDamage);
				await RunCase("无盾倍率和固定减伤保持生效", 0.0, 0.0, 425.0, ApplyScaledDamage);
				await RunCase("敌方护盾不保护地刺王", 1000.0, 1000.0, 400.0, delegate(TowerDefensePlantSpikeRock body, TowerDefenseZombie _)
				{
					body.ExplodeHurt(300.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
				}, null, opposingShield: true);
				await RunCase("巨人砸整格消耗一层", 2000.0, 1000.0, 450.0, SmashCell, "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres");
				await RunCase("巨人无盾仍限伤50", 0.0, 0.0, 400.0, SmashCell, "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres");
				await RunCase("大嘴吞咬消耗一层", 2000.0, 1000.0, 450.0, Bite, "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalChomper.tres");
				await RunCase("大嘴吞咬无盾仍限伤50", 0.0, 0.0, 400.0, Bite, "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalChomper.tres");
				await RunCase("冰车碾压消耗一层", 2000.0, 1000.0, 450.0, DriveOverSpike, "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres");
				await RunCase("冰车无盾仍限伤50", 0.0, 0.0, 400.0, DriveOverSpike, "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"SPIKE_ROCK_SHIELD_EXCEPTION {value}");
			}
		}
		finally
		{
			ReleaseCharacters();
			await WaitFrames(3);
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousBegin;
			manager.gridSize = previousSize;
			manager.gridNum = previousNum;
			map?.Destroy();
			mapControl?.Free();
			_control?.QueueFree();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _cases == 21;
		GD.Print($"SPIKE_ROCK_SHIELD_DAMAGE_RESULT passed={flag} cases={_cases} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunCase(string name, double shieldHitpoints, double expectedShield, double expectedBody, Action<TowerDefensePlantSpikeRock, TowerDefenseZombie> damage, string attackerPacketPath = null, bool opposingShield = false)
	{
		ReleaseCharacters();
		await WaitFrames(3);
		TowerDefensePlantSpikeRock body = Spawn<TowerDefensePlantSpikeRock>("res://Asset/Anime/Character/Plant/Cover/SpikeRock/Packet/PlantSpikeRock.tres");
		TowerDefenseItemSheild shield = ((shieldHitpoints > 0.0) ? Spawn<TowerDefenseItemSheild>("res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres") : null);
		TowerDefenseZombie attacker = ((attackerPacketPath == null) ? null : Spawn<TowerDefenseZombie>(attackerPacketPath));
		await WaitFrames(3);
		Check(body.instance.hitpoints == 450.0 && body.instance.smashHurt == 50.0 && body.instance.explosionHurt == 50.0 && body.instance.spikeHurt == 50.0 && body.instance.biteHurt == 50.0, name + "：必须使用正式地刺王的血量与限伤配置");
		if (shield != null)
		{
			shield.shieldHitpoints = shieldHitpoints;
			shield.instance.hypnoses = opposingShield;
			Check(body.cell.itemShield == shield, name + "：护盾必须注册在地刺王所在格子");
		}
		int bodyDamage = 0;
		body.OnBodyHurt += delegate(int amount)
		{
			bodyDamage += amount;
		};
		_control.isGameRunning = true;
		damage(body, attacker);
		_control.isGameRunning = false;
		double num = shield?.instance.hitpoints ?? 0.0;
		double hitpoints = body.instance.hitpoints;
		Check(Math.Abs(num - expectedShield) < 0.001, $"{name}：护盾预期 {expectedShield}，实际 {num}");
		Check(Math.Abs(hitpoints - expectedBody) < 0.001, $"{name}：本体预期 {expectedBody}，实际 {hitpoints}");
		Check(bodyDamage == (int)(450.0 - expectedBody), $"{name}：本体受击事件预期 {450.0 - expectedBody}，实际 {bodyDamage}");
		_cases++;
		GD.Print($"SPIKE_ROCK_SHIELD_CASE name={name} shield={num} body={hitpoints} damageEvents={bodyDamage}");
	}

	private static void SmashCell(TowerDefensePlantSpikeRock body, TowerDefenseZombie attacker)
	{
		attacker.attackComponent.target = body;
		attacker.attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)attacker.config).smashAttack);
	}

	private static void Bite(TowerDefensePlantSpikeRock body, TowerDefenseZombie attacker)
	{
		body.instance.maskFlags |= attacker.instance.collisionFlags;
		((TowerDefenseZombieNormalChomper)attacker).chomperComponent.BitCharacter(body);
	}

	private void DriveOverSpike(TowerDefensePlantSpikeRock body, TowerDefenseZombie attacker)
	{
		attacker.attackComponent.target = body;
		attacker.WalkProcessing(0.0);
		Check(attacker.die || attacker.nearDie || attacker.isDestroy, "冰车必须实际触发地刺爆胎");
	}

	private static void ApplyScaledDamage(TowerDefensePlantSpikeRock body, TowerDefenseZombie attacker)
	{
		body.instance.dealHurtScale = 0.5;
		body.instance.dealHurtReduce = 5.0;
		body.Hurt(400.0, playSplatAudio: false, default(Vector2), createDamagePart: true, 60.0);
	}

	private T Spawn<T>(string path) where T : TowerDefenseCharacter
	{
		if (!(GD.Load<TowerDefensePacketConfig>(path).Plant(TargetGrid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) is T val))
		{
			throw new InvalidOperationException("正式卡片放置失败：" + path);
		}
		val.ProcessMode = ProcessModeEnum.Disabled;
		_spawned.Add(val);
		return val;
	}

	private static TowerDefenseBattleFeatureMap CreateMap(TowerDefenseMapControl control, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = control,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		control.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private void ReleaseCharacters()
	{
		if (_control != null)
		{
			_control.isGameRunning = false;
		}
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
			GD.PushError(message);
		}
	}
}
