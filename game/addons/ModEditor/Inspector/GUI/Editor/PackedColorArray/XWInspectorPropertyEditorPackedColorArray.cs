using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedColorArray : XWInspectorPropertyEditorPackedArrayBase
{
	private Color[] _array = Array.Empty<Color>();

	protected override string ArrayTypeName => "PackedColorArray";

	protected override Variant.Type ElementType => Variant.Type.Color;

	protected override Variant GetDefaultElement()
	{
		return default(Color);
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
		_array[index] = value.AsColor();
	}

	protected override void AppendElement(Variant value)
	{
		Color[] array = new Color[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = value.AsColor();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		Color[] array = new Color[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedColorArray)
		{
			_array = value.AsColorArray();
		}
		else
		{
			_array = Array.Empty<Color>();
		}
	}
}
