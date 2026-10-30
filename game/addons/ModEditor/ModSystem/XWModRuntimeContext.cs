using System;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModRuntimeContext
{
	public string ModId { get; }

	public string ModVersion { get; }

	public string PackageRoot { get; }

	public int ApiVersion => 1;

	public bool IsAndroid => OperatingSystem.IsAndroid();

	internal XWModRuntimeContext(XWModManifest manifest, string packageRoot)
	{
		if (manifest == null)
		{
			throw new ArgumentNullException("manifest");
		}
		ModId = manifest.Id ?? "";
		ModVersion = manifest.Version ?? "";
		PackageRoot = packageRoot ?? "";
	}

	public bool TryRegister(string category, string key, Variant value, bool allowOverride, out string diagnostic)
	{
		return XWModRuntimeRegistry.Register(ModId, category, key, value, allowOverride, out diagnostic);
	}

	public void Log(string message)
	{
		GD.Print("[Mod:" + ModId + "] " + message);
	}

	public void Warn(string message)
	{
		GD.PushWarning("[Mod:" + ModId + "] " + message);
	}
}
