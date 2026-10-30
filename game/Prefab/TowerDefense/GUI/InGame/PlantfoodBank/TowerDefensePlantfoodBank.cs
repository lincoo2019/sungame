using Godot;

[GlobalClass]
public partial class TowerDefensePlantfoodBank : Control
{
	public delegate void PickEventHandler();

	private HBoxContainer _plantfoodSlotContainer;

	private TextureButton _plantfoodButton;

	public int beginSizeX = 314;

	public int sizeInterval = 50;

	public event PickEventHandler OnPick;

	public override void _Ready()
	{
		_plantfoodSlotContainer = GetNodeOrNull<HBoxContainer>("%PlantfoodSlotContainer");
		_plantfoodButton = GetNodeOrNull<TextureButton>("%PlantfoodButton");
		if (GodotObject.IsInstanceValid(_plantfoodButton))
		{
			_plantfoodButton.Pressed += PlantfoodButtonPressed;
		}
	}

	public void PlantfoodButtonPressed()
	{
		this.OnPick?.Invoke();
	}
}
