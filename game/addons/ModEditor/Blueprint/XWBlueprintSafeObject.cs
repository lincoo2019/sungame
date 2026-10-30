using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public sealed partial class XWBlueprintSafeObject : RefCounted
{
	private readonly Dictionary<string, Variant> _properties;

	private readonly HashSet<string> _writableProperties;

	private readonly HashSet<string> _allowedMethods;

	public string ClassName { get; }

	public string SourcePath { get; }

	internal XWBlueprintSafeObject(string className, string sourcePath, Dictionary<string, Variant> properties, IEnumerable<string> writableProperties, IEnumerable<string> allowedMethods)
	{
		ClassName = (string.IsNullOrWhiteSpace(className) ? "Object" : className.Trim());
		SourcePath = sourcePath ?? string.Empty;
		_properties = properties ?? new Dictionary<string, Variant>(StringComparer.Ordinal);
		_writableProperties = new HashSet<string>(writableProperties ?? Array.Empty<string>(), StringComparer.Ordinal);
		_allowedMethods = new HashSet<string>(allowedMethods ?? Array.Empty<string>(), StringComparer.Ordinal);
	}

	internal bool MatchesClass(string expectedClass)
	{
		bool flag = string.IsNullOrWhiteSpace(expectedClass);
		if (!flag)
		{
			bool flag2 = ((expectedClass == "Object" || expectedClass == "GodotObject") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return true;
		}
		if (expectedClass == "Resource")
		{
			return !string.IsNullOrWhiteSpace(SourcePath);
		}
		return string.Equals(ClassName, expectedClass.Trim(), StringComparison.Ordinal);
	}

	internal bool TryGetProperty(string name, out Variant value)
	{
		value = default(Variant);
		if (IsSafeIdentifier(name))
		{
			return _properties.TryGetValue(name, out value);
		}
		return false;
	}

	internal bool TrySetProperty(string name, Variant value)
	{
		if (!IsSafeIdentifier(name) || !_writableProperties.Contains(name))
		{
			return false;
		}
		_properties[name] = value;
		return true;
	}

	internal bool IsMethodAllowed(string methodName)
	{
		if (IsSafeIdentifier(methodName))
		{
			return _allowedMethods.Contains(methodName);
		}
		return false;
	}

	internal bool TryInvoke(string methodName, IReadOnlyList<Variant> arguments, out Variant result)
	{
		result = default(Variant);
		if (!IsMethodAllowed(methodName))
		{
			return false;
		}
		switch (methodName)
		{
		case "has_property":
		{
			bool flag = arguments.Count == 1;
			if (flag)
			{
				Variant.Type variantType = arguments[0].VariantType;
				bool flag2 = ((variantType == Variant.Type.String || variantType == Variant.Type.StringName) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				result = Variant.From<bool>(_properties.ContainsKey(arguments[0].AsString()));
				return true;
			}
			break;
		}
		case "get_property_count":
			if (arguments.Count == 0)
			{
				result = Variant.From<long>((long)_properties.Count);
				return true;
			}
			break;
		case "get_type":
			if (arguments.Count == 0)
			{
				result = Variant.From<string>(ClassName);
				return true;
			}
			break;
		case "get_resource_name":
		{
			if (arguments.Count == 0 && _properties.TryGetValue("resource_name", out var value))
			{
				result = value;
				return true;
			}
			break;
		}
		case "get_resource_path":
			if (arguments.Count == 0)
			{
				result = Variant.From<string>(SourcePath);
				return true;
			}
			break;
		}
		return false;
	}

	internal static bool IsSafeIdentifier(string value)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
		{
			return false;
		}
		if (!char.IsLetter(value[0]) && value[0] != '_')
		{
			return false;
		}
		for (int i = 1; i < value.Length; i++)
		{
			char c = value[i];
			if (!char.IsLetterOrDigit(c) && c != '_')
			{
				return false;
			}
		}
		return true;
	}
}
