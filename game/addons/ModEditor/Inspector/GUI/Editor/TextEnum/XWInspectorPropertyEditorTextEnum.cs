using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorTextEnum : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/TextEnum/XWInspectorPropertyEditorTextEnum.tscn";

	private OptionButton _optionButton;

	private LineEdit _lineEdit;

	private string[] _options = Array.Empty<string>();

	private bool _isCustom;

	public static XWInspectorPropertyEditorTextEnum Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/TextEnum/XWInspectorPropertyEditorTextEnum.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorTextEnum>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_optionButton = GetNode<OptionButton>("%OptionButton");
		_lineEdit = GetNode<LineEdit>("%LineEdit");
		_optionButton.ItemSelected += OnOptionSelected;
		_lineEdit.TextChanged += OnCustomTextChanged;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		ParseHintString(property.HintString);
		base.SetEditProperty(property, field);
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			string text = propertyValue.ToString();
			int num = Array.IndexOf(_options, text);
			if (num >= 0)
			{
				_optionButton.Select(num);
				ShowCustomEdit(show: false);
			}
			else
			{
				_optionButton.Select(_options.Length);
				XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_lineEdit, text);
				ShowCustomEdit(show: true);
			}
		}
	}

	public override Variant GetValue()
	{
		if (_isCustom)
		{
			return _lineEdit.Text;
		}
		int selected = _optionButton.Selected;
		if (selected >= 0 && selected < _options.Length)
		{
			return _options[selected];
		}
		return "";
	}

	private void ParseHintString(string hintString)
	{
		_optionButton.Clear();
		_options = hintString.Split(",");
		string[] options = _options;
		foreach (string label in options)
		{
			_optionButton.AddItem(label);
		}
		_optionButton.AddItem("自定义...");
		ShowCustomEdit(show: false);
	}

	private void ShowCustomEdit(bool show)
	{
		_isCustom = show;
		_lineEdit.Visible = show;
		if (show)
		{
			_optionButton.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			_lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		}
		else
		{
			_optionButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_lineEdit.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		}
	}

	private void OnOptionSelected(long index)
	{
		if (index == _options.Length)
		{
			ShowCustomEdit(show: true);
			_lineEdit.GrabFocus();
		}
		else
		{
			ShowCustomEdit(show: false);
			ValueChange(_options[index]);
		}
	}

	private void OnCustomTextChanged(string newText)
	{
		ValueChange(newText);
	}
}
