using Godot;

public partial class DialogBoxShopCantSale : DialogPopup
{
	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
	}

	public void TrueButtonPressed()
	{
		CloseDialog();
	}
}
