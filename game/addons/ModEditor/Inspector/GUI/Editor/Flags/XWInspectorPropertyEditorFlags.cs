using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorFlags : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Flags/XWInspectorPropertyEditorFlags.tscn";

	private VBoxContainer _checkboxContainer;

	private readonly List<CheckBox> _checkboxes = new List<CheckBox>();

	private string[] _flagNames = Array.Empty<string>();

	public static XWInspectorPropertyEditorFlags Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Flags/XWInspectorPropertyEditorFlags.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorFlags>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_checkboxContainer = GetNode<VBoxContainer>("%CheckboxContainer");
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		ParseFlagsHint(property.HintString);
		base.SetEditProperty(property, field);
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Nil)
		{
			return;
		}
		long num = propertyValue.AsInt64();
		for (int i = 0; i < _checkboxes.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_checkboxes[i]))
			{
				_checkboxes[i].SetPressedNoSignal((num & (1L << i)) != 0);
			}
		}
	}

	public override Variant GetValue()
	{
		long num = 0L;
		for (int i = 0; i < _checkboxes.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_checkboxes[i]) && _checkboxes[i].ButtonPressed)
			{
				num |= 1L << i;
			}
		}
		return num;
	}

	private void ParseFlagsHint(string hintString)
	{
		foreach (CheckBox checkbox in _checkboxes)
		{
			if (GodotObject.IsInstanceValid(checkbox))
			{
				checkbox.QueueFree();
			}
		}
		_checkboxes.Clear();
		_flagNames = (string.IsNullOrEmpty(hintString) ? Array.Empty<string>() : hintString.Split(","));
		string[] flagNames = _flagNames;
		foreach (string text in flagNames)
		{
			CheckBox checkBox = new CheckBox
			{
				Text = text,
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			checkBox.Toggled += OnFlagToggled;
			_checkboxContainer.AddChild(checkBox, forceReadableName: false, InternalMode.Disabled);
			_checkboxes.Add(checkBox);
		}
	}

	private void OnFlagToggled(bool pressed)
	{
		ValueChange(GetValue());
	}
}
