using System;
using System.Diagnostics;
using Godot;

public partial class PlantAttackIntervalRuntimeTest : Node
{
	public override void _Ready()
	{
		try
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			PlantAttackIntervalControl plantAttackIntervalControl = new PlantAttackIntervalControl
			{
				isGameRunning = true,
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(plantAttackIntervalControl, forceReadableName: false, InternalMode.Disabled);
			plantAttackIntervalControl.characterNode = new Node2D();
			plantAttackIntervalControl.AddChild(plantAttackIntervalControl.characterNode, forceReadableName: false, InternalMode.Disabled);
			instance.currentControl = plantAttackIntervalControl;
			instance.gridBeginPos = new Vector2(256f, 45f);
			instance.gridSize = new Vector2(80f, 98f);
			instance.gridNum = new Vector2I(9, 5);
			PlantAttackIntervalOwner plantAttackIntervalOwner = new PlantAttackIntervalOwner
			{
				Position = new Vector2((float)((instance.GetMapGroundLeft() + instance.GetMapGroundRight()) / 2.0), 241f)
			};
			plantAttackIntervalOwner.instance = new TowerDefenseCharacterInstance
			{
				character = plantAttackIntervalOwner,
				collisionFlags = 1
			};
			plantAttackIntervalControl.characterNode.AddChild(plantAttackIntervalOwner, forceReadableName: false, InternalMode.Disabled);
			plantAttackIntervalOwner.inGame = true;
			plantAttackIntervalOwner.SetPhysicsProcess(enable: false);
			plantAttackIntervalOwner.componentRunning = false;
			FireComponentDefinition fireComponentDefinition = new FireComponentDefinition
			{
				ComponentTypeId = "FireComponent",
				DefinitionId = "fire.interval.test",
				InstanceId = "fire.interval.test"
			};
			fireComponentDefinition.fireCheckList.Add(new FireComponentCheckConfig
			{
				projectile = new FireComponentProjectileSingle
				{
					projectileData = new TowerDefenseProjectileCreateData()
				}
			});
			ComponentManager componentManager = new ComponentManager();
			FireComponent fireComponent = new FireComponent();
			fireComponent.Bind(componentManager, plantAttackIntervalOwner, fireComponentDefinition);
			fireComponent.Activate();
			fireComponent.sprite = new AdobeAnimateSprite();
			plantAttackIntervalOwner.AddChild(fireComponent.sprite, forceReadableName: false, InternalMode.Disabled);
			AttackComponentDefinition attackComponentDefinition = new AttackComponentDefinition
			{
				ComponentTypeId = "AttackComponent",
				DefinitionId = "attack.interval.test",
				InstanceId = "attack.interval.test",
				checkGrid = false,
				checkLine = false
			};
			attackComponentDefinition.checkShapeResources.Add(new AabbShape2DResource
			{
				Geometry = new RectangleShape2D
				{
					Size = new Vector2(500f, 500f)
				}
			});
			AttackComponent attackComponent = new AttackComponent();
			attackComponent.Bind(componentManager, plantAttackIntervalOwner, attackComponentDefinition);
			attackComponent.Activate();
			attackComponent.sprite = fireComponent.sprite;
			fireComponent.fireIntervalOffset = 0f;
			attackComponent.attackIntervalOffset = 0.0;
			fireComponent.fireInterval = 1.5f;
			attackComponent.attackInterval = 1.5;
			GD.Print($"PLANT_INTERVAL_SETUP inside={plantAttackIntervalOwner.IsInsideComponentBattlefieldForRuntime} running={instance.IsGameRunning()} fireAlive={fireComponent.alive} fireChecks={fireComponent.fireCheckList.Count} sprite={fireComponent.sprite != null}");
			for (int i = 0; i < 4; i++)
			{
				Measure(fireComponent, attackComponent, measureFire: true, i);
				Measure(fireComponent, attackComponent, measureFire: false, i);
			}
			if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify-interval") >= 0)
			{
				Verify(fireComponent, attackComponent);
			}
			attackComponent.Release();
			fireComponent.Release();
			componentManager.Dispose();
			GD.Print("PLANT_ATTACK_INTERVAL_RESULT passed=True");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			GetTree().Quit(1);
		}
	}

	private static void Measure(FireComponent fire, AttackComponent attack, bool measureFire, int pass)
	{
		fire.timer = 0f;
		fire.checkIntreval = 0;
		attack.timer = 0.0;
		attack.checkIntrevalNow = 0;
		int num = 0;
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long timestamp = Stopwatch.GetTimestamp();
		for (int i = 0; i < 120000; i++)
		{
			if (measureFire)
			{
				fire.PhysicsProcess(1.0 / 60.0, (ulong)i);
				int checkIntreval = fire.checkIntreval;
				float timer = fire.timer;
				fire.IdleProcessing(1.0 / 60.0);
				if (fire.checkIntreval > checkIntreval || fire.timer > timer)
				{
					num++;
				}
			}
			else
			{
				attack.PhysicsProcess(1.0 / 60.0, (ulong)i);
				int checkIntrevalNow = attack.checkIntrevalNow;
				double timer2 = attack.timer;
				attack.IdleProcessing(1.0 / 60.0);
				if (attack.checkIntrevalNow > checkIntrevalNow || attack.timer > timer2)
				{
					num++;
				}
			}
		}
		GD.Print($"PLANT_INTERVAL_BENCH component={(measureFire ? "Fire" : "Attack")} pass={pass} frames=120000 checks={num} ms={Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F3} bytes={GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread}");
		if (num == 0)
		{
			throw new InvalidOperationException("测试未进入实际检测路径。");
		}
	}

	private static void Verify(FireComponent fire, AttackComponent attack)
	{
		fire.timer = 0f;
		fire.checkIntreval = 0;
		attack.timer = 0.0;
		attack.checkIntrevalNow = 0;
		fire.IdleProcessing(0.0);
		attack.IdleProcessing(0.0);
		if (Math.Abs((double)fire.timer - 1.5) > 0.001 || Math.Abs(attack.timer - 1.5) > 0.001)
		{
			throw new InvalidOperationException("无目标检测必须启动完整攻击间隔。");
		}
		fire.IdleProcessing(0.0);
		attack.IdleProcessing(0.0);
		fire.timeScale = 2f;
		attack.timeScale = 2.0;
		fire.PhysicsProcess(0.25, 1uL);
		attack.PhysicsProcess(0.25, 1uL);
		if ((double)Math.Abs(fire.timer - 1f) > 0.001 || Math.Abs(attack.timer - 1.0) > 0.001)
		{
			throw new InvalidOperationException("检测间隔必须遵循时间倍率，且重复回调不能重置冷却。");
		}
		fire.timeScale = 1f;
		attack.timeScale = 1.0;
		fire.timer = 0.01f;
		attack.timer = 0.01;
		fire.PhysicsProcess(0.02, 2uL);
		attack.PhysicsProcess(0.02, 2uL);
		fire.IdleProcessing(0.0);
		attack.IdleProcessing(0.0);
		if (Math.Abs((double)fire.timer - 1.5) > 0.001 || Math.Abs(attack.timer - 1.5) > 0.001)
		{
			throw new InvalidOperationException("间隔结束后必须重新检测并启动下一轮冷却。");
		}
		fire.timeScale = 0f;
		attack.timeScale = 0.0;
		fire.PhysicsProcess(10.0, 3uL);
		attack.PhysicsProcess(10.0, 3uL);
		if (Math.Abs((double)fire.timer - 1.5) > 0.001 || Math.Abs(attack.timer - 1.5) > 0.001 || attack.CanAttack())
		{
			throw new InvalidOperationException("暂停时冷却不能推进，外部索敌不能绕过冷却。");
		}
		fire.timeScale = 1f;
		attack.timeScale = 1.0;
		PlantAttackIntervalProjectileProbe plantAttackIntervalProjectileProbe = new PlantAttackIntervalProjectileProbe();
		PlantAttackIntervalProjectileProbe plantAttackIntervalProjectileProbe2 = new PlantAttackIntervalProjectileProbe
		{
			Available = true
		};
		fire.fireCheckList.Clear();
		fire.fireCheckList.Add(new FireComponentCheckConfig
		{
			projectile = plantAttackIntervalProjectileProbe
		});
		fire.fireCheckList.Add(new FireComponentCheckConfig
		{
			projectile = plantAttackIntervalProjectileProbe2
		});
		fire.RefreshConfiguration();
		fire.fireDirect = true;
		fire.fireAudioName = "";
		fire.timer = 0f;
		int volleys = 0;
		fire.OnFireReady += delegate
		{
			volleys++;
		};
		fire.IdleProcessing(0.0);
		fire.IdleProcessing(0.0);
		if (plantAttackIntervalProjectileProbe.Checks != 1 || plantAttackIntervalProjectileProbe2.Checks != 1 || volleys != 1 || Math.Abs((double)fire.timer - 1.5) > 0.001)
		{
			throw new InvalidOperationException("同轮必须检查后续配置，成功开火后不能重复检测或发射。");
		}
		GD.Print("PLANT_INTERVAL_VERIFY passed=True");
	}
}
