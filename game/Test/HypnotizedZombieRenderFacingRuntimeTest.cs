using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class HypnotizedZombieRenderFacingRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		TowerDefenseZombieNormal zombie = null;
		try
		{
			try
			{
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				zombie = Instantiate<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(zombie), "测试必须实例化真实普通僵尸场景。");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_0047;
				}
				zombie.inGame = false;
				zombie.editorPreviewMode = false;
				zombie.GlobalPosition = new Vector2(480f, 300f);
				AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitProcessFrames(4);
				Check(GodotObject.IsInstanceValid(zombie.sprite), "真实普通僵尸必须提供 Adobe Animate 身体根节点。");
				if (!GodotObject.IsInstanceValid(zombie.sprite))
				{
					goto end_IL_0047;
				}
				zombie.sprite.ClearRetainedRootMotionForStoppedMovement();
				Transform2D transform2D = ReadRenderedTransform(zombie.sprite);
				Check(zombie.Scale.X > 0f && !ReadRenderTransformDirty(zombie.sprite), "魅惑前必须是朝向左侧且渲染变换缓存干净的普通僵尸。");
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.Hypnoses();
				Check(zombie.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && zombie.instance.hypnoses, "魅惑必须立即把真实普通僵尸切换到植物阵营。");
				Check(zombie.Scale.X < 0f, "魅惑必须立即翻转真实普通僵尸的逻辑朝向。");
				Check(ReadRenderTransformDirty(zombie.sprite), "魅惑必须在普通轮询停用时同步标脏身体渲染变换。");
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { zombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				Transform2D transform2D2 = ReadRenderedTransform(zombie.sprite);
				Check(transform2D2.IsEqualApprox(zombie.sprite.GlobalTransform), "魅惑后的下一次 Crowd 事务必须消费翻转后的身体变换。");
				Check(transform2D.Determinant() * transform2D2.Determinant() < 0f, "魅惑后的渲染变换必须与普通僵尸使用相反朝向。");
				zombie.BuffDelete("Hypnoses");
				Check(zombie.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && !zombie.instance.hypnoses && zombie.Scale.X > 0f, "解除魅惑必须恢复真实普通僵尸的阵营和逻辑朝向。");
				Check(ReadRenderTransformDirty(zombie.sprite), "解除魅惑也必须同步标脏身体渲染变换。");
				AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { zombie.sprite }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				Transform2D transform2D3 = ReadRenderedTransform(zombie.sprite);
				Check(transform2D3.IsEqualApprox(zombie.sprite.GlobalTransform), "解除魅惑后的下一次 Crowd 事务必须消费恢复后的身体变换。");
				Check(transform2D.Determinant() * transform2D3.Determinant() > 0f, "解除魅惑后的渲染朝向必须恢复为普通僵尸朝向。");
				goto end_IL_003e;
				end_IL_0047:;
			}
			catch (Exception value)
			{
				_failures.Add($"运行测试出现异常：{value}");
				goto end_IL_003e;
			}
			end_IL_003e:;
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			Finish();
		}
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
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[HypnotizedZombieRenderFacing] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 12;
		GD.Print($"HYPNOTIZED_ZOMBIE_RENDER_FACING_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
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
}
