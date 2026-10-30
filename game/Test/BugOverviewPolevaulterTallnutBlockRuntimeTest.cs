using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPolevaulterTallnutBlockRuntimeTest : Node
{
	private const string PolevaulterScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn";

	private const string TallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn";

	private static readonly Vector2I EncounterGrid = new Vector2I(4, 2);

	private static readonly Vector2 EncounterPosition = new Vector2(400f, 252f);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousBackZombie = manager?.backZombie ?? false;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		BugOverviewPolevaulterTallnutBlockControlStub control = null;
		TowerDefenseZombiePolevaulter polevaulter = null;
		TowerDefensePlantTallnut tallnut = null;
		PackedScene polevaulterScene = null;
		PackedScene tallnutScene = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager 自动加载必须可用。");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_0113;
				}
				control = new BugOverviewPolevaulterTallnutBlockControlStub
				{
					Name = "PolevaulterTallnutBlockControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				manager.backZombie = false;
				TowerDefenseZombie.UseBatch = false;
				polevaulterScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				tallnutScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(polevaulterScene) && GodotObject.IsInstanceValid(tallnutScene), "正式撑杆僵尸和高坚果场景必须加载。");
				polevaulter = polevaulterScene?.Instantiate<TowerDefenseZombiePolevaulter>(PackedScene.GenEditState.Disabled);
				tallnut = tallnutScene?.Instantiate<TowerDefensePlantTallnut>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(polevaulter) && GodotObject.IsInstanceValid(tallnut), "正式撑杆僵尸和高坚果必须实例化。");
				if (!GodotObject.IsInstanceValid(polevaulter) || !GodotObject.IsInstanceValid(tallnut))
				{
					goto end_IL_0113;
				}
				PrepareCharacter(tallnut, EncounterGrid, EncounterPosition);
				PrepareCharacter(polevaulter, EncounterGrid, EncounterPosition);
				control.characterNode.AddChild(tallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(polevaulter, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				polevaulter.ProcessMode = ProcessModeEnum.Disabled;
				tallnut.ProcessMode = ProcessModeEnum.Disabled;
				PrepareCharacter(tallnut, EncounterGrid, EncounterPosition);
				PrepareCharacter(polevaulter, EncounterGrid, EncounterPosition);
				polevaulter.groundMoveComponent?.SetAlive(false);
				await WaitFrames(2);
				Check(polevaulter.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn" && tallnut.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn" && polevaulter.config?.name == "ZombiePolevaulter" && tallnut.config?.name == "PlantTallnut", "复现场景必须使用用户反馈的一章撑杆僵尸和正式高坚果。");
				Check((polevaulter.StateMachine?.IsInitialized ?? false) && (polevaulter.sprite?.HasClip("Jump") ?? false) && polevaulter.sprite.HasClip(polevaulter.walkAnimeClip), "正式撑杆僵尸状态机和 Jump/Walk 动画必须可用。");
				AttackComponent attackComponent = polevaulter.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				BugOverviewPolevaulterTallnutBlockRuntimeTest bugOverviewPolevaulterTallnutBlockRuntimeTest = this;
				int condition;
				if (attackComponent != null && !attackComponent.IsReleased && attackComponent.checkTall)
				{
					GroundMoveComponent groundMoveComponent = polevaulter.groundMoveComponent;
					condition = ((groundMoveComponent != null && !groundMoveComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewPolevaulterTallnutBlockRuntimeTest.Check((byte)condition != 0, "撑杆跳目标攻击组件必须开启高植物优先检测。");
				BugOverviewPolevaulterTallnutBlockRuntimeTest bugOverviewPolevaulterTallnutBlockRuntimeTest2 = this;
				TowerDefenseCharacterInstance instance = tallnut.instance;
				bugOverviewPolevaulterTallnutBlockRuntimeTest2.Check(instance != null && instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL && polevaulter.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && tallnut.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && polevaulter.HasHitBox && tallnut.HasHitBox && polevaulter.gridPos == EncounterGrid && tallnut.gridPos == EncounterGrid, "正式高坚果必须是高身位、对立阵营且碰撞体已注册。");
				if (attackComponent == null || attackComponent.IsReleased)
				{
					goto end_IL_0113;
				}
				attackComponent.target = tallnut;
				attackComponent.alive = true;
				attackComponent.timer = 0.0;
				attackComponent.checkIntrevalNow = 0;
				Check(attackComponent.HasAttackGridTargetCandidates() && attackComponent.CanAttack() && attackComponent.target == tallnut, "正式撑杆跳检测必须能在起跳前命中重叠高坚果。");
				attackComponent.target = null;
				bool flag = polevaulter.SendStateEvent("ToJump");
				Check(flag && polevaulter.CurrentStateHandle?.StableId == "zombie.polevaulter.jump" && polevaulter.sprite.clip == "Jump", "撑杆僵尸必须通过正式状态机进入 Jump。");
				Check(attackComponent.target == null && polevaulter.instance.collisionFlags == 0 && polevaulter.instance.maskFlags == 0, "测试必须覆盖跳跃期目标缓存为空且自身碰撞标记被清空的漏判窗口。");
				polevaulter.AnimeEvent("check", default(Variant));
				polevaulter.JumpProcessing(1.0 / 60.0);
				float blockedX = polevaulter.GetLogicalGlobalPosition().X;
				string blockedState = polevaulter.CurrentStateHandle?.StableId ?? string.Empty;
				string blockedClip = polevaulter.sprite.clip;
				Check(polevaulter.jumpOver && !polevaulter.isJump && blockedState == "zombie.walk" && blockedClip == polevaulter.walkAnimeClip && polevaulter.instance.collisionFlags == 1, $"高坚果必须立刻挡下撑杆跳并恢复行走；state={blockedState}, clip={blockedClip}。");
				Check(Mathf.IsEqualApprox(blockedX, tallnut.GetLogicalGlobalPosition().X + 40f), $"撑杆僵尸必须停在高坚果前方的拦截位置；x={blockedX}。");
				polevaulter.AnimeCompleted("Jump");
				await WaitFrames(2);
				Check(Mathf.IsEqualApprox(polevaulter.GetLogicalGlobalPosition().X, blockedX) && polevaulter.CurrentStateHandle?.StableId == blockedState && polevaulter.sprite.clip == blockedClip, "被高坚果挡下后，迟到的 Jump 完成回调不能再把撑杆僵尸送到高坚果背后。");
				await WaitFrames(6);
				Check(Mathf.IsEqualApprox(polevaulter.GetLogicalGlobalPosition().X, blockedX) && polevaulter.jumpOver && polevaulter.CurrentStateHandle?.StableId == "zombie.walk", "拦截后的后续帧必须保持单一行走状态和稳定停位。");
				goto end_IL_00f8;
				end_IL_0113:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"BugOverviewPolevaulterTallnutBlockRuntimeTest"}] 未预期异常：{value}");
				goto end_IL_00f8;
			}
			return;
			end_IL_00f8:;
		}
		finally
		{
			AudioManager.Instance?.AudioStopAll();
			if (GodotObject.IsInstanceValid(polevaulter))
			{
				polevaulter.QueueFree();
			}
			if (GodotObject.IsInstanceValid(tallnut))
			{
				tallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
				manager.backZombie = previousBackZombie;
			}
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
			ObjectManager.Instance?.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			polevaulterScene?.Dispose();
			tallnutScene?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WaitFrames(4);
		}
		bool flag2 = _failures == 0 && _checks == 14;
		GD.Print($"POLEVAULTER_TALLNUT_BLOCK_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I gridPos, Vector2 position)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = gridPos;
		character.SetLogicalGlobalPosition(position);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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
			GD.PushError("[BugOverviewPolevaulterTallnutBlockRuntimeTest] " + message);
		}
	}
}
