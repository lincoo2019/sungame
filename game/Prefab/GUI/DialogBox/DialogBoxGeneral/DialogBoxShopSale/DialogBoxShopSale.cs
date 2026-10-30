using Godot;

public partial class DialogBoxShopSale : DialogPopup
{
	public delegate void SaleEventHandler(ShopItem item);

	public ShopItem item;

	public event SaleEventHandler OnSale;

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%TrueButton").Pressed += TrueButtonPressed;
		GetNode<BaseButton>("%FalseButton").Pressed += FalseButtonPressed;
	}

	public void TrueButtonPressed()
	{
		this.OnSale?.Invoke(item);
		CloseDialog();
	}

	public void FalseButtonPressed()
	{
		CloseDialog();
	}
}
