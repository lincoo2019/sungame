using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PropertiesArrayVector2 : PropertiesBase
{
	public delegate void ValueChangedEventHandler(Array<Vector2> value);

	private HBoxContainer _prefab;

	private VBoxContainer _root;

	private Button _addButton;

	public Array<Vector2> value = new Array<Vector2>();

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_prefab = GetNode<HBoxContainer>("%Prefab");
		_root = GetNode<VBoxContainer>("%Root");
		_addButton = GetNode<Button>("%AddButton");
		UpdateUI();
		_addButton.Pressed += OnAddButtonPressed;
	}

	public void SetValue(Array<Vector2> newValue)
	{
		value = newValue;
		if (IsNodeReady())
		{
			UpdateUI();
		}
	}

	private void UpdateUI()
	{
		foreach (Node child2 in _root.GetChildren())
		{
			child2.QueueFree();
		}
		for (int i = 0; i < value.Count; i++)
		{
			HBoxContainer hBoxContainer = _prefab.Duplicate() as HBoxContainer;
			hBoxContainer.GetNode<Label>("IndexLabel").Text = i.ToString();
			Node child = hBoxContainer.GetChild(1);
			SpinBox node = child.GetNode<SpinBox>("SpinBoxX");
			SpinBox node2 = child.GetNode<SpinBox>("SpinBoxY");
			node.Value = value[i].X;
			node2.Value = value[i].Y;
			int idx = i;
			node.ValueChanged += delegate(double v)
			{
				OnValueChangeX((float)v, idx);
			};
			node2.ValueChanged += delegate(double v)
			{
				OnValueChangeY((float)v, idx);
			};
			hBoxContainer.GetNode<Button>("RemoveButton").Pressed += delegate
			{
				OnRemoveButtonPressed(idx);
			};
			hBoxContainer.Visible = true;
			_root.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void OnAddButtonPressed()
	{
		value.Add(Vector2.Zero);
		UpdateUI();
		EmitValueChanged();
	}

	private void OnRemoveButtonPressed(int index)
	{
		GD.Print("remove_button_pressed", index);
		if (index < value.Count)
		{
			value.RemoveAt(index);
			UpdateUI();
			EmitValueChanged();
		}
	}

	private void OnValueChangeX(float v, int index)
	{
		if (index < value.Count)
		{
			value[index] = new Vector2(v, value[index].Y);
			EmitValueChanged();
		}
	}

	private void OnValueChangeY(float v, int index)
	{
		if (index < value.Count)
		{
			value[index] = new Vector2(value[index].X, v);
			EmitValueChanged();
		}
	}

	private void EmitValueChanged()
	{
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		foreach (Node child2 in _root.GetChildren())
		{
			Node child = child2.GetChild(1);
			SpinBox node = child.GetNode<SpinBox>("SpinBoxX");
			SpinBox node2 = child.GetNode<SpinBox>("SpinBoxY");
			Button node3 = child2.GetNode<Button>("RemoveButton");
			if (node.GetLineEdit().HasFocus() || node2.GetLineEdit().HasFocus() || node3.HasFocus())
			{
				return false;
			}
		}
		return true;
	}

	public override string _GetType()
	{
		return "ArrayVector2";
	}
}
