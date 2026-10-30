using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public sealed class XWGameplayLogicPresentationContext
{
	public Resource Resource { get; init; }

	public XWResourceEditContext EditContext { get; init; }

	public XWVisualPropertyBinding PropertyBinding { get; init; }

	public XWEmbeddedResourceEditService EmbeddedResourceEditService { get; init; }

	public XWGameplayResourcePickerWindow ResourcePicker { get; init; }

	public XWGameplayLogicPreviewSafety PreviewSafety { get; init; }

	public SubViewport StageViewport { get; init; }

	public Node2D StageRoot { get; init; }

	public Control OverlayRoot { get; init; }

	public Control HudRoot { get; init; }

	public Control TimelineRoot { get; init; }

	public Control ShelfRoot { get; init; }

	public Control AdvancedInspectorHost { get; init; }

	public Control ErrorBar { get; init; }
}
