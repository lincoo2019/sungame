using Godot.Collections;

public interface IBattleSessionNetworkContext
{
	void ApplyChooseReady(Dictionary data);

	void ApplyChooseOver();

	void ApplyPause(bool paused);

	void ApplyGameEntry(int roundNum);

	void ApplyTips(string text, double duration);

	void ApplyGameResult(bool victory, bool leave);

	void SendGameEntryAck();

	void SendClientReady();
}
