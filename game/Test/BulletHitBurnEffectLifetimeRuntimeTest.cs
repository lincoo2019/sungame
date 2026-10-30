using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;

public partial class BulletHitBurnEffectLifetimeRuntimeTest : Node
{
	private const string FireSplatScenePath = "res://Prefab/Particles/Splats/FireSplats/FireSplats.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseControlNew currentControl = instance.currentControl;
		AdobeAnimateRenderBackend adobeAnimateRenderBackend = Global.Instance.adobeAnimateRenderBackend;
		TowerDefenseControlNew towerDefenseControlNew = null;
		Node2D node2D = null;
		try
		{
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			node2D = new Node2D
			{
				Name = "BulletHitBurnEffectMount"
			};
			AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			towerDefenseControlNew = (instance.currentControl = new TowerDefenseControlNew
			{
				characterNode = node2D
			});
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/Particles/Splats/FireSplats/FireSplats.tscn", null, ResourceLoader.CacheMode.Reuse);
			Check(GodotObject.IsInstanceValid(packedScene), "The production FireSplats scene must load.");
			AdobeAnimateSprite adobeAnimateSprite = packedScene?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(adobeAnimateSprite), "The production FireSplats scene must instantiate as AdobeAnimateSprite.");
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				throw new InvalidOperationException("FireSplats could not be instantiated.");
			}
			AdobeAnimateData flashAnimeData = adobeAnimateSprite.flashAnimeData;
			Vector2I clip = flashAnimeData.GetClip("Done");
			int num = clip.Y - clip.X;
			Check(num > 1 && flashAnimeData.frameRate > 0.0, $"The production Done clip must have a finite multi-frame duration; range={clip}, frameRate={flashAnimeData.frameRate}.");
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = instance.CreateEffectSpriteSceneOnce(adobeAnimateSprite, new Vector2I(1, 1), "Done");
			node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseEffectSpriteOnceBatcher nodeOrNull = node2D.GetNodeOrNull<TowerDefenseEffectSpriteOnceBatcher>("TowerDefenseEffectSpriteOnceBatcher");
			Check(GodotObject.IsInstanceValid(nodeOrNull), "The production FireSplats effect must use the one-shot GPU batcher.");
			if (!GodotObject.IsInstanceValid(nodeOrNull))
			{
				throw new InvalidOperationException("The one-shot GPU batcher was not created.");
			}
			nodeOrNull.SetProcess(enable: false);
			AnimateMultiMeshRenderer nodeOrNull2 = nodeOrNull.GetNodeOrNull<AnimateMultiMeshRenderer>("EffectSpriteOnceAnimateMultiMeshRenderer");
			Check(GodotObject.IsInstanceValid(nodeOrNull2), "The one-shot GPU batcher must own its production renderer.");
			if (!GodotObject.IsInstanceValid(nodeOrNull2))
			{
				throw new InvalidOperationException("The one-shot GPU renderer was not created.");
			}
			double delta = 1.0 / flashAnimeData.frameRate;
			for (int i = 1; i < num; i++)
			{
				nodeOrNull._Process(delta);
			}
			Check(!towerDefenseEffectSpriteOnce.IsQueuedForDeletion(), "The burn effect must remain alive through the final authored frame.");
			Check(nodeOrNull2.GetVisibleInstanceCountForTest() == 1, "The burn effect must publish exactly one instance before completion.");
			nodeOrNull._Process(delta);
			Check(towerDefenseEffectSpriteOnce.IsQueuedForDeletion(), "The burn effect must queue deletion when its authored duration ends.");
			Check(nodeOrNull2.GetVisibleInstanceCountForTest() == 0, "The completion tick must not publish the terminal burn frame for one extra render frame.");
			int childCount = node2D.GetChildCount();
			AdobeAnimateSprite adobeAnimateSprite2 = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce2 = instance.CreateEffectSpriteSceneOnce(adobeAnimateSprite2, new Vector2I(4, 3), "Done");
			towerDefenseEffectSpriteOnce2.GlobalPosition = new Vector2(480f, 220f);
			node2D.AddChild(towerDefenseEffectSpriteOnce2, forceReadableName: false, InternalMode.Disabled);
			Check(towerDefenseEffectSpriteOnce2.GetParent() == node2D && adobeAnimateSprite2.GetParent() == towerDefenseEffectSpriteOnce2 && node2D.GetChildCount() == childCount + 1, "The BulletField fallback must create a real TowerDefenseEffectSpriteOnce/AdobeAnimateSprite Node pair.");
			Check(!towerDefenseEffectSpriteOnce2.IsQueuedForDeletion(), "The real fallback effect must start its normal one-shot lifecycle.");
			for (int j = 1; j < num; j++)
			{
				nodeOrNull._Process(delta);
			}
			Check(!towerDefenseEffectSpriteOnce2.IsQueuedForDeletion(), "The real fallback effect must remain alive through its final authored frame.");
			Check(nodeOrNull2.GetVisibleInstanceCountForTest() == 1, "The Node-backed fallback may batch rendering while retaining its real Nodes.");
			nodeOrNull._Process(delta);
			Check(towerDefenseEffectSpriteOnce2.IsQueuedForDeletion(), "The real fallback effect must leave through the normal completion callback.");
			Check(nodeOrNull2.GetVisibleInstanceCountForTest() == 0, "The Node-backed fallback completion tick must clear its GPU instance.");
			AdobeAnimateSprite adobeAnimateSprite3 = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			List<string> chainedEvents = new List<string>();
			adobeAnimateSprite3.OnAnimeStarted += delegate(string text)
			{
				chainedEvents.Add("Started:" + text);
			};
			adobeAnimateSprite3.OnAnimeStoped += delegate(string text)
			{
				chainedEvents.Add("Stopped:" + text);
			};
			adobeAnimateSprite3.OnAnimeCompleted += delegate(string text)
			{
				chainedEvents.Add("Completed:" + text);
			};
			adobeAnimateSprite3.OnAnimeBlendCompleted += delegate(string text)
			{
				chainedEvents.Add("BlendCompleted:" + text);
			};
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce3 = instance.CreateEffectSpriteSceneOnce(adobeAnimateSprite3, new Vector2I(1, 1), "Flame|Done");
			node2D.AddChild(towerDefenseEffectSpriteOnce3, forceReadableName: false, InternalMode.Disabled);
			chainedEvents.Clear();
			towerDefenseEffectSpriteOnce3.Refresh();
			nodeOrNull.SetProcess(enable: false);
			Vector2I clip2 = flashAnimeData.GetClip("Flame");
			int num2 = clip2.Y - clip2.X;
			Check(!HasBatchEntry(nodeOrNull, towerDefenseEffectSpriteOnce3) && !GetPrivateBool(towerDefenseEffectSpriteOnce3, "_gpuBatchActive") && adobeAnimateSprite3.Visible && !adobeAnimateSprite3.pause, "A multi-clip Effect must remain a real CPU Adobe Node because its authored 0.1s blend cannot be GPU batched.");
			for (int num3 = 0; num3 < num2; num3++)
			{
				adobeAnimateSprite3.BatchProcessUpdate(delta);
			}
			int num4 = chainedEvents.IndexOf("Stopped:Flame");
			int num5 = chainedEvents.IndexOf("Completed:Flame");
			int num6 = chainedEvents.IndexOf("Started:Done");
			Check(!towerDefenseEffectSpriteOnce3.IsQueuedForDeletion() && towerDefenseEffectSpriteOnce3.currentIndex == 1 && adobeAnimateSprite3.clip == "Done" && adobeAnimateSprite3.blend && !HasBatchEntry(nodeOrNull, towerDefenseEffectSpriteOnce3), "Completing Flame must advance the real Node to Done with the authored blend and without a GPU Entry.");
			Check(num4 >= 0 && num5 > num4 && num6 > num5, "The real multi-clip Node must preserve Stopped, Completed, then next Started event order.");
			adobeAnimateSprite3.BatchProcessUpdate(0.1);
			Check(chainedEvents.Contains("BlendCompleted:Done") && !adobeAnimateSprite3.blend, "The real multi-clip Node must complete its authored cross-clip blend.");
			Vector2I clip3 = flashAnimeData.GetClip("Done");
			int num7 = clip3.Y - clip3.X;
			adobeAnimateSprite3.BatchProcessUpdate(((double)num7 + 1.0) / flashAnimeData.frameRate);
			Check(towerDefenseEffectSpriteOnce3.IsQueuedForDeletion(), "The final Done clip must retire the real chained Effect through its normal completion callback.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BulletHitBurnEffectLifetimeRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = adobeAnimateRenderBackend;
			instance.currentControl = currentControl;
			if (GodotObject.IsInstanceValid(node2D))
			{
				node2D.QueueFree();
			}
			if (GodotObject.IsInstanceValid(towerDefenseControlNew))
			{
				towerDefenseControlNew.Free();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"BULLET_HIT_BURN_EFFECT_LIFETIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BulletHitBurnEffectLifetimeRuntimeTest] " + message);
		}
	}

	private static bool HasBatchEntry(TowerDefenseEffectSpriteOnceBatcher batcher, TowerDefenseEffectSpriteOnce effect)
	{
		if ((batcher?.GetType().GetField("_entryByEffect", BindingFlags.Instance | BindingFlags.NonPublic))?.GetValue(batcher) is IDictionary dictionary)
		{
			return dictionary.Contains(effect);
		}
		return false;
	}

	private static bool GetPrivateBool(object owner, string fieldName)
	{
		object obj = owner?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner);
		bool flag = default(bool);
		int num;
		if (obj is bool)
		{
			flag = (bool)obj;
			num = 1;
		}
		else
		{
			num = 0;
		}
		return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
	}
}
