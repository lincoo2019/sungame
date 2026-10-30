using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedFloat32Array : XWInspectorPropertyEditorPackedArrayBase
{
	private float[] _array = Array.Empty<float>();

	protected override string ArrayTypeName => "PackedFloat32Array";

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
		return (double)_array[index];
	}

	protected override void SetElement(int index, Variant value)
	{
		_array[index] = (float)value.AsDouble();
	}

	protected override void AppendElement(Variant value)
	{
		float[] array = new float[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = (float)value.AsDouble();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		float[] array = new float[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedFloat32Array)
		{
			_array = value.AsFloat32Array();
		}
		else
		{
			_array = Array.Empty<float>();
		}
	}
}
