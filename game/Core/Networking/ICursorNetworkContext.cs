using Godot;

public interface ICursorNetworkContext
{
	string LocalPeerId { get; }

	Vector2 GetCursorPosition();

	CursorPickState GetCursorPickState();

	void SendCursorPosition(Vector2 position);

	void SendCursorPick(CursorPickState pickState);

	void ApplyRemoteCursorPosition(string userId, Vector2 position);

	void ApplyRemoteCursorPick(string userId, CursorPickState pickState);

	void RemoveRemoteCursor(string userId);
}
