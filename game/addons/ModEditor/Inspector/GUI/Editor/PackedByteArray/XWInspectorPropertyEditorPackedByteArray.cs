using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedByteArray : XWInspectorPropertyEditorPackedArrayBase
{
	private byte[] _array = Array.Empty<byte>();

	protected override string ArrayTypeName => "PackedByteArray";

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
		_array[index] = (byte)value.AsInt64();
	}

	protected override void AppendElement(Variant value)
	{
		byte[] array = new byte[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = (byte)value.AsInt64();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		byte[] array = new byte[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedByteArray)
		{
			_array = value.AsByteArray();
		}
		else
		{
			_array = Array.Empty<byte>();
		}
	}
}
