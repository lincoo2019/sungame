using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedVector2Array : XWInspectorPropertyEditorPackedArrayBase
{
	private Vector2[] _array = Array.Empty<Vector2>();

	protected override string ArrayTypeName => "PackedVector2Array";

	protected override Variant.Type ElementType => Variant.Type.Vector2;

	protected override Variant GetDefaultElement()
	{
		return Vector2.Zero;
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
		_array[index] = value.AsVector2();
	}

	protected override void AppendElement(Variant value)
	{
		Vector2[] array = new Vector2[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = value.AsVector2();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		Vector2[] array = new Vector2[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedVector2Array)
		{
			_array = value.AsVector2Array();
		}
		else
		{
			_array = Array.Empty<Vector2>();
		}
	}
}
