using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWInlineTextProperty
{
	public Resource Owner { get; init; }

	public StringName Property { get; init; }

	public Variant.Type VariantType { get; init; }

	public PropertyHint Hint { get; init; }

	public string HintString { get; init; } = "";

	public string Category { get; init; } = "General";

	public string OwnerType { get; init; } = "Resource";

	public string Label { get; init; } = "";

	public XWInlineTextRole Role { get; init; }

	public string ClassificationReason { get; init; } = "";

	public bool IsSemantic
	{
		get
		{
			XWInlineTextRole role = Role;
			if ((uint)role <= 4u)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsTechnical => !IsSemantic;

	public bool IsTextEnum
	{
		get
		{
			bool flag = Hint == PropertyHint.Enum;
			if (flag)
			{
				Variant.Type variantType = VariantType;
				bool flag2 = ((variantType == Variant.Type.String || variantType == Variant.Type.StringName) ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	public bool IsNodePath => VariantType == Variant.Type.NodePath;

	public string CoverageKey => $"{OwnerType}.{Property}";
}
