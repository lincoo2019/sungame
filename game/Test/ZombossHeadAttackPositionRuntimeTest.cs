using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ZombossHeadAttackPositionRuntimeTest : Node
{
	private const string RegularSpriteScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/ZombieBoss.tscn";

	private const string DaveSpriteScenePath = "res://Asset/Anime/Character/Zombie/Boss/BossDave/ZombieBossDave.tscn";

	private int _checks;

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		try
		{
			Check(GodotObject.IsInstanceValid(instance), "测试必须能够使用正式战斗管理器。");
			if (!GodotObject.IsInstanceValid(instance))
			{
				throw new InvalidOperationException("缺少正式战斗管理器。");
			}
			await VerifySprite("res://Asset/Anime/Character/Zombie/Boss/Boss/ZombieBoss.tscn", isDave: false, "普通僵王");
			await VerifySprite("res://Asset/Anime/Character/Zombie/Boss/BossDave/ZombieBossDave.tscn", isDave: true, "戴夫僵王");
		}
		catch (Exception value)
		{
			_failures.Add($"运行测试出现异常：{value}");
		}
		finally
		{
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		Finish();
	}

	private async Task VerifySprite(string scenePath, bool isDave, string label)
	{
		PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		AdobeAnimateSprite sprite = scene?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(sprite), label + "必须从正式动画场景实例化。");
		if (!GodotObject.IsInstanceValid(sprite))
		{
			scene?.Dispose();
			return;
		}
		Node2D host = new Node2D
		{
			Name = label + "HeadAttackHost"
		};
		AddChild(host, forceReadableName: false, InternalMode.Disabled);
		host.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(3);
		Node2D armNode = sprite.GetNodeOrNull<Node2D>("%ArmNode");
		Check(GodotObject.IsInstanceValid(armNode), label + "必须保留正式机械臂节点。");
		AdobeAnimateGpuRenderGraphDefinition graph;
		AdobeAnimateSprite[] ownerSprites;
		string failureReason;
		bool flag = AdobeAnimateGpuRenderGraphBuilder.TryBuild(sprite, out graph, out ownerSprites, out failureReason);
		Check(flag && Array.Exists(ownerSprites, (AdobeAnimateSprite owner) => owner == sprite), $"{label}必须进入正式 GPU 渲染图；原因={failureReason}，拥有者={((ownerSprites != null) ? ownerSprites.Length : 0)}，槽位={(graph?.RenderSlots?.Length).GetValueOrDefault()}。");
		float initialHeadOffset = sprite.offset.Y;
		ulong initialPoseRevision = sprite.ManagedPoseRevision;
		BeginHeadAttack(sprite, isDave, 1, 1.0);
		await WaitFrames(6);
		float topLineHeadOffset = GetExpectedHeadOffset(1);
		Check(sprite.offset.Y < initialHeadOffset - 1f && !Mathf.IsEqualApprox(sprite.offset.Y, topLineHeadOffset), $"{label}吐球前头部补间必须真实移动且尚未结束；初始={initialHeadOffset}，当前={sprite.offset.Y}，目标={topLineHeadOffset}。");
		Check(sprite.ManagedPoseRevision > initialPoseRevision + 2, $"{label}完整 offset 补间必须持续发布 GPU 姿态；初始修订={initialPoseRevision}，当前修订={sprite.ManagedPoseRevision}。");
		await WaitFrames(90);
		Check(Mathf.IsEqualApprox(sprite.offset.Y, topLineHeadOffset), $"{label}补间完成后头部必须同步到第一行；期望={topLineHeadOffset}，实际={sprite.offset.Y}。");
		Check(GodotObject.IsInstanceValid(armNode) && Mathf.IsEqualApprox(armNode.Position.Y, GetExpectedArmOffset(1)), label + "补间完成后机械臂必须同步到第一行。");
		Check(!IsHeadTweenRunning(sprite, isDave), label + "补间完成后不得保留继续改写位置的头部补间。");
		int lastLine = TowerDefenseManager.Instance.GetMapGridNum().Y;
		BeginHeadAttack(sprite, isDave, lastLine, 1.0);
		await WaitFrames(90);
		Check(Mathf.IsEqualApprox(sprite.offset.Y, GetExpectedHeadOffset(lastLine)), label + "补间完成后头部必须同步到最后一行。");
		Check(GodotObject.IsInstanceValid(armNode) && Mathf.IsEqualApprox(armNode.Position.Y, GetExpectedArmOffset(lastLine)), label + "补间完成后机械臂必须同步到最后一行。");
		VerifyBallSpawnMarkerRefresh(sprite, isDave, label);
		sprite.QueueFree();
		host.QueueFree();
		await WaitFrames(3);
		scene.Dispose();
	}

	private void VerifyBallSpawnMarkerRefresh(AdobeAnimateSprite sprite, bool isDave, string label)
	{
		Node2D nodeOrNull = sprite.GetNodeOrNull<Node2D>("HeadSlot");
		Marker2D nodeOrNull2 = sprite.GetNodeOrNull<Marker2D>("%BallSpawnMarker");
		Check(GodotObject.IsInstanceValid(nodeOrNull), label + "必须保留正式头部插槽。");
		Check(GodotObject.IsInstanceValid(nodeOrNull2), label + "必须保留正式吐球 Marker。");
		if (!GodotObject.IsInstanceValid(nodeOrNull) || !GodotObject.IsInstanceValid(nodeOrNull2))
		{
			return;
		}
		sprite.SetAnimation("HeadAttack4", loop: false);
		sprite.frameIndex = sprite.clipRange.X;
		sprite.elapsedTimer = 0.0;
		sprite.UpdateChild();
		Vector2 globalPosition = nodeOrNull2.GlobalPosition;
		Vector2 to = (nodeOrNull.GlobalPosition = new Vector2(-4096f, -4096f));
		TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter
		{
			Name = label + "BallMarkerOwner"
		};
		try
		{
			Vector2 value = ReadBallSpawnMarkerPosition(sprite, isDave, towerDefenseCharacter);
			Check(value.DistanceTo(globalPosition) <= 0.5f, $"{label}吐球嘴部坐标读取前必须刷新当前帧头部插槽；期望={globalPosition}，实际={value}。");
			Check(value.DistanceTo(to) > 100f, $"{label}吐球嘴部坐标不得沿用旧 HeadSlot 左上角位置；实际={value}。");
		}
		finally
		{
			towerDefenseCharacter.Free();
		}
	}

	private static Vector2 ReadBallSpawnMarkerPosition(AdobeAnimateSprite sprite, bool isDave, TowerDefenseCharacter owner)
	{
		if (isDave && sprite is ZombieBossDave zombieBossDave)
		{
			return zombieBossDave.GetBallSpawnMarkerGlobalPos(owner);
		}
		if (sprite is ZombieBoss zombieBoss)
		{
			return zombieBoss.GetBallSpawnMarkerGlobalPos(owner);
		}
		return Vector2.Zero;
	}

	private static void BeginHeadAttack(AdobeAnimateSprite sprite, bool isDave, int line, double time)
	{
		if (isDave && sprite is ZombieBossDave zombieBossDave)
		{
			zombieBossDave.SetHeadAttack(line, time);
		}
		else if (sprite is ZombieBoss zombieBoss)
		{
			zombieBoss.SetHeadAttack(line, time);
		}
	}

	private static bool IsHeadTweenRunning(AdobeAnimateSprite sprite, bool isDave)
	{
		Tween tween = ((isDave && sprite is ZombieBossDave zombieBossDave) ? zombieBossDave.headTween : (sprite as ZombieBoss)?.headTween);
		if (GodotObject.IsInstanceValid(tween))
		{
			return tween.IsRunning();
		}
		return false;
	}

	private static float GetExpectedHeadOffset(int line)
	{
		int y = TowerDefenseManager.Instance.GetMapGridNum().Y;
		float y2 = TowerDefenseManager.Instance.GetMapGridSize().Y;
		int num = (int)Mathf.Floor((float)y / 2f) + 2;
		return -300f + y2 * (float)(line - num);
	}

	private static float GetExpectedArmOffset(int line)
	{
		int y = TowerDefenseManager.Instance.GetMapGridNum().Y;
		float y2 = TowerDefenseManager.Instance.GetMapGridSize().Y;
		int num = (int)Mathf.Floor((float)y / 2f);
		return y2 * (float)(line - num * 2);
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

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[ZombossHeadAttackPositionRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 29;
		GD.Print($"ZOMBOSS_HEAD_ATTACK_POSITION_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
