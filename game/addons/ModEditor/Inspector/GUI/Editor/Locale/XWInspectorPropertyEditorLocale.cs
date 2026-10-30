using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorLocale : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Locale/XWInspectorPropertyEditorLocale.tscn";

	private OptionButton _optionButton;

	private string[] _locales = new string[34]
	{
		"zh", "zh_CN", "zh_TW", "zh_HK", "en", "en_US", "en_GB", "ja", "ko", "de",
		"fr", "es", "pt", "ru", "it", "ar", "hi", "th", "vi", "id",
		"nl", "pl", "sv", "da", "no", "fi", "cs", "hu", "ro", "bg",
		"uk", "tr", "el", "he"
	};

	public static XWInspectorPropertyEditorLocale Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Locale/XWInspectorPropertyEditorLocale.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorLocale>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_optionButton = GetNode<OptionButton>("%OptionButton");
		string[] locales = _locales;
		foreach (string label in locales)
		{
			_optionButton.AddItem(label);
		}
		_optionButton.ItemSelected += OnLocaleSelected;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			string text = propertyValue.AsString();
			int num = Array.IndexOf(_locales, text);
			if (num >= 0)
			{
				_optionButton.Select(num);
				return;
			}
			_optionButton.AddItem(text);
			string[] array = new string[_locales.Length + 1];
			_locales.CopyTo(array, 0);
			array[_locales.Length] = text;
			_locales = array;
			_optionButton.Select(_optionButton.ItemCount - 1);
		}
	}

	public override Variant GetValue()
	{
		int selected = _optionButton.Selected;
		if (selected >= 0 && selected < _locales.Length)
		{
			return _locales[selected];
		}
		return "";
	}

	private void OnLocaleSelected(long index)
	{
		int num = (int)index;
		if (num >= 0 && num < _locales.Length)
		{
			ValueChange(_locales[num]);
		}
	}
}
