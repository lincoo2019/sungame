using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedInt32Array : XWInspectorPropertyEditorPackedArrayBase
{
	private int[] _array = Array.Empty<int>();

	protected override string ArrayTypeName => "PackedInt32Array";

	protected override Variant.Type ElementType => Variant.Type.Int;

	protected override Variant GetDefaultElement()
	{
		return 0L;
	}

	protected override int GetArraySize()
	{
		return _array.Length;
	}

	protected override Variant GetElement(int index)
	{
		return (long)_array[index];
	}

	protected override void SetElement(int index, Variant value)
	{
		_array[index] = (int)value.AsInt64();
	}

	protected override void AppendElement(Variant value)
	{
		int[] array = new int[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = (int)value.AsInt64();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		int[] array = new int[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedInt32Array)
		{
			_array = value.AsInt32Array();
		}
		else
		{
			_array = Array.Empty<int>();
		}
	}
}
