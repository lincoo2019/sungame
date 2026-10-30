using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PropertiesEnum : PropertiesBase
{
	public delegate void ValueChangedEventHandler(Variant value);

	private OptionButton _optionButton;

	public Dictionary itemDictionary = new Dictionary();

	public Dictionary hintDictionary = new Dictionary();

	public Variant value = false;

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		_optionButton = GetNode<OptionButton>("%OptionButton");
		LevelEditorDropdown.Configure(_optionButton);
		_optionButton.ItemSelected += ValueChange;
		if (hintDictionary.Count > 0)
		{
			RebuildItems();
		}
	}

	public void SetHintDictionary(Dictionary newHintDictionary)
	{
		hintDictionary = newHintDictionary;
		if (IsNodeReady())
		{
			RebuildItems();
		}
	}

	private void RebuildItems()
	{
		itemDictionary.Clear();
		_optionButton.Clear();
		foreach (Variant key in hintDictionary.Keys)
		{
			string text = key.AsString();
			itemDictionary[hintDictionary[key]] = text;
			_optionButton.AddItem(text);
		}
	}

	public void SetValue(Variant newValue)
	{
		if (IsNodeReady())
		{
			value = newValue;
			if (itemDictionary.ContainsKey(newValue))
			{
				_optionButton.ItemSelected -= ValueChange;
				_optionButton.Selected = FindOptionButtonId(_optionButton, itemDictionary[newValue].AsString());
				_optionButton.ItemSelected += ValueChange;
			}
		}
	}

	private void ValueChange(long index)
	{
		int idx = (int)index;
		value = hintDictionary[_optionButton.GetItemText(idx)];
		this.OnValueChanged?.Invoke(value);
	}

	public override bool CanSetValue()
	{
		return !_optionButton.HasFocus();
	}

	public override string _GetType()
	{
		return "Enum";
	}

	public int FindOptionButtonId(OptionButton optionButton, string key)
	{
		for (int i = 0; i < optionButton.ItemCount; i++)
		{
			if (optionButton.GetItemText(i) == key)
			{
				return optionButton.GetItemId(i);
			}
		}
		return -1;
	}
}
