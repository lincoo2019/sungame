using System;
using Godot;

public partial class AdobeAnimateDetachedChildOwnershipProbe : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			RunDetachedChildCheck();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[AdobeAnimateDetachedChildOwnershipProbe] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"ADOBE_ANIMATE_DETACHED_CHILD_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void RunDetachedChildCheck()
	{
		AdobeAnimateSprite adobeAnimateSprite = new AdobeAnimateSprite
		{
			Name = "OldParent"
		};
		AdobeAnimateSprite adobeAnimateSprite2 = new AdobeAnimateSprite
		{
			Name = "DetachedHead"
		};
		Node2D node2D = new Node2D
		{
			Name = "DamagePartDrop"
		};
		adobeAnimateSprite.layerVisible.Add(item: true);
		adobeAnimateSprite.AddChild(adobeAnimateSprite2, forceReadableName: false, InternalMode.Disabled);
		AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
		AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
		adobeAnimateSprite.InsertSpriteAtLayer(adobeAnimateSprite2, 0);
		Check(Contains(adobeAnimateSprite.GetSpriteChildrenForRender(), adobeAnimateSprite2), "The original parent must initially cache its nested animation.");
		Check(adobeAnimateSprite2.IsRenderedByParentSpriteForRender(), "The nested animation must initially render through its original parent.");
		adobeAnimateSprite.RemoveChild(adobeAnimateSprite2);
		node2D.AddChild(adobeAnimateSprite2, forceReadableName: false, InternalMode.Disabled);
		Check(!Contains(adobeAnimateSprite.GetSpriteChildrenForRender(), adobeAnimateSprite2), "Detaching a damage part must evict it from the original parent child cache.");
		Check(!adobeAnimateSprite2.IsRenderedByParentSpriteForRender(), "A detached damage part must render independently instead of through its original parent.");
		adobeAnimateSprite2.QueueFree();
		adobeAnimateSprite.QueueFree();
		node2D.QueueFree();
	}

	private static bool Contains(AdobeAnimateSprite[] sprites, AdobeAnimateSprite expected)
	{
		for (int i = 0; i < sprites.Length; i++)
		{
			if (sprites[i] == expected)
			{
				return true;
			}
		}
		return false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[AdobeAnimateDetachedChildOwnershipProbe] " + message);
		}
	}
}
