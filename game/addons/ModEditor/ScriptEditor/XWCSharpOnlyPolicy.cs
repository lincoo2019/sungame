using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public static class XWCSharpOnlyPolicy
{
	private static readonly HashSet<string> UnsupportedScriptSourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		".gd", ".gdscript", ".lua", ".py", ".pyw", ".js", ".mjs", ".cjs", ".ts", ".tsx",
		".jsx", ".csx", ".vb", ".fs", ".fsx", ".rb", ".php"
	};

	public static bool IsSupportedCSharpScriptPath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static bool IsUnsupportedScriptSourcePath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return UnsupportedScriptSourceExtensions.Contains(Path.GetExtension(path));
		}
		return false;
	}

	public static bool IsSupportedModScriptResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource) || !(resource is Script))
		{
			return true;
		}
		if (!string.Equals(resource.GetClass(), "CSharpScript", StringComparison.Ordinal))
		{
			return IsSupportedCSharpScriptPath(resource.ResourcePath);
		}
		return true;
	}
}
