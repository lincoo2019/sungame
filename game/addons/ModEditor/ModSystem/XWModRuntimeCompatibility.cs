using System;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModRuntimeCompatibility
{
	public static bool HasManagedCode(XWModManifest manifest)
	{
		if (manifest != null)
		{
			if (string.IsNullOrWhiteSpace(manifest.RuntimeAssembly))
			{
				return !string.IsNullOrWhiteSpace(manifest.RuntimeEntryType);
			}
			return true;
		}
		return false;
	}

	public static bool ValidatePackage(XWModManifest manifest, out string diagnostic)
	{
		diagnostic = "";
		if (!HasManagedCode(manifest))
		{
			return true;
		}
		string text = ((manifest.SchemaVersion != 2) ? $"schemaVersion={manifest.SchemaVersion}, expected={2}" : ((manifest.RuntimeApiVersion != 1) ? $"runtimeApiVersion={manifest.RuntimeApiVersion}, expected={1}" : (string.IsNullOrWhiteSpace(manifest.RuntimeAssembly) ? "runtimeEntryType requires runtimeAssembly" : ((!string.Equals(manifest.RuntimeAssemblyPolicy?.Trim(), "required", StringComparison.OrdinalIgnoreCase) && !string.Equals(manifest.RuntimeAssemblyPolicy?.Trim(), "optional", StringComparison.OrdinalIgnoreCase)) ? ("runtimeAssemblyPolicy='" + manifest.RuntimeAssemblyPolicy + "', expected=required|optional") : ""))));
		if (text.Length == 0)
		{
			return true;
		}
		diagnostic = $"Mod '{manifest.Id}' 托管兼容声明无效：{text}；请使用当前工具链重新编译并重新导出。";
		return false;
	}
}
