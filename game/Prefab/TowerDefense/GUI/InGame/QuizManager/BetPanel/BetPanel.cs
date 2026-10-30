using Godot;

[GlobalClass]
public partial class BetPanel : Control
{
	public SpinBox betCoinSpinBox;

	public Control checkBoxNode;

	public CheckBox winCheckBox;

	public CheckBox failCheckBox;

	public CheckBox skipCheckBox;

	public Label betCoinLabel;

	public Label lineLabel;

	public Label chooseLabel;

	public int line = 1;

	public override void _Ready()
	{
		betCoinSpinBox = GetNodeOrNull<SpinBox>("%BetCoinSpinBox");
		checkBoxNode = GetNodeOrNull<Control>("%CheckBoxNode");
		winCheckBox = GetNodeOrNull<CheckBox>("%WinCheckBox");
		failCheckBox = GetNodeOrNull<CheckBox>("%FailCheckBox");
		skipCheckBox = GetNodeOrNull<CheckBox>("%SkipCheckBox");
		betCoinLabel = GetNodeOrNull<Label>("%BetCoinLabel");
		lineLabel = GetNodeOrNull<Label>("%LineLabel");
		chooseLabel = GetNodeOrNull<Label>("%ChooseLabel");
		winCheckBox.Toggled += WinCheckBoxToggled;
		failCheckBox.Toggled += FailCheckBoxToggled;
		skipCheckBox.Toggled += SkipCheckBoxToggled;
	}

	public void Init(int _line = 1)
	{
		line = _line;
		lineLabel.Text = $"{line}行 ：";
	}

	public void Finish()
	{
		betCoinSpinBox.Visible = false;
		checkBoxNode.Visible = false;
		betCoinLabel.Visible = true;
		betCoinLabel.Text = $"{(int)betCoinSpinBox.Value} 金币";
		chooseLabel.Visible = true;
		if (winCheckBox.ButtonPressed)
		{
			chooseLabel.Text = "赢";
		}
		if (failCheckBox.ButtonPressed)
		{
			chooseLabel.Text = "输";
		}
		if (skipCheckBox.ButtonPressed)
		{
			chooseLabel.Text = "跳";
		}
	}

	public void WinCheckBoxToggled(bool toggledOn)
	{
		if (!winCheckBox.ButtonPressed)
		{
			winCheckBox.SetPressedNoSignal(pressed: true);
			toggledOn = true;
		}
		if (toggledOn)
		{
			failCheckBox.SetPressedNoSignal(pressed: false);
			skipCheckBox.SetPressedNoSignal(pressed: false);
		}
	}

	public void FailCheckBoxToggled(bool toggledOn)
	{
		if (!failCheckBox.ButtonPressed)
		{
			failCheckBox.SetPressedNoSignal(pressed: true);
			toggledOn = true;
		}
		if (toggledOn)
		{
			winCheckBox.SetPressedNoSignal(pressed: false);
			skipCheckBox.SetPressedNoSignal(pressed: false);
		}
	}

	public void SkipCheckBoxToggled(bool toggledOn)
	{
		if (!skipCheckBox.ButtonPressed)
		{
			skipCheckBox.SetPressedNoSignal(pressed: true);
			toggledOn = true;
		}
		if (toggledOn)
		{
			failCheckBox.SetPressedNoSignal(pressed: false);
			winCheckBox.SetPressedNoSignal(pressed: false);
		}
	}
}
