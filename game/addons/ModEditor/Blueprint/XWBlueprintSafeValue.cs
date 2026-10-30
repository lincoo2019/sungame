using System;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

internal static class XWBlueprintSafeValue
{
	public static bool TryClone(Variant source, XWBlueprintRuntimeLimits limits, Func<XWBlueprintSafeObject, bool> objectAllowed, ref int clonedValues, int depth, out Variant clone, out string diagnostic)
	{
		clone = default(Variant);
		diagnostic = string.Empty;
		if (depth > limits.MaxValueDepth)
		{
			diagnostic = $"Value exceeds the {limits.MaxValueDepth} level preview depth limit.";
			return false;
		}
		clonedValues++;
		if (clonedValues > limits.MaxClonedValues)
		{
			diagnostic = $"Preview copied more than {limits.MaxClonedValues} values.";
			return false;
		}
		Variant.Type variantType = source.VariantType;
		if (variantType != Variant.Type.String)
		{
			Variant.Type num = variantType - 23;
			if ((ulong)num <= 5uL)
			{
				switch ((int)num)
				{
				case 5:
				{
					Godot.Collections.Array array = source.AsGodotArray();
					if (array.Count > limits.MaxCollectionItems)
					{
						diagnostic = $"Array exceeds the {limits.MaxCollectionItems} item preview limit.";
						return false;
					}
					Godot.Collections.Array from2 = new Godot.Collections.Array();
					foreach (Variant item in array)
					{
						if (!TryClone(item, limits, objectAllowed, ref clonedValues, depth + 1, out var clone2, out diagnostic))
						{
							return false;
						}
						from2.Add(clone2);
					}
					clone = Variant.From(in from2);
					return true;
				}
				case 4:
				{
					Dictionary dictionary = source.AsGodotDictionary();
					if (dictionary.Count > limits.MaxCollectionItems)
					{
						diagnostic = $"Dictionary exceeds the {limits.MaxCollectionItems} item preview limit.";
						return false;
					}
					Dictionary from3 = new Dictionary();
					foreach (Variant key in dictionary.Keys)
					{
						if (!TryClone(key, limits, objectAllowed, ref clonedValues, depth + 1, out var clone3, out diagnostic) || !TryClone(dictionary[key], limits, objectAllowed, ref clonedValues, depth + 1, out var clone4, out diagnostic))
						{
							return false;
						}
						from3[clone3] = clone4;
					}
					clone = Variant.From(in from3);
					return true;
				}
				case 1:
				{
					XWBlueprintSafeObject from = source.AsGodotObject() as XWBlueprintSafeObject;
					if (from != null && GodotObject.IsInstanceValid(from) && objectAllowed != null && objectAllowed(from))
					{
						clone = Variant.From(in from);
						return true;
					}
					diagnostic = "Arbitrary Object and Resource values are blocked in editor-safe Blueprint preview.";
					return false;
				}
				case 0:
				case 2:
				case 3:
					diagnostic = $"Capability value '{source.VariantType}' is blocked in editor-safe Blueprint preview.";
					return false;
				}
			}
			clone = source;
			return true;
		}
		string from4 = source.AsString();
		if (from4.Length > limits.MaxStringLength)
		{
			diagnostic = $"String exceeds the {limits.MaxStringLength} character preview limit.";
			return false;
		}
		clone = Variant.From(in from4);
		return true;
	}
}
