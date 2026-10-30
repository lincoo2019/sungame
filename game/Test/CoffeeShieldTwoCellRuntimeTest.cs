using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class CoffeeShieldTwoCellRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	private int _checks;

	private CoffeeShieldTwoCellRuntimeControl _control;

	private TowerDefenseBattleFeatureMap _map;

	private TowerDefenseMapControl _mapControl;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNumber = manager.gridNum;
		try
		{
			_ = 3;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				SetupBattle(manager);
				await VerifyCannon();
				await VerifyPumpkinProtection();
				await VerifyHypnosisAndSingleCell();
			}
			catch (Exception value)
			{
				_failures.Add($"回归运行异常：{value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.isGameRunning = false;
				_control.QueueFree();
			}
			await WaitFrames(6);
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousBegin;
			manager.gridSize = previousSize;
			manager.gridNum = previousNumber;
			_map?.Destroy();
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			ObjectManager.Instance.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		foreach (string failure in _failures)
		{
			GD.PushError("[CoffeeShieldTwoCellRuntimeTest] " + failure);
		}
		bool flag = _checks == 22 && _failures.Count == 0;
		GD.Print($"COFFEE_SHIELD_TWO_CELL_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyCannon()
	{
		Vector2I left = new Vector2I(3, 2);
		Vector2I right = left + Vector2I.Right;
		await Plant("PlantCornpult", left);
		await Plant("PlantCornpult", right);
		TowerDefensePlant towerDefensePlant = await Plant("PlantCobCannon", left);
		Check(towerDefensePlant.config is TowerDefensePlantConfig towerDefensePlantConfig && towerDefensePlantConfig.extendCoverDictionary.Count == 2 && Cell(left).characterList.Contains(towerDefensePlant) && Cell(right).characterList.Contains(towerDefensePlant), "正式升级后的玉米加农炮必须占用左右两格。");
		await UseCoffee(towerDefensePlant);
		Check(ShieldHp(left) == 4000.0, "玉米加农炮左格必须获得 4000 耐久护盾。");
		Check(ShieldHp(right) == 4000.0, "玉米加农炮右格必须获得 4000 耐久护盾。");
		TowerDefenseItemSheild itemShield = Cell(left).itemShield;
		TowerDefenseItemSheild itemShield2 = Cell(right).itemShield;
		Check(GodotObject.IsInstanceValid(itemShield) && GodotObject.IsInstanceValid(itemShield2) && itemShield != itemShield2 && itemShield.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(left)) && itemShield2.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(right)), "两格护盾必须分别在各自格子生成正式角色与显示位置。");
		Check(ShieldHp(left - Vector2I.Right) == 0.0 && ShieldHp(right + Vector2I.Right) == 0.0, "咖啡护盾不得越过目标植物的实际占格范围。");
	}

	private async Task VerifyPumpkinProtection()
	{
		Vector2I left = new Vector2I(3, 3);
		Vector2I right = left + Vector2I.Right;
		TowerDefensePlant leftPlant = await Plant("PlantFumeShroom", left);
		TowerDefensePlant rightPlant = await Plant("PlantFumeShroom", right);
		TowerDefensePlant pumpkin = await Plant("PlantPumpkinBig", left);
		Check(leftPlant.IsSleep() && rightPlant.IsSleep(), "白天两格大喷菇必须先处于真实睡眠状态。");
		Check(Cell(left).characterList.Contains(pumpkin) && Cell(right).characterList.Contains(pumpkin), "大南瓜必须通过正式种植登记在两格中。");
		await UseCoffee(pumpkin);
		Check(leftPlant.instance.wakeUp && !leftPlant.IsSleep(), "咖啡必须唤醒左格内的植物。");
		Check(rightPlant.instance.wakeUp && !rightPlant.IsSleep(), "咖啡必须唤醒右格内的植物。");
		Check(ShieldHp(left) == 4000.0 && ShieldHp(right) == 4000.0, "大南瓜的两格必须各获得一次 4000 耐久护盾。");
		double hitpoints = leftPlant.instance.hitpoints;
		double hitpoints2 = rightPlant.instance.hitpoints;
		leftPlant.Hurt(100.0);
		Check(leftPlant.instance.hitpoints == hitpoints && ShieldHp(left) == 3900.0 && ShieldHp(right) == 4000.0, "左格护盾必须独立承受 100 点伤害。");
		rightPlant.Hurt(200.0);
		Check(rightPlant.instance.hitpoints == hitpoints2 && ShieldHp(right) == 3800.0 && ShieldHp(left) == 3900.0, "右格护盾必须独立承受 200 点伤害并保护本体。");
		rightPlant.Hypnoses();
		Check(!rightPlant.instance.hypnoses && ShieldHp(right) == 2800.0, "右格护盾必须消耗 1000 耐久抵御真实魅惑。");
		await UseCoffee(pumpkin);
		Check(ShieldHp(left) == 7900.0 && ShieldHp(right) == 6800.0, "重复使用咖啡时两格各叠加 4000 耐久，不得漏加或重复计入。");
		await UseCoffee(pumpkin);
		Check(ShieldHp(left) == 8000.0 && ShieldHp(right) == 8000.0, "两格护盾必须分别遵守 8000 耐久上限。");
		Check(CountShields(Cell(left)) == 1 && CountShields(Cell(right)) == 1, "连续叠加后每格仍只能有一个护盾角色。");
		Godot.Collections.Array array = _map.SaveFeature()["plantGrid"].AsGodotArray();
		string text = array[left.X].AsGodotArray()[left.Y].AsGodotDictionary()["itemShield"].AsString();
		string text2 = array[right.X].AsGodotArray()[right.Y].AsGodotDictionary()["itemShield"].AsString();
		Check(text.Length > 0 && text2.Length > 0 && text != text2, "正式地图存档必须记录左右两格各自的护盾引用。");
	}

	private async Task VerifyHypnosisAndSingleCell()
	{
		Vector2I left = new Vector2I(3, 4);
		Vector2I right = left + Vector2I.Right;
		TowerDefensePlant pumpkin = await Plant("PlantPumpkinBig", left);
		pumpkin.Hypnoses();
		Check(pumpkin.instance.hypnoses, "阵营回归必须使用真实被魅惑的两格植物。");
		await UseCoffee(pumpkin, hypnoses: true);
		Check((Cell(left).itemShield?.instance.hypnoses ?? false) && Cell(left).itemShield.camp == pumpkin.camp, "左格新护盾必须同步魅惑咖啡的阵营。");
		Check((Cell(right).itemShield?.instance.hypnoses ?? false) && Cell(right).itemShield.camp == pumpkin.camp, "右格新护盾必须同步魅惑咖啡的阵营。");
		Vector2I single = new Vector2I(7, 1);
		await UseCoffee(await Plant("PlantWallnut", single));
		Check(ShieldHp(single) == 4000.0, "单格植物仍应获得一次 4000 耐久护盾。");
		Check(ShieldHp(single - Vector2I.Right) == 0.0 && ShieldHp(single + Vector2I.Right) == 0.0, "单格植物不能给相邻格额外生成护盾。");
	}

	private async Task<TowerDefensePlant> Plant(string packetName, Vector2I grid)
	{
		TowerDefensePlant plant = TowerDefenseManager.GetPacketConfig(packetName).Plant(grid, playAudio: false) as TowerDefensePlant;
		if (!GodotObject.IsInstanceValid(plant))
		{
			throw new InvalidOperationException($"正式种植失败：{packetName}，格子={grid}。");
		}
		await WaitFrames(6);
		plant.ProcessMode = ProcessModeEnum.Disabled;
		return plant;
	}

	private async Task UseCoffee(TowerDefensePlant target, bool hypnoses = false)
	{
		TowerDefensePlantCoffeeSheild coffee = TowerDefenseManager.GetPacketConfig("PlantCoffeeSheild").PlantOnPlant(target, playAudio: false) as TowerDefensePlantCoffeeSheild;
		if (!GodotObject.IsInstanceValid(coffee) || coffee.targetPlant != target)
		{
			throw new InvalidOperationException("护盾咖啡豆未通过正式入口附着到目标植物。");
		}
		coffee.ProcessMode = ProcessModeEnum.Disabled;
		if (hypnoses)
		{
			coffee.Hypnoses();
		}
		await WaitFrames(2);
		ExplodeComponent runtime = coffee.componentManager.GetRuntime<ExplodeComponent>();
		runtime.AnimeCompleted(runtime.explodeAnimeClips);
		await WaitFrames(6);
	}

	private static TowerDefenseCellInstance Cell(Vector2I grid)
	{
		return TowerDefenseManager.GetMapCell(grid);
	}

	private static double ShieldHp(Vector2I grid)
	{
		TowerDefenseItemSheild itemShield = Cell(grid).itemShield;
		if (!GodotObject.IsInstanceValid(itemShield))
		{
			return 0.0;
		}
		return itemShield.instance.hitpoints;
	}

	private static int CountShields(TowerDefenseCellInstance cell)
	{
		int num = 0;
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character is TowerDefenseItemSheild && GodotObject.IsInstanceValid(character) && !character.isDestroy)
			{
				num++;
			}
		}
		return num;
	}

	private void SetupBattle(TowerDefenseManager manager)
	{
		_control = new CoffeeShieldTwoCellRuntimeControl
		{
			isGameRunning = true,
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
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = manager.gridNum,
			gridBeginPos = manager.gridBeginPos,
			gridSize = manager.gridSize,
			plantOffset = 50.0,
			isNight = false
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, manager.gridNum.X, manager.gridNum.Y)
		});
		for (int i = 1; i <= manager.gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		_mapControl = new TowerDefenseMapControl();
		_map = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = _mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		};
		_mapControl.mapFeature = _map;
		_map.PlantGridInit();
		_control.featureDictionary["Map"] = _map;
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
			_failures.Add(message);
		}
	}
}
