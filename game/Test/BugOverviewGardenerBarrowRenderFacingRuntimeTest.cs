using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGardenerBarrowRenderFacingRuntimeTest : Node
{
	private const string GardenerScenePath = "res://Asset/Anime/Character/Zombie/Puzzle/Gardener/Scene/TowerDefenseZombieGardener.tscn";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		TowerDefenseZombieGardener gardener = null;
		TowerDefensePlant plant = null;
		try
		{
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			gardener = Instantiate<TowerDefenseZombieGardener>("res://Asset/Anime/Character/Zombie/Puzzle/Gardener/Scene/TowerDefenseZombieGardener.tscn");
			plant = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
			Check(GodotObject.IsInstanceValid(gardener), "测试必须实例化真实园丁僵尸场景。");
			Check(GodotObject.IsInstanceValid(plant), "测试必须实例化真实坚果植物场景。");
			if (!GodotObject.IsInstanceValid(gardener) || !GodotObject.IsInstanceValid(plant))
			{
				throw new InvalidOperationException("无法实例化园丁僵尸或坚果植物。");
			}
			PrepareCharacter(gardener, new Vector2I(4, 2), new Vector2(420f, 252f));
			PrepareCharacter(plant, new Vector2I(4, 2), new Vector2(420f, 252f));
			AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			AddChild(gardener, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(6);
			GroundMoveComponent groundMoveComponent = gardener.groundMoveComponent;
			Check(gardener.config?.name == "ZombieGardener", "测试对象必须保留园丁僵尸正式配置。");
			Check(GodotObject.IsInstanceValid(gardener.sprite), "园丁僵尸必须提供 Adobe Animate 身体根节点。");
			Check(groundMoveComponent != null && !groundMoveComponent.IsReleased, "园丁僵尸必须挂载正式 GroundMoveComponent。");
			if (!GodotObject.IsInstanceValid(gardener.sprite) || groundMoveComponent == null || groundMoveComponent.IsReleased)
			{
				throw new InvalidOperationException("园丁僵尸渲染或移动依赖未准备完成。");
			}
			gardener.sprite.pause = false;
			gardener.sprite.blend = false;
			PrimeCleanRenderTransform(gardener.sprite);
			Transform2D transform2D = ReadRenderedTransform(gardener.sprite);
			float num = Mathf.Sign(gardener.Scale.X);
			Check(!ReadRenderTransformDirty(gardener.sprite), "搬运前必须先建立干净的身体渲染变换缓存。");
			Check(num > 0f, "搬运前园丁应保持普通僵尸朝左的逻辑朝向。");
			gardener.Barrow(plant);
			gardener.TurnTowardBarrowExit();
			Check(gardener.barrowPlant == plant && !plant.inGame, "正式搬运逻辑必须把坚果挂到搬运车上并退出战斗更新。");
			Check((float)Mathf.Sign(gardener.Scale.X) == 0f - num, "偷到植物后园丁必须立即调头面向离场方向。");
			Check(ReadRenderTransformDirty(gardener.sprite), "调头必须立即标脏园丁身体渲染变换，不能等下一帧原生通知。");
			float x = gardener.GlobalPosition.X;
			gardener.TranslateForPhysicsFrame(new Vector2(6f, 0f), new Vector2(6f, 0f), Engine.GetPhysicsFrames());
			float num2 = gardener.GlobalPosition.X - x;
			Check(num2 > 0.01f, $"调头后的同帧位移必须向右离场，实际 deltaX={num2:F4}。");
			Check(ReadRenderTransformDirty(gardener.sprite), "同帧右移不能吞掉调头留下的身体渲染脏标记。");
			AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { gardener.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
			Transform2D transform2D2 = ReadRenderedTransform(gardener.sprite);
			Check(transform2D2.IsEqualApprox(gardener.sprite.GlobalTransform), "离场右移后的 GPU Crowd 提交必须消费当前园丁身体变换。");
			Check(transform2D.Determinant() * transform2D2.Determinant() < 0f, "离场右移后的身体渲染朝向必须与搬运前相反。");
			Check(transform2D2.Origin.X > transform2D.Origin.X + 0.01f, $"离场右移后的身体渲染位置必须同步向右推进，renderDeltaX={transform2D2.Origin.X - transform2D.Origin.X:F4}。");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewGardenerBarrowRenderFacingRuntimeTest] 运行测试出现异常：{value}");
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			if (GodotObject.IsInstanceValid(gardener))
			{
				gardener.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"GARDENER_BARROW_RENDER_FACING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I gridPosition, Vector2 globalPosition)
	{
		character.editorPreviewMode = false;
		character.inGame = false;
		character.gridPos = gridPosition;
		character.GlobalPosition = globalPosition;
	}

	private static void PrimeCleanRenderTransform(AdobeAnimateSprite sprite)
	{
		AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
		sprite.ClearRetainedRootMotionForStoppedMovement();
	}

	private static Transform2D ReadRenderedTransform(AdobeAnimateSprite sprite)
	{
		return ReadPrivateField<Transform2D>(sprite, "_renderGlobalTransform");
	}

	private static bool ReadRenderTransformDirty(AdobeAnimateSprite sprite)
	{
		return ReadPrivateField<bool>(sprite, "_renderGlobalTransformDirty");
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

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
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
			GD.PushError("[BugOverviewGardenerBarrowRenderFacingRuntimeTest] " + message);
		}
	}
}
