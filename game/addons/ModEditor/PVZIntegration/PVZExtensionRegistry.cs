using Godot;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ResourceEditors;

namespace PVZHE.ModEditor.PVZIntegration;

public static class PVZExtensionRegistry
{
	public static void Initialize()
	{
		XWResourceEditorRegistry.RegisterDefaultEditors();
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("dat", new DatExtensionMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("json", new JsonExtensionMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("tscn", new TscnExtensionMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("tres", new TresExtensionMethod());
		XWFileSystemExtensionImageMethod method = new XWFileSystemExtensionImageMethod();
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("png", method);
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("jpg", method);
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("jpeg", method);
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("svg", method);
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("webp", method);
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("gd", new JsonExtensionMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("cs", new JsonExtensionMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("csv", new XWFileSystemExtensionLocalizationCsvMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("cfg", new JsonExtensionMethod());
		XWFileSystemExtensionRegistry.RegisterExtensionMethod("txt", new JsonExtensionMethod());
		GD.Print("[PVZExtensionRegistry] 已注册 PVZ 文件扩展: dat, json, tscn, tres, png, jpg, jpeg, svg, webp, gd, cs, csv, cfg, txt");
	}
}
