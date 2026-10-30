using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PropertiesFlag : PropertiesBase
{
	public delegate void ValueChangedEventHandler(int value);

	private VBoxContainer _checkBoxContainer;

	public Array<CheckBox> checkBoxList = new Array<CheckBox>();

	public Dictionary hintDictionary = new Dictionary();

	public int value;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_checkBoxContainer = GetNode<VBoxContainer>("%CheckBoxContainer");
	}

	public void SetHintDictionary(Dictionary newHintDictionary)
	{
		if (!IsNodeReady())
		{
			return;
		}
		hintDictionary = newHintDictionary;
		foreach (Node child in _checkBoxContainer.GetChildren())
		{
			child.QueueFree();
		}
		checkBoxList.Clear();
		foreach (Variant key in hintDictionary.Keys)
		{
			string text = key.AsString();
			CheckBox checkBox = new CheckBox();
			checkBox.Text = text;
			checkBox.Toggled += ValueChange;
			_checkBoxContainer.AddChild(checkBox, forceReadableName: false, InternalMode.Disabled);
			checkBoxList.Add(checkBox);
		}
	}

	public void SetValue(int newValue)
	{
		if (!IsNodeReady())
		{
			return;
		}
		value = newValue;
		if (CanSetValue())
		{
			for (int i = 0; i < checkBoxList.Count; i++)
			{
				CheckBox checkBox = checkBoxList[i];
				checkBox.Toggled -= ValueChange;
				checkBox.ButtonPressed = (value & (1 << i)) != 0;
				checkBox.Toggled += ValueChange;
			}
		}
	}

	private void ValueChange(bool toggledOn)
	{
		int num = 0;
		for (int i = 0; i < checkBoxList.Count; i++)
		{
			if (checkBoxList[i].ButtonPressed)
			{
				num += 1 << i;
			}
		}
		value = num;
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return true;
	}

	public override string _GetType()
	{
		return "Flag";
	}
}
