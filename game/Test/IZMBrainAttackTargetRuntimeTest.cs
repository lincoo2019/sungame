using System;
using System.Threading.Tasks;
using Godot;

public partial class IZMBrainAttackTargetRuntimeTest : Node
{
	private const string BrainScenePath = "res://Asset/Anime/Character/Item/Brain/Scene/TowerDefenseItemBrain.tscn";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string FallbackZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Balloon/Scene/TowerDefenseZombieBalloon.tscn";

	private const int TestLine = 2;

	private const string DiggerScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		IZMBrainAttackTargetControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseBattleFeatureBrain brainFeature = null;
		TowerDefenseItemBrain brain = null;
		TowerDefenseZombie zombie = null;
		TowerDefenseZombie fallbackZombie = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0106;
				}
				control = new IZMBrainAttackTargetControlStub
				{
					Name = "IZMBrainAttackTargetControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM
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
				manager.gridSize = new Vector2(80f, 98f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				brainFeature = new TowerDefenseBattleFeatureBrain
				{
					control = control,
					config = new TowerDefenseBattleFeatureBrainConfig()
				};
				control.featureDictionary[new StringName("Brain")] = brainFeature;
				brainFeature.NormalizeBrainLineSize();
				brain = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Item/Brain/Scene/TowerDefenseItemBrain.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseItemBrain>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(brain))
				{
					brain.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, 2)) + new Vector2((float)brainFeature.config.horizontalOffset, 0f));
					brain.gridPos = new Vector2I(0, 2);
					control.characterNode.AddChild(brain, forceReadableName: false, InternalMode.Disabled);
					brain.OnBrainDestroy += brainFeature.BrainDestroy;
					brainFeature.brainLine[2] = brain;
				}
				await CheckDiggerBrainDirection(control, brain);
				await CheckPoleLanding(control, brain);
				zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.gridPos = new Vector2I(1, 2);
					control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				}
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(brain) && brain.config?.name == "ItemBrain", "The scenario must register the real IZM Brain in the Brain Feature.");
				Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieNormal", "The scenario must instantiate the real normal Zombie scene.");
				if (!GodotObject.IsInstanceValid(brain) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_0106;
				}
				AttackComponent attackComponent = zombie.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				Check(attackComponent != null && !attackComponent.IsReleased && attackComponent.checkGrid && attackComponent.checkLine, "The real normal Zombie must use the grid-indexed melee Attack runtime.");
				if (attackComponent == null || attackComponent.IsReleased)
				{
					goto end_IL_0106;
				}
				zombie.GlobalPosition = new Vector2((float)manager.GetMapGroundLeft(), brain.GlobalPosition.Y);
				zombie.gridPos = new Vector2I(1, 2);
				zombie.inGame = true;
				zombie.componentAlive = true;
				zombie.componentRunning = false;
				zombie.instance.sleep = false;
				attackComponent.SetAlive(alive: true);
				attackComponent.target = null;
				control.isGameRunning = true;
				Check(brain.gridPos == new Vector2I(0, 2) && zombie.gridPos == new Vector2I(1, 2), "The boundary regression fixture must preserve Brain column 0 and Zombie column 1.");
				Check(brain.WorldHitRect.Intersects(zombie.WorldHitRect), "The Brain and Zombie hit boxes must physically overlap before target lookup.");
				Check(zombie.CanTarget(brain) && zombie.CanCollision(brain.instance.maskFlags) && brain.canCheck && brain.instance.canBeCollection, "The Brain must pass the production camp, collision, and collection gates.");
				Check(manager.characterRegistry.HasAttackGridCharacters(2, 1, 1, includeAllLineCheck: true, zombie.camp), "Column 1 melee lookup must expose the adjacent off-board Brain candidate.");
				TowerDefenseCharacter target = attackComponent.GetTarget();
				Check(target == brain && attackComponent.target == brain, "The production grid-indexed melee query must acquire the overlapping Brain.");
				Check(attackComponent.CanAttackOnce() && attackComponent.target == brain, "The immediate attack check must keep the Brain as the Zombie target.");
				double hitpoints = brain.instance.hitpoints;
				attackComponent.AttackDpsExecute(0.25, ((TowerDefenseZombieConfig)zombie.config).attack);
				Check(brain.instance.hitpoints < hitpoints, "The acquired Brain must take damage from the real Zombie eating path.");
				fallbackZombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Balloon/Scene/TowerDefenseZombieBalloon.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(fallbackZombie))
				{
					fallbackZombie.gridPos = new Vector2I(1, 2);
					control.characterNode.AddChild(fallbackZombie, forceReadableName: false, InternalMode.Disabled);
				}
				await WaitFrames(8);
				AttackComponent attackComponent2 = fallbackZombie?.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				if (attackComponent2 != null && !attackComponent2.IsReleased)
				{
					fallbackZombie.GlobalPosition = new Vector2((float)manager.GetMapGroundLeft(), brain.GlobalPosition.Y);
					fallbackZombie.gridPos = new Vector2I(1, 2);
					fallbackZombie.inGame = true;
					fallbackZombie.componentAlive = true;
					fallbackZombie.componentRunning = false;
					fallbackZombie.instance.sleep = false;
					attackComponent2.SetAlive(alive: true);
					attackComponent2.target = null;
				}
				Check(GodotObject.IsInstanceValid(fallbackZombie) && attackComponent2 != null && !attackComponent2.IsReleased, "The fallback scenario must instantiate the real Balloon Zombie attack runtime.");
				Check(GodotObject.IsInstanceValid(fallbackZombie) && !fallbackZombie.CanCollision(brain.instance.maskFlags) && attackComponent2?.GetTarget() == null, "The fallback scenario must preserve a special Zombie that cannot acquire the ground Brain through ordinary collision targeting.");
				TowerDefenseBattleProcessIZM towerDefenseBattleProcessIZM = new TowerDefenseBattleProcessIZM();
				towerDefenseBattleProcessIZM.control = control;
				towerDefenseBattleProcessIZM.brainFeature = brainFeature;
				towerDefenseBattleProcessIZM.config = new TowerDefenseLevelIZMManagerConfig();
				towerDefenseBattleProcessIZM.ZombieEnterHouse(fallbackZombie);
				await WaitFrames(2);
				Check(brain.isDestroy && brain.over && !GodotObject.IsInstanceValid(brainFeature.brainLine[2]), "IZM house entry must consume the live Brain in the special Zombie's line before removing that Zombie.");
				goto end_IL_00e7;
				end_IL_0106:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[IZMBrainAttackTargetRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00e7;
			}
			return;
			end_IL_00e7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(fallbackZombie) && !fallbackZombie.IsQueuedForDeletion())
			{
				fallbackZombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(brain) && !brain.IsQueuedForDeletion())
			{
				brain.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			brainFeature?.Destroy();
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(2);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 38;
		GD.Print($"IZM_BRAIN_ATTACK_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task CheckDiggerBrainDirection(IZMBrainAttackTargetControlStub control, TowerDefenseItemBrain brain)
	{
		TowerDefenseZombieDigger digger = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/Base/TowerDefenseZombieDigger.tscn", null, ResourceLoader.CacheMode.Ignore).Instantiate<TowerDefenseZombieDigger>(PackedScene.GenEditState.Disabled);
		double initialBrainHealth = brain.instance.hitpoints;
		try
		{
			digger.gridPos = new Vector2I(1, 2);
			control.characterNode.AddChild(digger, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			AttackComponent attack = digger.attackComponent;
			Check(attack != null && !attack.IsReleased && attack.useParentHitBox && attack.attackType == "Eat", "矿工必须使用正式的身体接触啃食组件。");
			if (attack?.IsReleased ?? true)
			{
				return;
			}
			float lawnLeft = (float)TowerDefenseManager.Instance.GetMapGroundLeft();
			digger.SetLogicalGlobalPosition(new Vector2(lawnLeft + 31f, brain.GetLogicalGlobalPosition().Y));
			TowerDefenseManager.Instance.CharacterRegister(digger);
			control.isGameRunning = true;
			digger.ActivateGameplayProcessing();
			digger.SendStateEvent("ToDig");
			for (int frame = 0; frame < 480; frame++)
			{
				await WaitFrames(1);
				string text = digger.CurrentStateHandle?.StableId;
				if (digger.digOver && !digger.isRise && (text == "zombie.walk" || text == "zombie.attack"))
				{
					break;
				}
			}
			Check(digger.digOver && !digger.isRise && (digger.CurrentStateHandle?.StableId == "zombie.walk" || digger.CurrentStateHandle?.StableId == "zombie.attack"), "矿工必须经过真实挖掘、出土和落地流程。");
			Check(digger.GetLogicalGlobalTransform(digger.sprite).X.X < 0f && brain.GetLogicalGlobalPosition().X < digger.GetLogicalGlobalPosition().X, "自然出土后矿工应朝右，脑子位于它的左后方。");
			digger.SetLogicalGlobalPosition(new Vector2(lawnLeft + 5f, brain.GetLogicalGlobalPosition().Y));
			digger.InvalidateHitBoxBounds();
			Check(digger.WorldHitRect.Intersects(brain.WorldHitRect), "复现位置必须让身后的脑子与矿工身体碰撞框重叠。");
			Check(attack.GetTarget() == null, "网格索敌不能选中矿工背后的脑子。");
			Check(!attack.CanAttackOnce() && attack.target == null, "立即攻击检测不能选中矿工背后的脑子。");
			attack.target = brain;
			Check(!attack.CanAttack() && attack.target == null, "转身后必须释放原先缓存的脑子目标。");
			attack.target = brain;
			double healthBeforeRearBite = brain.instance.hitpoints;
			attack.AttackDpsExecute(0.25, 1.0);
			Check(Math.Abs(brain.instance.hitpoints - healthBeforeRearBite) < 0.001, "残留的持续啃食回调不能伤害背后的脑子。");
			attack.AttackExecute(1.0);
			Check(Math.Abs(brain.instance.hitpoints - healthBeforeRearBite) < 0.001, "残留的单次啃食回调不能伤害背后的脑子。");
			attack.target = null;
			float beforeWalkX = digger.GetLogicalGlobalPosition().X;
			await WaitFrames(30);
			Check(Math.Abs(brain.instance.hitpoints - healthBeforeRearBite) < 0.001 && digger.CurrentStateHandle?.StableId == "zombie.walk" && digger.GetLogicalGlobalPosition().X > beforeWalkX, "矿工应自然向右离开脑子，脑子生命值保持不变。");
			GD.Print($"DIGGER_BRAIN_NATURAL x={beforeWalkX}->{digger.GetLogicalGlobalPosition().X} hp={brain.instance.hitpoints}/{healthBeforeRearBite} state={digger.CurrentStateHandle?.StableId}");
			digger.SetLogicalGlobalPosition(new Vector2(lawnLeft - 5f, brain.GetLogicalGlobalPosition().Y));
			digger.gridPos = new Vector2I(0, 2);
			digger.InvalidateHitBoxBounds();
			Check(!digger.IsInsideComponentBattlefield && digger.WorldHitRect.Intersects(brain.WorldHitRect), "场外复现位置仍须与脑子碰撞框重叠。");
			attack.target = null;
			Check(!attack.CanAttack() && attack.target == null, "草坪外的脑子接触索敌也不能绕过朝向判断。");
			attack.target = brain;
			Check(!attack.CanAttack() && attack.target == null, "场外矿工同样必须释放身后脑子的缓存目标。");
			attack.target = brain;
			double hitpoints = brain.instance.hitpoints;
			attack.AttackDpsExecute(0.25, 1.0);
			Check(Math.Abs(brain.instance.hitpoints - hitpoints) < 0.001, "场外矿工残留的啃食回调不能对身后脑子造成伤害。");
			digger.Scale = Vector2.One;
			attack.target = null;
			Check(attack.CanAttack() && attack.target == brain, "矿工朝左且脑子位于前方时，场外接触索敌必须正常工作。");
			attack.AttackDpsExecute(0.25, 1.0);
			Check(brain.instance.hitpoints < hitpoints, "矿工必须能正常啃食前方脑子并造成伤害。");
			digger.Scale = new Vector2(-1f, 1f);
			digger.transformPoint.Scale = new Vector2(0f - digger.transformPoint.Scale.X, digger.transformPoint.Scale.Y);
			attack.target = null;
			Check(attack.CanAttack() && attack.target == brain, "脑子前后判断必须采用完整显示朝向，包含子节点镜像。");
		}
		finally
		{
			control.isGameRunning = false;
			if (GodotObject.IsInstanceValid(digger))
			{
				TowerDefenseManager.Instance.CharacterUnregister(digger);
				digger.QueueFree();
			}
			brain.instance.hitpoints = initialBrainHealth;
			await WaitFrames(3);
		}
	}

	private async Task CheckPoleLanding(IZMBrainAttackTargetControlStub control, TowerDefenseItemBrain brain)
	{
		TowerDefensePlant wallnut = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn").Instantiate<TowerDefensePlant>(PackedScene.GenEditState.Disabled);
		TowerDefenseZombiePolevaulter pole = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn").Instantiate<TowerDefenseZombiePolevaulter>(PackedScene.GenEditState.Disabled);
		try
		{
			wallnut.gridPos = new Vector2I(1, 2);
			wallnut.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
			wallnut.Position = TowerDefenseManager.GetMapCellPlantPos(wallnut.gridPos);
			pole.gridPos = new Vector2I(3, 2);
			pole.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
			pole.Position = TowerDefenseManager.GetMapCellPlantPos(pole.gridPos);
			control.characterNode.AddChild(wallnut, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(pole, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseManager.Instance.CharacterRegister(wallnut);
			TowerDefenseManager.Instance.CharacterRegister(pole);
			control.isGameRunning = true;
			await WaitFrames(4);
			TowerDefenseManager.GetMapCell(wallnut.gridPos).CharacterPlant(GD.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres"), wallnut);
			pole.ActivateGameplayProcessing();
			pole.Walk();
			double hp = brain.instance.hitpoints;
			bool jumped = false;
			bool landedOutside = false;
			bool ate = false;
			for (int frame = 0; frame < 600; frame++)
			{
				if (!GodotObject.IsInstanceValid(pole))
				{
					break;
				}
				await WaitFrames(1);
				if (!GodotObject.IsInstanceValid(pole) || !GodotObject.IsInstanceValid(pole.sprite))
				{
					break;
				}
				jumped |= pole.sprite.clip == "Jump";
				landedOutside |= jumped && pole.jumpOver && pole.CurrentStateHandle?.StableId != "zombie.polevaulter.jump" && (double)pole.GetLogicalGlobalPosition().X < TowerDefenseManager.Instance.GetMapGroundLeft();
				ate |= pole.sprite.clip == "Eat" && brain.instance.hitpoints < hp;
				if (ate || pole.GetLogicalGlobalPosition().X < -45f)
				{
					break;
				}
			}
			GD.Print($"POLE_BRAIN_LANDING jumped={jumped} outside={landedOutside} ate={ate} x={pole.GetLogicalGlobalPosition().X} state={pole.CurrentStateHandle?.StableId} clip={pole.sprite.clip} hp={brain.instance.hitpoints} field={pole.IsInsideComponentBattlefield}");
			Check(jumped, "撑杆必须自然起跳越过第一列坚果。");
			Check(landedOutside, "撑杆必须自然落到草坪左边界外以覆盖视频路径。");
			Check(ate, "落地后必须播放啃食动画并实际扣除脑子生命值。");
			brain.instance.invincible = true;
			Check(!pole.attackComponent.CanAttack(), "脑子无敌时不能绕过正常攻击限制。");
			brain.instance.invincible = false;
			int maskFlags = brain.instance.maskFlags;
			brain.instance.maskFlags = 0;
			Check(!pole.attackComponent.CanAttack(), "脑子碰撞掩码不匹配时不能啃食。");
			brain.instance.maskFlags = maskFlags;
			pole.SetLogicalGlobalPosition(new Vector2(-200f, brain.GetLogicalGlobalPosition().Y));
			Check(!pole.attackComponent.CanAttack(), "不接触脑子的场外僵尸不能隔空啃食。");
			pole.SetLogicalGlobalPosition(new Vector2(-30f, (float)TowerDefenseManager.GetMapLineY(3)));
			pole.gridPos = new Vector2I(0, 3);
			Check(!pole.attackComponent.CanAttack(), "相邻行没有脑子时不能攻击其他行的脑子。");
		}
		finally
		{
			control.isGameRunning = false;
			TowerDefenseCharacter[] array = new TowerDefenseCharacter[2] { wallnut, pole };
			foreach (TowerDefenseCharacter towerDefenseCharacter in array)
			{
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					TowerDefenseManager.Instance.CharacterUnregister(towerDefenseCharacter);
					towerDefenseCharacter.QueueFree();
				}
			}
			await WaitFrames(3);
		}
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(80f, 98f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
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
			GD.PushError("[IZMBrainAttackTargetRuntimeTest] " + message);
		}
	}
}
