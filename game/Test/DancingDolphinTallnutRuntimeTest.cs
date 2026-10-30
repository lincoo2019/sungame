using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class DancingDolphinTallnutRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	private int _checks;

	private DancingDolphinTallnutRuntimeControl _control;

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
			_ = 2;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				SetupBattle(manager);
				string[] array = new string[3] { "PlantTallnut", "PlantSeaNut", "PlantWallnut" };
				foreach (string plantPacket in array)
				{
					await VerifyJump(plantPacket, useSummonedDancer: false);
					await VerifyJump(plantPacket, useSummonedDancer: true);
				}
			}
			catch (Exception value)
			{
				_failures.Add($"运行回归异常：{value}");
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
			GD.PushError("[DancingDolphinTallnutRuntimeTest] " + failure);
		}
		bool flag = _checks == 48 && _failures.Count == 0;
		GD.Print($"DANCING_DOLPHIN_TALLNUT_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyJump(string plantPacket, bool useSummonedDancer)
	{
		string label = (useSummonedDancer ? "召唤舞伴" : "舞王") + "/" + plantPacket;
		TowerDefenseZombieDolphinMJ leader = TowerDefenseManager.GetPacketConfig("ZombieDolphinMJ").Plant(new Vector2I(8, 3), playAudio: false) as TowerDefenseZombieDolphinMJ;
		leader.timer = 60.0;
		leader.ProcessMode = ProcessModeEnum.Disabled;
		await WaitFrames(6);
		TowerDefenseZombie zombie = leader;
		if (useSummonedDancer)
		{
			leader.SpawnDancer();
			await WaitFrames(6);
			Check(leader.dancerList.Count == 4 && leader.dancerList.All((TowerDefenseCharacter dancer) => GodotObject.IsInstanceValid(dancer) && dancer is TowerDefenseZombieDolphinDC), label + " 必须由正式召唤流程生成四名伴舞海豚。");
			zombie = leader.dancerList[2] as TowerDefenseZombieDolphinDC;
			for (int frame = 0; frame < 180; frame++)
			{
				if (!zombie.isRise)
				{
					break;
				}
				await WaitFrames(1);
			}
			Check(zombie is TowerDefenseZombieDolphinDC towerDefenseZombieDolphinDC && towerDefenseZombieDolphinDC.GetJackson() == leader && towerDefenseZombieDolphinDC.dolphin && !towerDefenseZombieDolphinDC.isRise, label + " 必须保留舞王归属并携带海豚完成出土。");
			foreach (TowerDefenseCharacter dancer in leader.dancerList)
			{
				dancer.ProcessMode = ProcessModeEnum.Disabled;
			}
		}
		Vector2I targetGrid = new Vector2I(5, 3);
		if (plantPacket != "PlantSeaNut")
		{
			TowerDefenseCharacter lily = TowerDefenseManager.GetPacketConfig("PlantLilyPad").Plant(targetGrid, playAudio: false);
			await WaitFrames(6);
			GD.Print($"DANCING_DOLPHIN_SETUP lily={GodotObject.IsInstanceValid(lily)} types={string.Join(',', TowerDefenseManager.GetMapCell(targetGrid).gridType)} water={zombie.inWater}");
		}
		TowerDefensePlant plant = (TowerDefenseManager.GetPacketConfig(plantPacket).Plant(targetGrid, playAudio: false) as TowerDefensePlant) ?? throw new InvalidOperationException("无法种植测试植物 " + plantPacket + "。");
		await WaitFrames(6);
		bool isTall = plantPacket != "PlantWallnut";
		Check(plant != null && plant.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL == isTall && zombie.inWater, label + " 必须使用正式植物高度与水中海豚状态。");
		Vector2 startPosition = plant.GetLogicalGlobalPosition() + new Vector2(65f, 0f);
		zombie.SetLogicalGlobalPosition(startPosition);
		zombie.gridPos = TowerDefenseManager.Instance.GetMapGridPos(startPosition);
		zombie.inSwimPlay = true;
		zombie.ProcessMode = ProcessModeEnum.Inherit;
		zombie.Walk();
		bool enteredJump = false;
		ulong timeoutFrame = Engine.GetPhysicsFrames() + (ulong)(Engine.PhysicsTicksPerSecond * 8);
		while (Engine.GetPhysicsFrames() < timeoutFrame)
		{
			await WaitFrames(1);
			bool flag = enteredJump;
			string text = zombie.CurrentStateHandle?.StableId;
			bool flag2 = ((text == "zombie.dolphin_mj.jump" || text == "zombie.dolphin_dc.jump") ? true : false);
			enteredJump = flag || flag2;
			if (enteredJump)
			{
				text = zombie.CurrentStateHandle?.StableId;
				if (!(text == "zombie.dolphin_mj.jump") && !(text == "zombie.dolphin_dc.jump"))
				{
					break;
				}
			}
		}
		zombie.walkSpeedScale = 0.0;
		await WaitFrames(6);
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		Vector2 logicalGlobalPosition = zombie.GetLogicalGlobalPosition();
		bool flag3 = ((zombie is TowerDefenseZombieDolphinMJ towerDefenseZombieDolphinMJ) ? towerDefenseZombieDolphinMJ.dolphin : ((TowerDefenseZombieDolphinDC)zombie).dolphin);
		bool flag4 = ((zombie is TowerDefenseZombieDolphinMJ towerDefenseZombieDolphinMJ2) ? towerDefenseZombieDolphinMJ2.isJump : ((TowerDefenseZombieDolphinDC)zombie).isJump);
		Check(enteredJump, label + " 必须经自然索敌进入跳跃状态。");
		Check(!flag3 && !flag4, label + " 拦截或完成跳跃后必须丢弃海豚并退出跳跃。");
		Check(isTall ? (logicalGlobalPosition.X >= plant.GetLogicalGlobalPosition().X + 30f) : (logicalGlobalPosition.X < plant.GetLogicalGlobalPosition().X), $"{label} 落点必须位于{(isTall ? "高大植物前方" : "普通植物后方")}，实际 {logicalGlobalPosition}，植物 {plant.GetLogicalGlobalPosition()}。");
		Check(zombie.instance.collisionFlags != 0 && zombie.instance.maskFlags != 0, label + " 落地或拦截后必须恢复碰撞标记。");
		zombie.AnimeCompleted("DolphinJump");
		Check(zombie.GetLogicalGlobalPosition().IsEqualApprox(logicalGlobalPosition), label + " 迟到的跳跃完成回调不得再次移动僵尸。");
		Check(zombie.gridPos == TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition), $"{label} 落点必须同步逻辑格子，实际 {zombie.gridPos}，预期 {TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition)}。");
		GD.Print($"DANCING_DOLPHIN_TALLNUT case={label} jumped={enteredJump} tall={isTall} start={startPosition} final={logicalGlobalPosition} plant={plant.GetLogicalGlobalPosition()} dolphin={flag3} state={zombie.CurrentStateHandle?.StableId}");
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter && !child.IsQueuedForDeletion())
			{
				child.QueueFree();
			}
		}
		await WaitFrames(6);
	}

	private void SetupBattle(TowerDefenseManager manager)
	{
		_control = new DancingDolphinTallnutRuntimeControl
		{
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig(),
			characterNode = new Node2D()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig();
		manager.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
		manager.gridSize = towerDefenseMapConfig.gridSize;
		manager.gridNum = towerDefenseMapConfig.gridNum;
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, towerDefenseMapConfig.gridNum.X, towerDefenseMapConfig.gridNum.Y),
			gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
			{
				TowerDefenseEnum.PLANTGRIDTYPE.WATER,
				TowerDefenseEnum.PLANTGRIDTYPE.AIR
			}
		});
		for (int i = 1; i <= towerDefenseMapConfig.gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		_mapControl = new TowerDefenseMapControl();
		_map = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = _mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			rect = TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(towerDefenseMapConfig)
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
