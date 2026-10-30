namespace PVZHE.ModEditor.ModSystem;

public interface IXWModRuntimeEntry
{
	void Initialize(XWModRuntimeContext context);

	void OnAllModsLoaded();

	void Shutdown();
}
