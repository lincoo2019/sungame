using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedInt64Array : XWInspectorPropertyEditorPackedArrayBase
{
	private long[] _array = Array.Empty<long>();

	protected override string ArrayTypeName => "PackedInt64Array";

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
		return _array[index];
	}

	protected override void SetElement(int index, Variant value)
	{
		_array[index] = value.AsInt64();
	}

	protected override void AppendElement(Variant value)
	{
		long[] array = new long[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = value.AsInt64();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		long[] array = new long[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedInt64Array)
		{
			_array = value.AsInt64Array();
		}
		else
		{
			_array = Array.Empty<long>();
		}
	}
}
