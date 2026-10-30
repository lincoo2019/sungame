using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAspectScaledPreviewHost : Control
{
	private SubViewportContainer _viewportContainer;

	[Export(PropertyHint.None, "")]
	public Vector2 DesignSize { get; set; } = new Vector2(1080f, 600f);

	[Export(PropertyHint.None, "")]
	public bool AllowUpscale { get; set; }

	public float PreviewScale { get; private set; } = 1f;

	public Rect2 PreviewVisualRect { get; private set; }

	public override void _Ready()
	{
		_viewportContainer = GetNodeOrNull<SubViewportContainer>("ViewportContainer");
		base.Resized += ApplyResponsiveLayout;
		CallDeferred("ApplyResponsiveLayout");
	}

	public override void _ExitTree()
	{
		base.Resized -= ApplyResponsiveLayout;
		base._ExitTree();
	}

	public void ApplyResponsiveLayout()
	{
		if (GodotObject.IsInstanceValid(_viewportContainer) && !(base.Size.X <= 0f) && !(base.Size.Y <= 0f) && !(DesignSize.X <= 0f) && !(DesignSize.Y <= 0f))
		{
			float a = Mathf.Min(base.Size.X / DesignSize.X, base.Size.Y / DesignSize.Y);
			if (!AllowUpscale)
			{
				a = Mathf.Min(a, 1f);
			}
			PreviewScale = Mathf.Max(a, 0.05f);
			Vector2 vector = DesignSize * PreviewScale;
			Vector2 position = (base.Size - vector) * 0.5f;
			_viewportContainer.Size = DesignSize;
			_viewportContainer.Scale = Vector2.One * PreviewScale;
			_viewportContainer.Position = position;
			PreviewVisualRect = new Rect2(position, vector);
		}
	}

	public bool PreviewFitsWithinBounds(float tolerance = 1f)
	{
		if (PreviewVisualRect.Position.X >= 0f - tolerance && PreviewVisualRect.Position.Y >= 0f - tolerance && PreviewVisualRect.End.X <= base.Size.X + tolerance)
		{
			return PreviewVisualRect.End.Y <= base.Size.Y + tolerance;
		}
		return false;
	}
}
