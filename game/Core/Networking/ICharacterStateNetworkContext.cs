using System.Collections.Generic;
using Godot.Collections;

public interface ICharacterStateNetworkContext
{
	IDictionary<int, TowerDefenseCharacter> SyncCharacters { get; }

	Dictionary PendingDestroySyncIds { get; }

	Dictionary CharacterLastSyncState { get; }

	Dictionary ComponentLastSyncState { get; }

	void SendCharacterState(string charactersJson);

	void SendCharacterDestroy(int syncId, bool isExplode, bool isSmash);

	void SendCharacterInit(int syncId, double x, double y, double hp, bool die, string clipName, bool loop, double blendTime, int frame, double timeScale, double walkSpeedScale);

	void SendCharacterPosition(int syncId, double x, double y);

	void RemoveCharacterSyncState(int syncId);

	void CleanupCharacterCell(TowerDefenseCharacter character);
}
