using Godot;

[GlobalClass]
public partial class QuizControl : Control
{
	public delegate void RunButtonPressedEventHandler();

	public Control startGUINode;

	public Control changePresentBoxButtonNode;

	public Panel betPanelBack;

	public Control betPanelNode;

	public Label coinLabel;

	private TextureButton _runButton;

	public event RunButtonPressedEventHandler OnRunButtonPressed;

	public override void _Ready()
	{
		startGUINode = GetNodeOrNull<Control>("%StartGUINode");
		changePresentBoxButtonNode = GetNodeOrNull<Control>("%ChangePresentBoxButtonNode");
		betPanelBack = GetNodeOrNull<Panel>("%BetPanelBack");
		betPanelNode = GetNodeOrNull<Control>("%BetPanelNode");
		coinLabel = GetNodeOrNull<Label>("%CoinLabel");
		_runButton = GetNode<TextureButton>("%StartGUINode/RunButton");
		_runButton.Pressed += RunButtonPressed;
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_runButton))
		{
			_runButton.Pressed -= RunButtonPressed;
		}
		this.OnRunButtonPressed = null;
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.coinBank))
		{
			TowerDefenseManager.Instance.coinBank.StartHide();
		}
		if (GodotObject.IsInstanceValid(BroadCastManager.Instance))
		{
			BroadCastManager.Instance.BraodCastClear();
		}
		_runButton = null;
		base._ExitTree();
	}

	public void RunButtonPressed()
	{
		this.OnRunButtonPressed?.Invoke();
	}
}
