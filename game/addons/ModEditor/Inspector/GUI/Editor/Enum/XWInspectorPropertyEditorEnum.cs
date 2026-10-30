using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorEnum : XWInspectorPropertyEditorBase
{
	private OptionButton _optionButton;

	private readonly List<int> _enumValues = new List<int>();

	public override void _Ready()
	{
		base._Ready();
		_optionButton = GetNode<OptionButton>("%OptionButton");
		_optionButton.ItemSelected += delegate(long index)
		{
			if (index >= 0 && index < _enumValues.Count)
			{
				ValueChange(_enumValues[(int)index]);
			}
		};
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		ParseEnumHint(property.HintString);
		base.SetEditProperty(property, field);
	}

	public override void UpdateValue()
	{
		if (_optionButton.ItemCount <= 0)
		{
			return;
		}
		int num = GetPropertyValue().As<int>();
		for (int i = 0; i < _optionButton.ItemCount && i < _enumValues.Count; i++)
		{
			if (_enumValues[i] == num)
			{
				_optionButton.Select(i);
				break;
			}
		}
	}

	public override Variant GetValue()
	{
		int selected = _optionButton.Selected;
		return (selected >= 0 && selected < _enumValues.Count) ? _enumValues[selected] : 0;
	}

	private void ParseEnumHint(string hintString)
	{
		_optionButton.Clear();
		_enumValues.Clear();
		string[] array = hintString.Split(",");
		int num = 0;
		string[] array2 = array;
		foreach (string text in array2)
		{
			string label = text.Trim();
			int num2 = num;
			int num3 = text.LastIndexOf(':');
			if (num3 > 0)
			{
				string text2 = text;
				int num4 = num3 + 1;
				if (int.TryParse(text2.Substring(num4, text2.Length - num4).Trim(), out var result))
				{
					label = text.Substring(0, num3).Trim();
					num2 = result;
				}
			}
			_optionButton.AddItem(label);
			_enumValues.Add(num2);
			num = num2 + 1;
		}
	}
}
