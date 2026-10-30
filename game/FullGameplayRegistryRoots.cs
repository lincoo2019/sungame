using System.Collections.Generic;

public sealed class FullGameplayRegistryRoots
{
	public IReadOnlyDictionary<string, string> ScenePathByCharacter { get; }

	public IReadOnlyDictionary<string, string> SpritePathByCharacter { get; }

	public IReadOnlyDictionary<string, string> PacketPathByName { get; }

	public IReadOnlyDictionary<string, string> CharacterNameByPacket { get; }

	public IReadOnlyList<string> UniqueSceneRoots { get; }

	public IReadOnlyList<string> UniqueSpritePaths { get; }

	public IReadOnlyList<string> UniquePacketRoots { get; }

	public int RegisteredCharacterCount { get; }

	public FullGameplayRegistryRoots(IReadOnlyDictionary<string, string> scenePathByCharacter, IReadOnlyDictionary<string, string> spritePathByCharacter, IReadOnlyDictionary<string, string> packetPathByName, IReadOnlyDictionary<string, string> characterNameByPacket, IReadOnlyList<string> uniqueSceneRoots, IReadOnlyList<string> uniqueSpritePaths, IReadOnlyList<string> uniquePacketRoots, int registeredCharacterCount)
	{
		ScenePathByCharacter = scenePathByCharacter;
		SpritePathByCharacter = spritePathByCharacter;
		PacketPathByName = packetPathByName;
		CharacterNameByPacket = characterNameByPacket;
		UniqueSceneRoots = uniqueSceneRoots;
		UniqueSpritePaths = uniqueSpritePaths;
		UniquePacketRoots = uniquePacketRoots;
		RegisteredCharacterCount = registeredCharacterCount;
	}
}
