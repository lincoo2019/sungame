using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class GatlingZamboniHolographicBaseRuntimeTest : Node
{
	private const string ZamboniScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/GatlingPea/TowerDefenseZombieZamboniGatlingPea.tscn";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter9/SunFlowerQX/Scene/TowerDefensePlantSunFlowerQX.tscn";

	private const string NormalSunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private readonly List<int> _spawnedBullets = new List<int>();

	private int _checks;

	private int _failures;

	private GatlingZamboniHolographicBaseControl _control;

	private BulletField _bulletField;

	private ulong _simulationFrame;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousGridBegin = manager.gridBeginPos;
		Vector2 previousGridSize = manager.gridSize;
		Vector2I previousGridNum = manager.gridNum;
		ProjectileUpdateManager projectileManager = ProjectileUpdateManager.Instance;
		ProcessModeEnum previousProjectileProcessMode = projectileManager.ProcessMode;
		TowerDefenseBattleFeatureMap map = null;
		TowerDefenseMapControl mapControl = null;
		bool ownsBulletField = false;
		try
		{
			manager.gridBeginPos = Vector2.Zero;
			manager.gridSize = new Vector2(100f, 76f);
			manager.gridNum = new Vector2I(9, 5);
			_control = new GatlingZamboniHolographicBaseControl
			{
				isGameRunning = false,
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig(),
				characterNode = new Node2D()
			};
			AddChild(_control, forceReadableName: false, InternalMode.Disabled);
			_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
			manager.currentControl = _control;
			mapControl = new TowerDefenseMapControl();
			map = CreateMap(mapControl, manager);
			_control.featureDictionary["Map"] = map;
			TowerDefenseProjectileRegistry.Init();
			projectileManager.ProcessMode = ProcessModeEnum.Disabled;
			_bulletField = BulletField.Instance;
			if (!GodotObject.IsInstanceValid(_bulletField))
			{
				_bulletField = new BulletField();
				AddChild(_bulletField, forceReadableName: false, InternalMode.Disabled);
				ownsBulletField = true;
			}
			_bulletField.OnBulletSpawned += _spawnedBullets.Add;
			TowerDefensePlantSunFlowerQX sunflower = Spawn<TowerDefensePlantSunFlowerQX>("res://Asset/Anime/Character/Plant/Chapter9/SunFlowerQX/Scene/TowerDefensePlantSunFlowerQX.tscn", new Vector2I(4, 3));
			TowerDefensePlant normalSunflower = Spawn<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn", new Vector2I(2, 3));
			TowerDefenseZombieZamboniGatlingPea zamboni = Spawn<TowerDefenseZombieZamboniGatlingPea>("res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/GatlingPea/TowerDefenseZombieZamboniGatlingPea.tscn", new Vector2I(7, 3));
			await WaitFrames(3);
			foreach (Node child in _control.characterNode.GetChildren())
			{
				child.ProcessMode = ProcessModeEnum.Disabled;
			}
			_control.isGameRunning = true;
			_simulationFrame = Engine.GetPhysicsFrames();
			FireComponent runtime = zamboni.componentManager.GetRuntime<FireComponent>("character.fire");
			Check(runtime != null && !runtime.IsReleased, "机枪冰车必须加载正式开火组件。");
			FireComponentCheckConfig fireComponentCheckConfig = (runtime.runningCheck = runtime.fireCheckList[0]);
			sunflower.SetupBaseState();
			Check(sunflower.isDowned && !sunflower.die && sunflower.instance.hitpoints > 0.0, "全息葵必须进入存活的底座状态。");
			normalSunflower.instance.canBeCollection = false;
			Check(!runtime.CanFireCheckOnce(fireComponentCheckConfig.GetProjectile(), fireComponentCheckConfig.GetCollisionFlags()), "只有全息葵底座时，机枪冰车不应因此开火。");
			normalSunflower.instance.canBeCollection = true;
			Check(runtime.CanFireCheckOnce(fireComponentCheckConfig.GetProjectile(), fireComponentCheckConfig.GetCollisionFlags()), "底座后方存在普通植物时，机枪冰车仍应开火。");
			double hitpoints = sunflower.instance.hitpoints;
			double hitpoints2 = normalSunflower.instance.hitpoints;
			int index = FireOneProjectile(runtime);
			AdvanceUntilPast(index, sunflower.GlobalPosition.X - 50f);
			Check(_bulletField.IsBulletActive(index) && _bulletField.GetBulletDataRef(index).pos.X < sunflower.GlobalPosition.X - 50f, "真实子弹必须穿过全息葵底座并继续飞行。");
			Check(Mathf.IsEqualApprox(sunflower.instance.hitpoints, hitpoints), "子弹穿过底座后，底座生命值必须保持不变。");
			AdvanceUntilPast(index, normalSunflower.GlobalPosition.X - 60f);
			Check(normalSunflower.instance.hitpoints < hitpoints2 && !_bulletField.IsBulletActive(index), "穿过底座的同一颗子弹必须命中后方普通植物并结算伤害。");
			GD.Print($"GATLING_ZAMBONI_BASE_PASS baseHp={hitpoints}->{sunflower.instance.hitpoints} rearHp={hitpoints2}->{normalSunflower.instance.hitpoints}");
			sunflower.RestoreNormalState();
			normalSunflower.instance.canBeCollection = false;
			Check(!sunflower.isDowned && runtime.CanFireCheckOnce(fireComponentCheckConfig.GetProjectile(), fireComponentCheckConfig.GetCollisionFlags()), "全息葵复活后必须重新成为子弹目标。");
			double hitpoints3 = sunflower.instance.hitpoints;
			index = FireOneProjectile(runtime);
			AdvanceUntilPast(index, sunflower.GlobalPosition.X - 60f);
			Check(sunflower.instance.hitpoints < hitpoints3 && !_bulletField.IsBulletActive(index), "机枪冰车子弹必须正常伤害复活后的全息葵。");
			sunflower.SetupBaseState();
			zamboni.GlobalPosition = sunflower.GlobalPosition;
			zamboni.gridPos = sunflower.gridPos;
			Check(zamboni.attackComponent.GetTarget() == sunflower, "车身接触索敌必须仍能识别具有爆胎能力的底座。");
			Check((sunflower.instance.physiqueTypeFlags & 0x10) != 0 && sunflower.instance.spikeHurt > 0.0, "底座必须保留爆胎能力。");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[GatlingZamboniHolographicBaseRuntimeTest] {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_bulletField))
			{
				_bulletField.OnBulletSpawned -= _spawnedBullets.Add;
				_bulletField.ClearActiveBullets();
				if (ownsBulletField)
				{
					_bulletField.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.isGameRunning = false;
				_control.QueueFree();
			}
			await WaitFrames(3);
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousGridBegin;
			manager.gridSize = previousGridSize;
			manager.gridNum = previousGridNum;
			projectileManager.ProcessMode = previousProjectileProcessMode;
			map?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
		}
		bool flag = _failures == 0 && _checks == 13;
		GD.Print($"GATLING_ZAMBONI_HOLOGRAPHIC_BASE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private TowerDefenseBattleFeatureMap CreateMap(TowerDefenseMapControl mapControl, TowerDefenseManager manager)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = manager.gridNum,
			gridBeginPos = manager.gridBeginPos,
			gridSize = manager.gridSize,
			plantOffset = 50.0
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, manager.gridNum.X, manager.gridNum.Y)
		});
		for (int i = 1; i <= manager.gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			rect = new Rect2(-200f, -200f, 1400f, 900f)
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private T Spawn<T>(string scenePath, Vector2I grid) where T : TowerDefenseCharacter
	{
		T val = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Reuse).Instantiate<T>(PackedScene.GenEditState.Disabled);
		val.inGame = true;
		val.gridPos = grid;
		val.Position = TowerDefenseManager.GetMapCellPlantPos(grid);
		_control.characterNode.AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
	}

	private int FireOneProjectile(FireComponent fire)
	{
		_bulletField.ClearActiveBullets();
		_spawnedBullets.Clear();
		fire.Fire();
		Check(_spawnedBullets.Count == 1, "一次正式开火必须生成一颗机枪冰车子弹。");
		return _spawnedBullets[0];
	}

	private void AdvanceUntilPast(int index, float x)
	{
		for (int i = 0; i < 180; i++)
		{
			if (!_bulletField.IsBulletActive(index))
			{
				break;
			}
			if (!(_bulletField.GetBulletDataRef(index).pos.X >= x))
			{
				break;
			}
			_bulletField.Update(1.0 / 60.0, ++_simulationFrame);
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
			GD.PushError("[GatlingZamboniHolographicBaseRuntimeTest] " + message);
		}
	}
}
