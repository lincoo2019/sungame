using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class CatZombieLaneChangeRenderTransformRuntimeTest : Node
{
	private const string CatZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Cat/Scene/TowerDefenseZombieCat.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		TowerDefenseZombieCat catZombie = null;
		try
		{
			_ = 2;
			try
			{
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				catZombie = Instantiate<TowerDefenseZombieCat>("res://Asset/Anime/Character/Zombie/Chapter3/Cat/Scene/TowerDefenseZombieCat.tscn");
				Check(GodotObject.IsInstanceValid(catZombie), "测试必须实例化真实猫战士僵尸场景。");
				if (!GodotObject.IsInstanceValid(catZombie))
				{
					throw new InvalidOperationException("无法实例化真实猫战士僵尸场景。");
				}
				catZombie.inGame = false;
				catZombie.editorPreviewMode = false;
				catZombie.gridPos = new Vector2I(4, 1);
				catZombie.GlobalPosition = new Vector2(400f, 196f);
				AddChild(catZombie, forceReadableName: false, InternalMode.Disabled);
				await WaitProcessFrames(6);
				Check(catZombie.config?.name == "ZombieCat", "测试对象必须保留猫战士正式配置。");
				Check(GodotObject.IsInstanceValid(catZombie.sprite), "真实猫战士必须提供 Adobe Animate 根贴图。");
				GarlicComponent runtime = catZombie.componentManager.GetRuntime<GarlicComponent>();
				Check(runtime != null && !runtime.IsReleased, "猫战士必须挂载正式大蒜换行组件。");
				if (!GodotObject.IsInstanceValid(catZombie.sprite) || runtime == null || runtime.IsReleased)
				{
					throw new InvalidOperationException("猫战士换行运行依赖未准备完成。");
				}
				catZombie.sprite.pause = true;
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { catZombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				catZombie.sprite.ClearRetainedRootMotionForStoppedMovement();
				Check(!ReadRenderTransformDirty(catZombie.sprite), "单步换行前必须先建立干净的身体渲染变换缓存。");
				Vector2 startPosition = catZombie.GetLogicalGlobalPosition();
				float num = startPosition.Y + 24f;
				InvokeLaneTweenStep(catZombie, num);
				Check(Mathf.IsEqualApprox(catZombie.GetLogicalGlobalPosition().Y, num), "正式换行补间回调必须写入猫战士逻辑 Y 坐标。");
				Check(ReadRenderedTransform(catZombie.sprite).IsEqualApprox(catZombie.sprite.GlobalTransform), "换行补间每步都必须立即推进猫战士身体的保留渲染变换。");
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { catZombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				Check(ReadRenderedTransform(catZombie.sprite).IsEqualApprox(catZombie.sprite.GlobalTransform), "换行补间后的下一次渲染事务必须消费新的猫战士身体变换。");
				catZombie.SetLogicalGlobalPosition(startPosition);
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { catZombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				catZombie.sprite.ClearRetainedRootMotionForStoppedMovement();
				runtime.changeLineDuration = 0.15f;
				Task laneChange = runtime.ChangeLine();
				Check(catZombie.isChangeLine, "正式换行任务启动后必须进入换行状态。");
				bool observedIntermediatePosition = false;
				bool everyRenderedTransformMatched = true;
				for (int frame = 0; frame < 60; frame++)
				{
					if (laneChange.IsCompleted)
					{
						break;
					}
					await WaitProcessFrames(1);
					if (!Mathf.IsEqualApprox(catZombie.GetLogicalGlobalPosition().Y, startPosition.Y))
					{
						observedIntermediatePosition = true;
					}
					AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { catZombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
					if (!ReadRenderedTransform(catZombie.sprite).IsEqualApprox(catZombie.sprite.GlobalTransform))
					{
						everyRenderedTransformMatched = false;
					}
				}
				await laneChange;
				Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { catZombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				Check(observedIntermediatePosition, "正式猫战士换行必须产生可观察的中间纵向位移。");
				Check(everyRenderedTransformMatched, "猫战士换行期间每次 GPU Crowd 提交都必须跟随当前身体位置。");
				Check(Mathf.IsEqualApprox(catZombie.GetLogicalGlobalPosition().Y, startPosition.Y + mapGridSize.Y), "猫战士换行结束后逻辑位置必须到达下一行。");
				Check(catZombie.gridPos.Y == 2, "猫战士换行结束后格子行号必须同步到下一行。");
				Check(ReadRenderedTransform(catZombie.sprite).IsEqualApprox(catZombie.sprite.GlobalTransform), "猫战士换行结束后的贴图不能留在原行。");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[CatZombieLaneChangeRenderTransformRuntimeTest] 运行测试出现异常：{value}");
			}
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			if (GodotObject.IsInstanceValid(catZombie))
			{
				catZombie.QueueFree();
			}
			await WaitProcessFrames(3);
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"CAT_ZOMBIE_LANE_RENDER_TRANSFORM_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void InvokeLaneTweenStep(TowerDefenseCharacter character, float value)
	{
		MethodInfo? method = typeof(GarlicComponent).GetMethod("SetLogicalGlobalPositionY", BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException(typeof(GarlicComponent).FullName, "SetLogicalGlobalPositionY");
		}
		method.Invoke(null, new object[2] { character, value });
	}

	private static bool ReadRenderTransformDirty(AdobeAnimateSprite sprite)
	{
		return ReadPrivateField<bool>(sprite, "_renderGlobalTransformDirty");
	}

	private static Transform2D ReadRenderedTransform(AdobeAnimateSprite sprite)
	{
		return ReadPrivateField<Transform2D>(sprite, "_renderGlobalTransform");
	}

	private static T ReadPrivateField<T>(object owner, string fieldName)
	{
		FieldInfo? field = typeof(AdobeAnimateSprite).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new MissingFieldException(typeof(AdobeAnimateSprite).FullName, fieldName);
		}
		return (T)field.GetValue(owner);
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CatZombieLaneChangeRenderTransformRuntimeTest] " + message);
		}
	}
}
