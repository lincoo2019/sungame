using System;
using System.Collections.Generic;
using System.Linq;

namespace PVZHE.ModEditor.Debugging;

public sealed class XWModDebugVariable
{
	public string Name { get; }

	public string TypeName { get; }

	public string DisplayValue { get; }

	public IReadOnlyList<XWModDebugVariable> Children { get; }

	public XWModDebugVariable(string name, string typeName, string displayValue, IReadOnlyList<XWModDebugVariable> children = null)
	{
		Name = name?.Trim() ?? string.Empty;
		TypeName = typeName?.Trim() ?? string.Empty;
		DisplayValue = displayValue ?? string.Empty;
		Children = Array.AsReadOnly((from child in children ?? Array.Empty<XWModDebugVariable>()
			where child != null
			select child.Copy()).ToArray());
	}

	internal XWModDebugVariable Copy()
	{
		return new XWModDebugVariable(Name, TypeName, DisplayValue, Children);
	}
}
