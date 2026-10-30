using System;
using System.Globalization;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorFloat : XWInspectorPropertyEditorBase
{
	private SpinBox _spinBox;

	private string _pendingRangeHint = "";

	public override void _Ready()
	{
		base._Ready();
		_spinBox = GetNode<SpinBox>("%SpinBox");
		_spinBox.ValueChanged += delegate(double v)
		{
			ValueChange(v);
		};
		_spinBox.GetLineEdit().CaretBlink = true;
		if (_pendingRangeHint != "")
		{
			ApplyRangeHint(_pendingRangeHint);
			_pendingRangeHint = "";
		}
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			_spinBox.Value = propertyValue.As<double>();
		}
	}

	public override Variant GetValue()
	{
		return _spinBox.Value;
	}

	public override void SetupRangeHint(string hintString)
	{
		if (IsNodeReady())
		{
			ApplyRangeHint(hintString);
		}
		else
		{
			_pendingRangeHint = hintString;
		}
	}

	private void ApplyRangeHint(string hintString)
	{
		string[] array = hintString.Split(",");
		int num = 0;
		if (array.Length >= 2 && TryParseNumber(array[0], out var result) && TryParseNumber(array[1], out var result2))
		{
			_spinBox.MinValue = result;
			_spinBox.MaxValue = result2;
			_spinBox.AllowGreater = false;
			_spinBox.AllowLesser = false;
			num = 2;
			if (array.Length >= 3 && TryParseNumber(array[2], out var result3))
			{
				_spinBox.Step = result3;
				num = 3;
			}
		}
		for (int i = num; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text == "or_greater")
			{
				_spinBox.AllowGreater = true;
			}
			else if (text == "or_less")
			{
				_spinBox.AllowLesser = true;
			}
			else if (text.StartsWith("suffix:", StringComparison.Ordinal))
			{
				SpinBox spinBox = _spinBox;
				string text2 = text;
				int length = "suffix:".Length;
				spinBox.Suffix = text2.Substring(length, text2.Length - length);
			}
		}
	}

	private static bool TryParseNumber(string value, out double result)
	{
		return double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}
}
