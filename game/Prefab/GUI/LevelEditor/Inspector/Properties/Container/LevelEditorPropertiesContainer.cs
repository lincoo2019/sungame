using Godot;

[GlobalClass]
public partial class LevelEditorPropertiesContainer : VBoxContainer
{
	internal HBoxContainer innerContainer;

	internal VBoxContainer outerContainer;

	internal Label keyLabel;

	internal Button refreshButton;

	public PropertiesBase propertyEditor;

	private Variant _restValue;

	public Variant restValue
	{
		get
		{
			return _restValue;
		}
		set
		{
			_restValue = value;
			if (refreshButton != null)
			{
				refreshButton.Visible = _restValue.VariantType != Variant.Type.Nil;
			}
		}
	}

	public override void _Ready()
	{
		innerContainer = GetNode<HBoxContainer>("%InnerContainer");
		outerContainer = GetNode<VBoxContainer>("%OuterContainer");
		keyLabel = GetNode<Label>("%KeyLabel");
		refreshButton = GetNode<Button>("%RefreshButton");
	}

	public override void _Process(double delta)
	{
		if (restValue.VariantType != Variant.Type.Nil && propertyEditor != null)
		{
			refreshButton.Visible = !propertyEditor.GetValue().Equals(restValue);
		}
	}
}
