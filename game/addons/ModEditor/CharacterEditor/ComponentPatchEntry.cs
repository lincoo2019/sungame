namespace PVZHE.ModEditor.CharacterEditor;

public sealed class ComponentPatchEntry
{
	public string ComponentName { get; set; } = "";

	public string Action { get; set; } = "Inherit";

	public string ScenePath { get; set; } = "";
}
