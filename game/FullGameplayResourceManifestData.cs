using System.Collections.Generic;

public sealed class FullGameplayResourceManifestData
{
	public IReadOnlyList<string> CharacterSceneRoots { get; }

	public IReadOnlyList<string> IndependentSpriteRoots { get; }

	public IReadOnlyList<string> PacketRoots { get; }

	public int RegisteredCharacterCount { get; }

	public int RegisteredSpriteCount { get; }

	public int RegisteredPacketCount { get; }

	public string DependencyGraphSignature { get; }

	public FullGameplayResourceManifestData(IReadOnlyList<string> characterSceneRoots, IReadOnlyList<string> independentSpriteRoots, IReadOnlyList<string> packetRoots, int registeredCharacterCount, int registeredSpriteCount, int registeredPacketCount, string dependencyGraphSignature)
	{
		CharacterSceneRoots = characterSceneRoots;
		IndependentSpriteRoots = independentSpriteRoots;
		PacketRoots = packetRoots;
		RegisteredCharacterCount = registeredCharacterCount;
		RegisteredSpriteCount = registeredSpriteCount;
		RegisteredPacketCount = registeredPacketCount;
		DependencyGraphSignature = dependencyGraphSignature;
	}
}
