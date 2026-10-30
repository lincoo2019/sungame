using System;
using System.Collections.Generic;
using Godot;

public partial class AdobeAnimateClipBlendRuntimeProbe : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_CLIP_BLEND_RESULT";

	private const string WalkEatScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/ThreePeater/ZombieNormalThreePeater.tscn";

	public override async void _Ready()
	{
		bool previousRuntimeManager = AdobeAnimateRuntimeManager.UseRuntimeManager;
		bool passed = false;
		string failure = string.Empty;
		try
		{
			AdobeAnimateRuntimeManager.UseRuntimeManager = false;
			Node node = (GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/ThreePeater/ZombieNormalThreePeater.tscn") ?? throw new InvalidOperationException("Unable to load res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/ThreePeater/ZombieNormalThreePeater.tscn.")).Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is AdobeAnimateSprite sprite))
			{
				node.QueueFree();
				throw new InvalidOperationException("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/ThreePeater/ZombieNormalThreePeater.tscn did not instantiate an AdobeAnimateSprite.");
			}
			AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			sprite.SetFrozenPreview(frozen: true);
			EnableScreenDoorArmor(sprite);
			sprite.SetAnimation("Walk1");
			sprite.frameIndex = 75;
			sprite.elapsedTimer = 0.35;
			sprite.SetAnimation("Eat", loop: true, 0.5);
			if (!sprite.blend || !sprite.loop || sprite.clip != "Eat")
			{
				throw new InvalidOperationException($"Transition did not start: clip={sprite.clip} loop={sprite.loop} blend={sprite.blend}.");
			}
			AdobeAnimateRenderSnapshot snapshot;
			List<AdobeAnimateDrawItem> list = BuildAt(sprite, 0.0, out snapshot);
			AdobeAnimateRenderSnapshot snapshot2;
			List<AdobeAnimateDrawItem> list2 = BuildAt(sprite, 0.25, out snapshot2);
			AdobeAnimateRenderSnapshot snapshot3;
			List<AdobeAnimateDrawItem> list3 = BuildAt(sprite, 0.5, out snapshot3);
			ValidateStableSliceIdentity(snapshot);
			if (!snapshot.ClipBlend.Enabled || !snapshot2.ClipBlend.Enabled || !snapshot3.ClipBlend.Enabled || Math.Abs(snapshot.ClipBlend.FromFrameFloat - 75.35f) > 0.001f || Math.Abs(snapshot2.ClipBlend.Weight - 0.5f) > 0.001f || Math.Abs(snapshot3.ClipBlend.Weight - 1f) > 0.001f)
			{
				throw new InvalidOperationException($"Unexpected snapshot blend state: source={snapshot.ClipBlend.FromFrameFloat:F3} weights={snapshot.ClipBlend.Weight:F3}/{snapshot2.ClipBlend.Weight:F3}/{snapshot3.ClipBlend.Weight:F3}.");
			}
			if (!HasMidpointDrawItem(list, list2, list3, out var maxError))
			{
				throw new InvalidOperationException($"No body draw item followed a continuous midpoint blend; maxError={maxError:F6} counts={list.Count}/{list2.Count}/{list3.Count}.");
			}
			if (!HasMidpointLayerPose(sprite, out var maxError2))
			{
				throw new InvalidOperationException($"No layer/attachment pose followed a continuous midpoint blend; maxError={maxError2:F6}.");
			}
			sprite.blendTimer = 0.49;
			sprite.SetFrozenPreview(frozen: false);
			sprite.RunBatchedProcessUpdate(0.02);
			if (sprite.blend)
			{
				throw new InvalidOperationException("Blend timer did not complete and release the fallback path.");
			}
			passed = true;
		}
		catch (Exception ex)
		{
			failure = ex.ToString();
		}
		finally
		{
			AdobeAnimateRuntimeManager.UseRuntimeManager = previousRuntimeManager;
		}
		GD.Print(passed ? "ADOBE_ANIMATE_CLIP_BLEND_RESULT passed=True" : ("ADOBE_ANIMATE_CLIP_BLEND_RESULT passed=False failure=" + failure));
		GetTree().Quit((!passed) ? 2 : 0);
	}

	private static void EnableScreenDoorArmor(AdobeAnimateSprite sprite)
	{
		string[] array = new string[4] { "anim_screendoor", "Zombie_innerarm_screendoor", "Zombie_innerarm_screendoor_hand", "Zombie_outerarm_screendoor" };
		foreach (string text in array)
		{
			sprite.SetFliter(text, open: true);
		}
		array = new string[6] { "anim_innerarm1", "anim_innerarm2", "anim_innerarm3", "Zombie_outerarm_hand", "Zombie_outerarm_lower", "Zombie_outerarm_upper" };
		foreach (string text2 in array)
		{
			sprite.SetFliter(text2, open: false);
		}
	}

	private static void ValidateStableSliceIdentity(AdobeAnimateRenderSnapshot snapshot)
	{
		AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
		int num = Mathf.Clamp(Mathf.FloorToInt(snapshot.ClipBlend.FromFrameFloat), 0, definition.Frames.Length - 1);
		int num2 = Mathf.Clamp(Mathf.FloorToInt(snapshot.FrameFloat), 0, definition.Frames.Length - 1);
		PackedFrame sourceFrame = definition.Frames[num];
		PackedFrame targetFrame = definition.Frames[num2];
		int num3 = Math.Max(0, targetFrame.Offset);
		int num4 = Math.Min(definition.SliceMetadata.Length, num3 + Math.Max(0, targetFrame.Count));
		int num5 = 0;
		int num6 = 0;
		for (int i = num3; i < num4; i++)
		{
			int num7 = i - num3;
			int num8 = sourceFrame.Offset + num7;
			if (num7 < sourceFrame.Count && num8 >= 0 && num8 < definition.SliceMetadata.Length && definition.SliceMetadata[num8].SliceKey != definition.SliceMetadata[i].SliceKey)
			{
				num5++;
			}
			if (AdobeAnimateDefinitionCache.TryGetMatchingSliceIndexInFrame(definition, sourceFrame, targetFrame, i, out var sourceAbsoluteIndex))
			{
				num6++;
				if (definition.SliceMetadata[sourceAbsoluteIndex].SliceKey != definition.SliceMetadata[i].SliceKey)
				{
					throw new InvalidOperationException($"Stable slice identity mismatch target={i} source={sourceAbsoluteIndex}.");
				}
			}
		}
		if (num5 <= 0 || num6 <= 0)
		{
			throw new InvalidOperationException($"Walk1->Eat probe did not exercise reordered slice identities: reordered={num5} matched={num6}.");
		}
	}

	private static List<AdobeAnimateDrawItem> BuildAt(AdobeAnimateSprite sprite, double blendTimer, out AdobeAnimateRenderSnapshot snapshot)
	{
		sprite.blendTimer = blendTimer;
		if (!sprite.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false))
		{
			throw new InvalidOperationException($"Unable to build snapshot at blendTimer={blendTimer:F3}.");
		}
		List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
		AdobeAnimateDrawItemBuilder.Build(snapshot, list);
		if (list.Count == 0)
		{
			throw new InvalidOperationException($"Snapshot produced no draw items at blendTimer={blendTimer:F3}.");
		}
		return list;
	}

	private static bool HasMidpointDrawItem(List<AdobeAnimateDrawItem> start, List<AdobeAnimateDrawItem> middle, List<AdobeAnimateDrawItem> end, out float maxError)
	{
		maxError = 0f;
		if (start.Count != middle.Count || start.Count != end.Count)
		{
			return false;
		}
		int num = 0;
		int num2 = Math.Min(start.Count, Math.Min(middle.Count, end.Count));
		for (int i = 0; i < num2; i++)
		{
			if (!(TransformDistance(start[i].Transform, end[i].Transform) < 0.05f))
			{
				num++;
				Transform2D right = LerpTransform(start[i].Transform, end[i].Transform, 0.5f);
				float val = TransformDistance(middle[i].Transform, right);
				maxError = Math.Max(maxError, val);
			}
		}
		if (num > 0)
		{
			return maxError <= 0.002f;
		}
		return false;
	}

	private static bool HasMidpointLayerPose(AdobeAnimateSprite sprite, out float maxError)
	{
		maxError = 0f;
		if (sprite.flashAnimeData?.layerDictionary == null)
		{
			return false;
		}
		int num = 0;
		foreach (Variant value in sprite.flashAnimeData.layerDictionary.Values)
		{
			int layerId = value.AsInt32();
			sprite.blendTimer = 0.0;
			if (!sprite.TryGetInterpolatedLayerPoseForRender(layerId, out var mediaId, out var transform))
			{
				continue;
			}
			sprite.blendTimer = 0.25;
			if (sprite.TryGetInterpolatedLayerPoseForRender(layerId, out mediaId, out var transform2))
			{
				sprite.blendTimer = 0.5;
				if (sprite.TryGetInterpolatedLayerPoseForRender(layerId, out mediaId, out var transform3) && !(TransformDistance(transform, transform3) < 0.05f))
				{
					num++;
					float val = TransformDistance(transform2, LerpTransform(transform, transform3, 0.5f));
					maxError = Math.Max(maxError, val);
				}
			}
		}
		if (num > 0)
		{
			return maxError <= 0.002f;
		}
		return false;
	}

	private static Transform2D LerpTransform(Transform2D source, Transform2D target, float weight)
	{
		return new Transform2D(source.X + (target.X - source.X) * weight, source.Y + (target.Y - source.Y) * weight, source.Origin + (target.Origin - source.Origin) * weight);
	}

	private static float TransformDistance(Transform2D left, Transform2D right)
	{
		return Math.Max(Math.Max((left.X - right.X).Length(), (left.Y - right.Y).Length()), (left.Origin - right.Origin).Length());
	}
}
