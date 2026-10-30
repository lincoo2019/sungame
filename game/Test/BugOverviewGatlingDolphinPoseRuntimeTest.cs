using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGatlingDolphinPoseRuntimeTest : Node
{
	private const string CharacterScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinriderGatlingPea.tscn";

	private static readonly string[] MovementClips = new string[4] { "DolphinWalk", "JumpInWater", "DolphinRun", "DolphinJump" };

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		GatlingDolphinPoseControlStub control = null;
		TowerDefenseZombieDolphinriderGatlingPea zombie = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00c7;
				}
				control = new GatlingDolphinPoseControlStub
				{
					Name = "GatlingDolphinPoseControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinriderGatlingPea.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieDolphinriderGatlingPea>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "The real Gatling Dolphin Rider character scene must instantiate.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_00c7;
				}
				zombie.editorPreviewMode = true;
				zombie.inGame = false;
				zombie.GlobalPosition = new Vector2(650f, 228f);
				zombie.gridPos = new Vector2I(6, 3);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				AdobeAnimateSprite body = zombie.sprite;
				AdobeAnimateSprite head = zombie.GetNodeOrNull<AdobeAnimateSprite>("SpriteGroup/TransformPoint/ZombieDolphinriderGatlingPea/Head");
				AdobeAnimateSlot armSlot = zombie.GetNodeOrNull<AdobeAnimateSlot>("SpriteGroup/TransformPoint/ZombieDolphinriderGatlingPea/ArmSlot");
				Check(GodotObject.IsInstanceValid(body) && GodotObject.IsInstanceValid(head) && GodotObject.IsInstanceValid(armSlot), "The real character must expose its body, nested Gatling Head, and ArmSlot.");
				if (!GodotObject.IsInstanceValid(body) || !GodotObject.IsInstanceValid(head) || !GodotObject.IsInstanceValid(armSlot))
				{
					goto end_IL_00c7;
				}
				bool flag = true;
				string[] movementClips = MovementClips;
				foreach (string clipName in movementClips)
				{
					flag &= body.HasClip(clipName);
				}
				Check(flag, "The shared dolphin body must retain all walk, water-entry, run, and jump clips.");
				Check(head.Visible, "The nested Gatling Head must begin visible in the real character scene.");
				Check(head.parentSprite == body && head.insertLayerId == 19 && head.followParentSpriteLayerId == 19, "The nested Gatling Head must insert into and follow authored body layer 19.");
				Check(armSlot.followSlotId == 27 && armSlot.drawLayerId == 27, "ArmSlot must continue to follow and draw at authored outer-arm layer 27.");
				await VerifyRunTransitionRefreshesHeadAttachment(zombie, body, head);
				string[] movementClips2 = MovementClips;
				foreach (string clip in movementClips2)
				{
					await VerifyMovementClip(zombie, body, head, armSlot, clip);
				}
				goto end_IL_00b0;
				end_IL_00c7:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGatlingDolphinPoseRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b0;
			}
			return;
			end_IL_00b0:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag2 = _failures == 0 && _checks == 26;
		GD.Print($"GATLING_DOLPHIN_POSE_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private async Task VerifyRunTransitionRefreshesHeadAttachment(TowerDefenseZombieDolphinriderGatlingPea zombie, AdobeAnimateSprite body, AdobeAnimateSprite head)
	{
		body.offset = new Vector2(-40f, body.offset.Y);
		body.SetAnimation("JumpInWater");
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		await WaitFrames(2);
		AdobeAnimateGpuRenderGraphDefinition beforeGraph;
		AdobeAnimateGpuRenderOwner headOwner;
		bool resolvedBefore = TryResolveHeadGraphOwner(body, head, out beforeGraph, out headOwner);
		Check(resolvedBefore, "The JumpInWater pose must resolve a GPU graph owner for the nested Gatling Head.");
		Check(resolvedBefore && Mathf.IsEqualApprox(headOwner.ParentOffset.X, -40f), $"The pre-run Gatling Head graph must retain body offset -40; actual={headOwner.ParentOffset.X}.");
		body.SetAnimation("DolphinRun");
		int mediaId;
		Transform2D transform;
		bool flag = body.TryGetInterpolatedSlotPose(head.followParentSpriteLayerId, out mediaId, out transform);
		Vector2 vector = transform.Origin + body.offset;
		Check(Mathf.IsEqualApprox(body.offset.X, 24f) && flag && mediaId != 65535 && head.Position.IsEqualApprox(vector), $"DolphinRun must immediately reattach the Gatling Head after body offset changes to 24; actual={head.Position}, expected={vector}, offset={body.offset}.");
		await WaitFrames(2);
		AdobeAnimateGpuRenderGraphDefinition graph;
		AdobeAnimateGpuRenderOwner headOwner2;
		bool flag2 = TryResolveHeadGraphOwner(body, head, out graph, out headOwner2);
		Check(flag2, "The DolphinRun pose must resolve a GPU graph owner for the nested Gatling Head.");
		Check(flag2 && Mathf.IsEqualApprox(headOwner2.ParentOffset.X, 24f), $"The DolphinRun Gatling Head graph must rebuild with body offset 24; actual={headOwner2.ParentOffset.X}.");
		Check(resolvedBefore && flag2 && beforeGraph.Signature != graph.Signature, $"Changing the body attachment offset must invalidate the cached GPU graph; before=0x{beforeGraph?.Signature ?? 0:X16}, after=0x{graph?.Signature ?? 0:X16}.");
		Vector2 globalPosition = head.GlobalPosition;
		Vector2 vector2 = new Vector2(-64f, 0f);
		zombie.GlobalPosition += vector2;
		Check(head.GlobalPosition.IsEqualApprox(globalPosition + vector2), "The Gatling Head must remain attached when the water-entry completion shifts the character root.");
	}

	private static bool TryResolveHeadGraphOwner(AdobeAnimateSprite body, AdobeAnimateSprite head, out AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateGpuRenderOwner headOwner)
	{
		headOwner = default(AdobeAnimateGpuRenderOwner);
		if (!AdobeAnimateRenderManager.TryResolveGpuRenderGraph(body, out graph, out var graphOwners, out var _, out var _, out var _))
		{
			return false;
		}
		for (int i = 0; i < graphOwners.Length; i++)
		{
			if (graphOwners[i] == head)
			{
				headOwner = graph.Owners[i];
				return true;
			}
		}
		return false;
	}

	private async Task VerifyMovementClip(TowerDefenseZombieDolphinriderGatlingPea zombie, AdobeAnimateSprite body, AdobeAnimateSprite head, AdobeAnimateSlot armSlot, string clip)
	{
		body.SetAnimation(clip);
		bool everySampleRenderedHead = true;
		bool everySampleHasHeadPose = true;
		bool everySampleHasArmPose = true;
		int minimumHeadItems = 2147483647;
		for (int sample = 0; sample < 12; sample++)
		{
			zombie.GlobalPosition += new Vector2(-2f, 0f);
			await WaitFrames(1);
			AdobeAnimateRenderSnapshot snapshot;
			bool flag = body.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false);
			List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
			if (flag)
			{
				AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition?.GpuPoseTextureArray);
			}
			int num = 0;
			foreach (AdobeAnimateDrawItem item in list)
			{
				if (item.Owner == head)
				{
					num++;
				}
			}
			minimumHeadItems = Math.Min(minimumHeadItems, num);
			everySampleRenderedHead = everySampleRenderedHead && flag && num > 0;
			int mediaId;
			Transform2D transform;
			bool flag2 = body.TryGetInterpolatedSlotPose(head.followParentSpriteLayerId, out mediaId, out transform);
			everySampleHasHeadPose &= flag2 && mediaId != 65535 && IsSane(transform);
			int mediaId2;
			Transform2D transform2;
			bool flag3 = body.TryGetInterpolatedSlotPose(armSlot.followSlotId - 1, out mediaId2, out transform2);
			everySampleHasArmPose &= flag3 && mediaId2 != 65535 && IsSane(transform2);
		}
		Check(everySampleRenderedHead, $"The nested Gatling Head must render in all 12 moving {clip} samples; minimum={minimumHeadItems}.");
		Check(everySampleHasHeadPose, "Body layer 19 must provide a finite Gatling Head follow pose in all 12 " + clip + " samples.");
		Check(everySampleHasArmPose, "Body layer 27 must provide a finite outer-arm pose in all 12 " + clip + " samples.");
	}

	private static bool IsSane(Transform2D transform)
	{
		if (IsSane(transform.X.X) && IsSane(transform.X.Y) && IsSane(transform.Y.X) && IsSane(transform.Y.Y) && IsSane(transform.Origin.X))
		{
			return IsSane(transform.Origin.Y);
		}
		return false;
	}

	private static bool IsSane(float value)
	{
		if (float.IsFinite(value))
		{
			return Math.Abs(value) < 4096f;
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewGatlingDolphinPoseRuntimeTest] " + message);
		}
	}
}
