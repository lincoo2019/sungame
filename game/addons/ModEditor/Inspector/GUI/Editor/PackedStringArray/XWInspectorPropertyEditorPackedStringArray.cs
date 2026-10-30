using System;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPackedStringArray : XWInspectorPropertyEditorPackedArrayBase
{
	private string[] _array = Array.Empty<string>();

	protected override string ArrayTypeName => "PackedStringArray";

	protected override Variant.Type ElementType => Variant.Type.String;

	protected override Variant GetDefaultElement()
	{
		return "";
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
		_array[index] = value.AsString();
	}

	protected override void AppendElement(Variant value)
	{
		string[] array = new string[_array.Length + 1];
		_array.CopyTo(array, 0);
		array[_array.Length] = value.AsString();
		_array = array;
	}

	protected override void RemoveAt(int index)
	{
		string[] array = new string[_array.Length - 1];
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
		if (value.VariantType == Variant.Type.PackedStringArray)
		{
			_array = value.AsStringArray();
		}
		else
		{
			_array = Array.Empty<string>();
		}
	}
}
