using Godot;
using Godot.Collections;

public static class NetworkVariantComparer
{
	public static bool DictionaryApproxEquals(Dictionary a, Dictionary b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		foreach (Variant key in a.Keys)
		{
			if (!b.ContainsKey(key))
			{
				return false;
			}
			Variant variant = a[key];
			Variant variant2 = b[key];
			if (variant.VariantType == Variant.Type.Dictionary && variant2.VariantType == Variant.Type.Dictionary)
			{
				if (!DictionaryApproxEquals(variant.AsGodotDictionary(), variant2.AsGodotDictionary()))
				{
					return false;
				}
			}
			else if (variant.VariantType == Variant.Type.Array && variant2.VariantType == Variant.Type.Array)
			{
				if (!ArrayApproxEquals(variant.AsGodotArray(), variant2.AsGodotArray()))
				{
					return false;
				}
			}
			else if (variant.VariantType == Variant.Type.Float && variant2.VariantType == Variant.Type.Float)
			{
				if (Mathf.Abs((float)variant.AsDouble() - (float)variant2.AsDouble()) > 0.05f)
				{
					return false;
				}
			}
			else if (!variant.Equals(variant2))
			{
				return false;
			}
		}
		return true;
	}

	private static bool ArrayApproxEquals(Array arrA, Array arrB)
	{
		if (arrA.Count != arrB.Count)
		{
			return false;
		}
		for (int i = 0; i < arrA.Count; i++)
		{
			if (arrA[i].VariantType == Variant.Type.Dictionary && arrB[i].VariantType == Variant.Type.Dictionary)
			{
				if (!DictionaryApproxEquals(arrA[i].AsGodotDictionary(), arrB[i].AsGodotDictionary()))
				{
					return false;
				}
			}
			else if (arrA[i].VariantType == Variant.Type.Float && arrB[i].VariantType == Variant.Type.Float)
			{
				if (Mathf.Abs((float)arrA[i].AsDouble() - (float)arrB[i].AsDouble()) > 0.05f)
				{
					return false;
				}
			}
			else if (!arrA[i].Equals(arrB[i]))
			{
				return false;
			}
		}
		return true;
	}
}
