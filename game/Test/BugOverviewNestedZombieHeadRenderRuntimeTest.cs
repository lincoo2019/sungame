using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewNestedZombieHeadRenderRuntimeTest : Node
{
	private const string ChomperScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Chomper/TowerDefenseZombieNormalChomper.tscn";

	private const string GatlingZamboniScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/GatlingPea/TowerDefenseZombieZamboniGatlingPea.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		NestedZombieHeadRenderControlStub control = null;
		TowerDefenseZombieNormalChomper chomper = null;
		TowerDefenseZombieZamboniGatlingPea gatlingZamboni = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00ce;
				}
				control = new NestedZombieHeadRenderControlStub
				{
					Name = "NestedZombieHeadRenderControl",
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
				chomper = Instantiate<TowerDefenseZombieNormalChomper>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Chomper/TowerDefenseZombieNormalChomper.tscn");
				gatlingZamboni = Instantiate<TowerDefenseZombieZamboniGatlingPea>("res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/GatlingPea/TowerDefenseZombieZamboniGatlingPea.tscn");
				Check(GodotObject.IsInstanceValid(chomper), "The real Chomper Zombie scene must instantiate.");
				Check(GodotObject.IsInstanceValid(gatlingZamboni), "The real Gatling Zamboni scene must instantiate.");
				if (!GodotObject.IsInstanceValid(chomper) || !GodotObject.IsInstanceValid(gatlingZamboni))
				{
					goto end_IL_00ce;
				}
				Prepare(chomper, new Vector2(350f, 152f), new Vector2I(3, 2));
				Prepare(gatlingZamboni, new Vector2(650f, 228f), new Vector2I(6, 3));
				control.characterNode.AddChild(chomper, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(gatlingZamboni, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				await VerifyNestedHead(chomper, "SpriteGroup/TransformPoint/ZombieNormalChomper/Head", 7, new string[2] { "Idle2", "Walk1&Walk2" }, "Chomper Zombie");
				await VerifyNestedHead(gatlingZamboni, "SpriteGroup/TransformPoint/ZombieZamboniGatlingPea/Head", 11, new string[2] { "Drive", "Wheelie&Wheelie2" }, "Gatling Zamboni");
				goto end_IL_00b7;
				end_IL_00ce:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewNestedZombieHeadRenderRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(chomper))
			{
				chomper.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gatlingZamboni))
			{
				gatlingZamboni.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 29;
		GD.Print($"NESTED_ZOMBIE_HEAD_RENDER_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyNestedHead(TowerDefenseCharacter character, NodePath headPath, int expectedFollowLayer, string[] bodyClips, string label)
	{
		AdobeAnimateSprite body = character.sprite;
		AdobeAnimateSprite head = character.GetNodeOrNull<AdobeAnimateSprite>(headPath);
		Check(GodotObject.IsInstanceValid(body) && GodotObject.IsInstanceValid(head), label + " must expose its real body and nested Head sprites.");
		if (!GodotObject.IsInstanceValid(body) || !GodotObject.IsInstanceValid(head))
		{
			return;
		}
		Check(head.Visible, label + " nested Head must begin visible.");
		Check(head.parentSprite == body && head.followParentSpriteLayerId == expectedFollowLayer, $"{label} nested Head must remain attached to authored body layer {expectedFollowLayer}.");
		Check(head.flashAnimeData != null && head.flashAnimeData.mediaDictionary.Count > 0 && head.flashAnimeData.clips.Count > 0, label + " nested Head must retain its real animation media and clips.");
		int num = 0;
		int num2 = Math.Min(body.mediaReplace.Count, body.mediaReplaceUse.Count);
		for (int i = 0; i < num2; i++)
		{
			if (body.mediaReplaceUse[i] && GodotObject.IsInstanceValid(body.mediaReplace[i]))
			{
				num++;
			}
		}
		Check(num > 0, label + " must retain its serialized body media replacements after initialization.");
		Check(body.mediaReplaceAtlasShared && body.mediaReplaceAtlasUsesTextureArray && GodotObject.IsInstanceValid(body.mediaReplaceAtlasArray), label + " serialized body media replacements must resolve into the shared texture array before first render.");
		AdobeAnimateGpuRenderGraphDefinition graph;
		AdobeAnimateSprite[] ownerSprites;
		string failureReason;
		bool graphBuilt = AdobeAnimateGpuRenderGraphBuilder.TryBuild(body, out graph, out ownerSprites, out failureReason);
		Check(graphBuilt, label + " must build a GPU render graph containing its nested Head; reason=" + failureReason + ".");
		int headOwnerIndex = -1;
		if (graphBuilt)
		{
			for (int j = 0; j < ownerSprites.Length; j++)
			{
				if (ownerSprites[j] == head)
				{
					headOwnerIndex = j;
					break;
				}
			}
		}
		Check(headOwnerIndex > 0, $"{label} nested Head must be a child owner in the GPU render graph; owners={((ownerSprites != null) ? ownerSprites.Length : 0)}.");
		int num3 = 0;
		if (headOwnerIndex > 0)
		{
			AdobeAnimateGpuRenderSlot[] renderSlots = graph.RenderSlots;
			foreach (AdobeAnimateGpuRenderSlot adobeAnimateGpuRenderSlot in renderSlots)
			{
				if (adobeAnimateGpuRenderSlot.OwnerIndex == headOwnerIndex)
				{
					num3++;
				}
			}
		}
		Check(num3 > 0, $"{label} nested Head GPU owner must expose render slots; slots={num3}.");
		foreach (string bodyClip in bodyClips)
		{
			body.SetAnimation(bodyClip);
			bool everySampleRenderedHead = true;
			bool everyGpuSampleRenderedHead = graphBuilt && headOwnerIndex > 0;
			int minimumHeadItems = 2147483647;
			int minimumGpuHeadSlots = 2147483647;
			for (int sample = 0; sample < 12; sample++)
			{
				await WaitFrames(1);
				AdobeAnimateRenderSnapshot snapshot;
				bool flag = body.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false);
				List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
				if (flag)
				{
					AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition?.GpuPoseTextureArray);
				}
				int num4 = 0;
				foreach (AdobeAnimateDrawItem item in list)
				{
					if (item.Owner == head)
					{
						num4++;
					}
				}
				minimumHeadItems = Math.Min(minimumHeadItems, num4);
				everySampleRenderedHead = everySampleRenderedHead && flag && num4 > 0;
				AdobeAnimateCrowdRenderState state;
				AdobeAnimateCrowdRenderStateResult num5 = body.TryBuildCrowdRenderState(out state);
				int num6 = 0;
				if (num5 == AdobeAnimateCrowdRenderStateResult.Submitted && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph && headOwnerIndex < state.GpuGraphOwners.Length && state.GpuGraphOwners[headOwnerIndex] == head && head.TryBuildGpuGraphOwnerStateForRender(state.RenderMountParent, out var state2))
				{
					AdobeAnimateGpuRenderOwner adobeAnimateGpuRenderOwner = graph.Owners[headOwnerIndex];
					int num7 = Math.Clamp(state2.FrameIndex, 0, Math.Max(0, adobeAnimateGpuRenderOwner.Definition.Frames.Length - 1));
					for (int m = 0; m < adobeAnimateGpuRenderOwner.LocalSlotCount; m++)
					{
						int num8 = adobeAnimateGpuRenderOwner.FrameLookupBase + num7 * adobeAnimateGpuRenderOwner.LocalSlotCount + m;
						if ((uint)num8 < (uint)graph.FrameSlotLookup.Length && graph.FrameSlotLookup[num8].Visible)
						{
							num6++;
						}
					}
				}
				minimumGpuHeadSlots = Math.Min(minimumGpuHeadSlots, num6);
				everyGpuSampleRenderedHead = everyGpuSampleRenderedHead && num6 > 0;
			}
			Check(everySampleRenderedHead, $"{label} nested Head must contribute GPU-pose draw items in all 12 {bodyClip} samples; minimum={minimumHeadItems}.");
			Check(everyGpuSampleRenderedHead, $"{label} nested Head must remain visible in GPU Graph state in all 12 {bodyClip} samples; minimum={minimumGpuHeadSlots}.");
		}
	}

	private static void Prepare(TowerDefenseCharacter character, Vector2 position, Vector2I gridPos)
	{
		character.editorPreviewMode = true;
		character.inGame = false;
		character.GlobalPosition = position;
		character.gridPos = gridPos;
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
			GD.PushError("[BugOverviewNestedZombieHeadRenderRuntimeTest] " + message);
		}
	}
}
