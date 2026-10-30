using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWProgressDialog : AcceptDialog
{
	[Signal]
	public delegate void ProgressCanceledEventHandler();

	private Label _statusLabel;

	private ProgressBar _progressBar;

	private Button _cancelButton;

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("%StatusLabel");
		_progressBar = GetNode<ProgressBar>("%ProgressBar");
		_cancelButton = GetNode<Button>("%CancelButton");
		base.CloseRequested += Cancel;
		_cancelButton.Pressed += Cancel;
	}

	public void SetStatus(string text)
	{
		_statusLabel.Text = text;
	}

	public void SetProgress(float ratio)
	{
		_progressBar.Value = ratio * 100f;
	}

	public void SetProgressValue(double value)
	{
		_progressBar.Value = value;
	}

	public void SetMaxValue(double max)
	{
		_progressBar.MaxValue = max;
	}

	private void Cancel()
	{
		EmitSignal(SignalName.ProgressCanceled);
		Hide();
	}
}
