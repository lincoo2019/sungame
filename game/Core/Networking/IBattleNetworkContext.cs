public interface IBattleNetworkContext
{
	bool IsMultiplayerActive { get; }

	bool IsHost { get; }

	double GameTime { get; }

	void ApplyPause(bool paused);
}
