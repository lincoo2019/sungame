using Godot;

namespace PVZHE.ModEditor.PVZIntegration;

public static class PVZIntegrationModule
{
	private static bool _initialized;

	public static void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			PVZExtensionRegistry.Initialize();
			PVZApiRegistry.Initialize();
			GD.Print("[PVZIntegrationModule] PVZ 集成模块已初始化");
		}
	}
}
