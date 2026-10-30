using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedFloat64Array : XWInspectorPropertyEditorPackedArrayBase
{
	private double[] _array = Array.Empty<double>();

	protected override string ArrayTypeName => "PackedFloat64Array";

	protected override Variant.Type ElementType => Variant.Type.Float;

	protected override Variant GetDefaultElement()
	{
		return 0.0;
	}

	protected override int GetArraySize()
	{
		return _array.Length;
	}

	protected override Variant GetElement(int index)
	{
		return _array[index];
	}

	protected override void SetElement(int index, Variant value)
	{
		_array[index] = value.AsDouble();
	}

	protected override void AppendElement(Variant value)
	{
		double[] array = new double[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = value.AsDouble();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		double[] array = new double[_array.Length - 1];
		Array.Copy(_array, 0, array, 0, index);
		Array.Copy(_array, index + 1, array, index, _array.Length - index - 1);
		_array = array;
	}

	protected override Variant GetArrayAsVariant()
	{
		return _array;
	}

	protected override void LoadArrayFromVariant(Variant value)
	{
		if (value.VariantType == Variant.Type.PackedFloat64Array)
		{
			_array = value.AsFloat64Array();
		}
		else
		{
			_array = Array.Empty<double>();
		}
	}
}
