using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWEditorToaster : VBoxContainer
{
	public enum Severity
	{
		Info,
		Warning,
		Error
	}

	private VBoxContainer _toastVBox;

	public override void _Ready()
	{
		_toastVBox = GetNode<VBoxContainer>("%ToastVBox");
	}

	public void ShowToast(string message, Severity severity = Severity.Info, float duration = 3f)
	{
		if (IsInsideTree() && GodotObject.IsInstanceValid(_toastVBox) && _toastVBox.IsInsideTree())
		{
			PanelContainer panelContainer = BuildToast(message, severity);
			_toastVBox.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
			DismissLater(panelContainer, duration);
		}
	}

	private PanelContainer BuildToast(string message, Severity severity)
	{
		PanelContainer panelContainer = new PanelContainer();
		Label label = new Label
		{
			Text = message
		};
		label.AddThemeFontSizeOverride("font_size", 13);
		Color color = severity switch
		{
			Severity.Warning => new Color(1f, 0.84f, 0f), 
			Severity.Error => new Color(1f, 0.35f, 0.4f), 
			_ => new Color(0.85f, 0.9f, 1f), 
		};
		label.AddThemeColorOverride("font_color", color);
		panelContainer.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private async void DismissLater(Node toast, float delay)
	{
		if (!GodotObject.IsInstanceValid(toast) || !IsInsideTree() || !toast.IsInsideTree())
		{
			return;
		}
		SceneTree tree = GetTree();
		if (GodotObject.IsInstanceValid(tree))
		{
			SceneTreeTimer source = tree.CreateTimer(delay);
			await ToSignal(source, SceneTreeTimer.SignalName.Timeout);
			if (GodotObject.IsInstanceValid(toast) && toast.IsInsideTree())
			{
				toast.QueueFree();
			}
		}
	}
}
