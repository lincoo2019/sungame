using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class PolevaulterLandingRenderTransformRuntimeTest : Node
{
	private const string PolevaulterScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn";

	private const string PolestrikerScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Polestriker/Scene/TowerDefenseZombiePolestriker.tscn";

	private static readonly Vector2I StartGrid = new Vector2I(4, 2);

	private static readonly Vector2 StartPosition = new Vector2(400f, 252f);

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousBackZombie = manager?.backZombie ?? false;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		TowerDefenseZombiePolevaulter polevaulter = null;
		TowerDefenseZombiePolestriker polestriker = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "测试必须加载 TowerDefenseManager 自动加载节点。");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00fc;
				}
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				TowerDefenseZombie.UseBatch = false;
				PolevaulterLandingRenderTransformControlStub polevaulterLandingRenderTransformControlStub = new PolevaulterLandingRenderTransformControlStub
				{
					Name = "PolevaulterLandingRenderTransformControl",
					isGameRunning = false,
					isInit = true
				};
				AddChild(polevaulterLandingRenderTransformControlStub, forceReadableName: false, InternalMode.Disabled);
				polevaulterLandingRenderTransformControlStub.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				polevaulterLandingRenderTransformControlStub.AddChild(polevaulterLandingRenderTransformControlStub.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = polevaulterLandingRenderTransformControlStub;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				manager.backZombie = false;
				polevaulter = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn", null, ResourceLoader.CacheMode.IgnoreDeep)?.Instantiate<TowerDefenseZombiePolevaulter>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(polevaulter), "测试必须实例化第一章正式撑杆跳僵尸场景。");
				if (!GodotObject.IsInstanceValid(polevaulter))
				{
					goto end_IL_00fc;
				}
				polestriker = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter2/Polestriker/Scene/TowerDefenseZombiePolestriker.tscn", null, ResourceLoader.CacheMode.IgnoreDeep)?.Instantiate<TowerDefenseZombiePolestriker>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(polestriker), "测试必须实例化第二章正式支撑杆僵尸场景。");
				if (!GodotObject.IsInstanceValid(polestriker))
				{
					goto end_IL_00fc;
				}
				PrepareCharacter(polevaulter);
				PrepareCharacter(polestriker);
				polevaulterLandingRenderTransformControlStub.characterNode.AddChild(polevaulter, forceReadableName: false, InternalMode.Disabled);
				polevaulterLandingRenderTransformControlStub.characterNode.AddChild(polestriker, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				polevaulter.ProcessMode = ProcessModeEnum.Disabled;
				polestriker.ProcessMode = ProcessModeEnum.Disabled;
				PrepareCharacter(polevaulter);
				PrepareCharacter(polestriker);
				polestriker.transformPoint.Scale = new Vector2(2.25f, 1.75f);
				polevaulter.groundMoveComponent?.SetAlive(false);
				polestriker.groundMoveComponent?.SetAlive(false);
				await WaitFrames(2);
				Check(polevaulter.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn" && polevaulter.config?.name == "ZombiePolevaulter" && (polevaulter.StateMachine?.IsInitialized ?? false) && GodotObject.IsInstanceValid(polevaulter.sprite) && polevaulter.sprite.HasClip("Jump"), "测试必须使用带正式状态机和 Jump 动画的第一章撑杆跳僵尸。");
				if (!GodotObject.IsInstanceValid(polevaulter.sprite))
				{
					goto end_IL_00fc;
				}
				bool flag = polevaulter.SendStateEvent("ToJump");
				Check(flag && polevaulter.CurrentStateHandle?.StableId == "zombie.polevaulter.jump" && polevaulter.sprite.clip == "Jump", "正式状态机必须进入撑杆跳 Jump 动画。");
				polevaulter.AnimeEvent("check", default(Variant));
				polevaulter.AnimeEvent("jumpOver", default(Variant));
				polevaulter.sprite.ClearRetainedRootMotionForStoppedMovement();
				Transform2D other = ReadRenderedTransform(polevaulter.sprite);
				Check(!ReadRenderTransformDirty(polevaulter.sprite) && other.IsEqualApprox(polevaulter.sprite.GlobalTransform), "落地前必须先建立干净且位于起跳点的渲染变换缓存。");
				float b = polevaulter.GetLogicalGlobalPosition().X - (float)((double)(polevaulter.Scale.X * polevaulter.transformPoint.Scale.X) * 148.0);
				polevaulter.AnimeCompleted("Jump");
				Check(Mathf.IsEqualApprox(polevaulter.GetLogicalGlobalPosition().X, b), "Jump 完成回调必须应用一次正式的 148 像素落地位移。");
				Transform2D transform2D = ReadRenderedTransform(polevaulter.sprite);
				Check(!ReadRenderTransformDirty(polevaulter.sprite) && transform2D.IsEqualApprox(polevaulter.sprite.GlobalTransform) && !transform2D.IsEqualApprox(other), "落地坐标写入必须在同一次调用中推进 Adobe Animate 身体变换，不能等待后续脏标记。");
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { polevaulter.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				Transform2D transform2D2 = ReadRenderedTransform(polevaulter.sprite);
				Check(transform2D2.IsEqualApprox(polevaulter.sprite.GlobalTransform) && !transform2D2.IsEqualApprox(other), "下一次 GPU Crowd 渲染事务必须消费落地点变换，不能继续复用起跳点贴图位置。");
				Check(polestriker.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter2/Polestriker/Scene/TowerDefenseZombiePolestriker.tscn" && polestriker.config?.name == "ZombiePolestriker" && (polestriker.StateMachine?.IsInitialized ?? false) && GodotObject.IsInstanceValid(polestriker.sprite) && polestriker.sprite.HasClip("Jump"), "测试必须使用带正式状态机和 Jump 动画的第二章支撑杆僵尸。");
				Check(polestriker.transformPoint.Scale.X > 2f, "支撑杆回归必须在大体型缩放下执行。");
				if (!GodotObject.IsInstanceValid(polestriker.sprite))
				{
					goto end_IL_00fc;
				}
				bool flag2 = polestriker.SendStateEvent("ToJump");
				Check(flag2 && polestriker.CurrentStateHandle?.StableId == "zombie.polestriker.jump" && polestriker.sprite.clip == "Jump", "正式状态机必须让支撑杆僵尸进入 Jump 动画。");
				polestriker.AnimeEvent("jumpOver", default(Variant));
				polestriker.sprite.ClearRetainedRootMotionForStoppedMovement();
				Transform2D other2 = ReadRenderedTransform(polestriker.sprite);
				Check(!ReadRenderTransformDirty(polestriker.sprite) && other2.IsEqualApprox(polestriker.sprite.GlobalTransform), "支撑杆落地前必须先建立干净且位于起跳点的渲染变换缓存。");
				float x = polestriker.GetLogicalGlobalPosition().X;
				float num = Mathf.Sign(polestriker.Scale.X * polestriker.transformPoint.Scale.X);
				if (Mathf.IsZeroApprox(num))
				{
					num = 1f;
				}
				float b2 = x - num * (manager.GetMapGridSize().X + 148f);
				polestriker.AnimeCompleted("Jump");
				Check(Mathf.IsEqualApprox(polestriker.GetLogicalGlobalPosition().X, b2), "支撑杆 Jump 完成回调必须落到起跳时固定的格宽加 148 像素终点。");
				Check(ReadRenderedTransform(polestriker.sprite).IsEqualApprox(polestriker.sprite.GlobalTransform), "支撑杆落地坐标写入必须立即推进 Adobe Animate 根节点的保留渲染变换。");
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { polestriker.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				Transform2D transform2D3 = ReadRenderedTransform(polestriker.sprite);
				Check(transform2D3.IsEqualApprox(polestriker.sprite.GlobalTransform) && !transform2D3.IsEqualApprox(other2), "下一次 GPU Crowd 渲染事务必须保持支撑杆落地点变换，不能继续复用起跳点贴图位置。");
				goto end_IL_00ea;
				end_IL_00fc:;
			}
			catch (Exception value)
			{
				_failures.Add($"运行测试出现异常：{value}");
				goto end_IL_00ea;
			}
			end_IL_00ea:;
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
				manager.backZombie = previousBackZombie;
			}
			if (GodotObject.IsInstanceValid(polevaulter))
			{
				polevaulter.QueueFree();
			}
			if (GodotObject.IsInstanceValid(polestriker))
			{
				polestriker.QueueFree();
			}
			await WaitFrames(3);
			Finish();
		}
	}

	private static void PrepareCharacter(TowerDefenseZombiePolevaulter polevaulter)
	{
		polevaulter.editorPreviewMode = false;
		polevaulter.inGame = true;
		polevaulter.gridPos = StartGrid;
		polevaulter.GlobalPosition = StartPosition;
	}

	private static void PrepareCharacter(TowerDefenseZombiePolestriker polestriker)
	{
		polestriker.editorPreviewMode = false;
		polestriker.inGame = true;
		polestriker.gridPos = StartGrid;
		polestriker.GlobalPosition = StartPosition;
	}

	private static bool ReadRenderTransformDirty(AdobeAnimateSprite sprite)
	{
		return ReadPrivateField<bool>(sprite, "_renderGlobalTransformDirty");
	}

	private static Transform2D ReadRenderedTransform(AdobeAnimateSprite sprite)
	{
		return ReadPrivateField<Transform2D>(sprite, "_renderGlobalTransform");
	}

	private static T ReadPrivateField<T>(AdobeAnimateSprite sprite, string fieldName)
	{
		FieldInfo? field = typeof(AdobeAnimateSprite).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new MissingFieldException(typeof(AdobeAnimateSprite).FullName, fieldName);
		}
		return (T)field.GetValue(sprite);
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
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[PolevaulterLandingRenderTransform] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 16;
		GD.Print($"POLEVAULTER_LANDING_RENDER_TRANSFORM_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
