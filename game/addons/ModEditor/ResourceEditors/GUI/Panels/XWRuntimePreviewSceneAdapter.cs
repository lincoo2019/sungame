using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal static class XWRuntimePreviewSceneAdapter
{
	public static void PrepareAnimationSprites(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.preview = true;
			adobeAnimateSprite.forceLocalRender = true;
			adobeAnimateSprite.keepRenderSubmittedWhenPaused = true;
			adobeAnimateSprite.refreshEveryFrame = true;
		}
		foreach (Node child in node.GetChildren())
		{
			PrepareAnimationSprites(child);
		}
	}
}
