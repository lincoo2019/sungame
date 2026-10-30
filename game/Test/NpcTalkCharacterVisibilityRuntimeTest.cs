using System;
using System.Threading.Tasks;
using Godot;

public partial class NpcTalkCharacterVisibilityRuntimeTest : Node
{
	private const string CrazyDaveScenePath = "res://Prefab/Npc/CrazyDave/NpcCrazyDave.tscn";

	private const string NpcTalkControlScenePath = "res://Registry/Battle/Feature/NpcTalk/Control/NpcTalkControl.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		CanvasLayer backgroundLayer = null;
		CanvasLayer npcLayer = null;
		try
		{
			_ = 2;
			try
			{
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
				AdobeAnimateDefinitionCache.Clear();
				backgroundLayer = new CanvasLayer
				{
					Name = "BackgroundLayer",
					Layer = -10
				};
				AddChild(backgroundLayer, forceReadableName: false, InternalMode.Disabled);
				backgroundLayer.AddChild(new ColorRect
				{
					Name = "Background",
					Position = Vector2.Zero,
					Size = GetViewport().GetVisibleRect().Size,
					Color = new Color(0.08f, 0.12f, 0.16f),
					MouseFilter = Control.MouseFilterEnum.Ignore
				}, forceReadableName: false, InternalMode.Disabled);
				npcLayer = new CanvasLayer
				{
					Name = "NpcTalkLayer",
					Layer = 4
				};
				AddChild(npcLayer, forceReadableName: false, InternalMode.Disabled);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/NpcTalk/Control/NpcTalkControl.tscn", null, ResourceLoader.CacheMode.Reuse);
				Check(GodotObject.IsInstanceValid(packedScene), "The production NPC talk control scene must load.");
				NpcTalkControl control = packedScene?.Instantiate<NpcTalkControl>(PackedScene.GenEditState.Disabled);
				if (!GodotObject.IsInstanceValid(control))
				{
					throw new InvalidOperationException("NPC talk control fixture could not be created.");
				}
				npcLayer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Prefab/Npc/CrazyDave/NpcCrazyDave.tscn", null, ResourceLoader.CacheMode.Reuse);
				Check(GodotObject.IsInstanceValid(packedScene2), "The production Crazy Dave scene must load.");
				NpcCrazyDave npc = packedScene2?.Instantiate<NpcCrazyDave>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(npc), "The production Crazy Dave scene must instantiate.");
				if (!GodotObject.IsInstanceValid(npc))
				{
					throw new InvalidOperationException("Crazy Dave fixture could not be created.");
				}
				npc.sprite.flashAnimeData = npc.sprite.flashAnimeData?.Duplicate(deep: true) as AdobeAnimateData;
				control.AddNpc(npc);
				await WaitFrames(2);
				TextureRect bubble = npc.GetNodeOrNull<TextureRect>("%TalkBubble");
				Control nodeOrNull = control.GetNodeOrNull<Control>("%NpcRenderMount");
				Check(GodotObject.IsInstanceValid(npc.sprite) && GodotObject.IsInstanceValid(bubble), "The NPC must keep both its Adobe Animate body and ordinary talk bubble.");
				if (!GodotObject.IsInstanceValid(npc.sprite) || !GodotObject.IsInstanceValid(bubble))
				{
					throw new InvalidOperationException("Crazy Dave visual nodes are unavailable.");
				}
				Check(npc.sprite.forceLocalRender && GodotObject.IsInstanceValid(nodeOrNull), "NpcTalkControl.AddNpc must route the body through the NPC-local render mount.");
				npc.sprite.SetAnimation("Idle");
				npc.sprite.pause = true;
				npc.sprite.Visible = true;
				bubble.Visible = true;
				npc.GetNode<Label>("%TalkLabel").Text = "NPC 对话人物可见性回归";
				await WaitFrames(12);
				Check(npc.IsVisibleInTree() && npc.sprite.IsVisibleInTree() && bubble.IsVisibleInTree(), "The production NPC, body, and talk bubble must all be logically visible.");
				int publishedInstances = CountPublishedInstances(GetTree().Root);
				Check(publishedInstances > 0, $"The NPC fallback render mount must publish body instances; count={publishedInstances}.");
				using Image visibleImage = GetViewport().GetTexture().GetImage();
				npc.sprite.Visible = false;
				await WaitFrames(6);
				using Image hidden = GetViewport().GetTexture().GetImage();
				int num = CountChangedPixels(visibleImage, hidden);
				Check(num >= 256, $"Hiding only the NPC body must remove visible character pixels while the bubble stays mounted; changed={num}.");
				GD.Print($"NPC_TALK_CHARACTER_VISIBILITY_METRIC changed={num} instances={publishedInstances} bubbleVisible={bubble.IsVisibleInTree()}");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[NpcTalkCharacterVisibilityRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(npcLayer))
			{
				npcLayer.QueueFree();
			}
			if (GodotObject.IsInstanceValid(backgroundLayer))
			{
				backgroundLayer.QueueFree();
			}
			await WaitFrames(6);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
		}
		bool flag = _failures == 0 && _checks == 8;
		GD.Print($"NPC_TALK_CHARACTER_VISIBILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static int CountPublishedInstances(Node node)
	{
		int num = ((node is AdobeAnimateMultiMeshBatcher adobeAnimateMultiMeshBatcher) ? adobeAnimateMultiMeshBatcher.GetVisibleInstanceCountForTest() : 0);
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			num += CountPublishedInstances(child);
		}
		return num;
	}

	private static int CountChangedPixels(Image visible, Image hidden)
	{
		int num = Math.Min(visible.GetWidth(), hidden.GetWidth());
		int num2 = Math.Min(visible.GetHeight(), hidden.GetHeight());
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (ColorDistance(visible.GetPixel(j, i), hidden.GetPixel(j, i)) > 0.08f)
				{
					num3++;
				}
			}
		}
		return num3;
	}

	private static float ColorDistance(Color left, Color right)
	{
		return Mathf.Max(Mathf.Max(Mathf.Abs(left.R - right.R), Mathf.Abs(left.G - right.G)), Mathf.Max(Mathf.Abs(left.B - right.B), Mathf.Abs(left.A - right.A)));
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
			GD.PushError("[NpcTalkCharacterVisibilityRuntimeTest] " + message);
		}
	}
}
