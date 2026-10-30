using System.Collections.Generic;

namespace PVZHE.ModEditor.CharacterEditor;

public sealed class XWCharacterModDefinition
{
	public enum CharacterCategory
	{
		Plant,
		Zombie,
		Vase,
		Mower,
		Item,
		Gravestone,
		Crater
	}

	public enum CreationMode
	{
		Blank,
		InheritAsNew,
		OverrideBuiltin
	}

	public CharacterCategory Category { get; set; }

	public CreationMode Mode { get; set; }

	public string BaseCharacterKey { get; set; } = "";

	public string TargetCharacterKey { get; set; } = "";

	public string SceneOverride { get; set; } = "";

	public string SpriteOverride { get; set; } = "";

	public Dictionary<string, object> PacketPatch { get; set; } = new Dictionary<string, object>();

	public Dictionary<string, object> CharacterConfigPatch { get; set; } = new Dictionary<string, object>();

	public List<ComponentPatchEntry> ComponentPatch { get; set; } = new List<ComponentPatchEntry>();
}
