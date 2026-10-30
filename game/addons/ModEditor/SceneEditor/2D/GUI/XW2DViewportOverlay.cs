using Godot;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DViewportOverlay : Control
{
	public XW2DViewport Viewport { get; set; }

	public override void _Draw()
	{
		Viewport?.DrawOverlay(this);
	}
}
