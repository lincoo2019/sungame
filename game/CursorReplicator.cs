using Godot;
using Godot.Collections;

public sealed class CursorReplicator : NetworkReplicatorBase
{
	private const double CursorSyncInterval = 0.033;

	private readonly ICursorNetworkContext _cursorContext;

	private Vector2 _lastCursorPosition = Vector2.Zero;

	private string _lastPickType = "";

	private string _lastPickName = "";

	private int _cursorSyncCounter;

	private double _syncTimer;

	public CursorReplicator()
	{
	}

	public CursorReplicator(ICursorNetworkContext cursorContext)
	{
		_cursorContext = cursorContext;
	}

	public override void Process(double delta)
	{
		if (base.Context != null && base.Context.IsMultiplayerActive && _cursorContext != null)
		{
			_syncTimer += delta;
			if (_syncTimer >= 0.033)
			{
				_syncTimer = 0.0;
				SyncLocalCursor();
			}
		}
	}

	public void SyncLocalCursor()
	{
		if (base.Context != null && base.Context.IsMultiplayerActive && _cursorContext != null)
		{
			Vector2 cursorPosition = _cursorContext.GetCursorPosition();
			CursorPickState cursorPickState = _cursorContext.GetCursorPickState();
			bool flag = cursorPickState.PickType != _lastPickType || cursorPickState.PickName != _lastPickName;
			_cursorSyncCounter++;
			if (cursorPosition.DistanceSquaredTo(_lastCursorPosition) > 25f || flag || _cursorSyncCounter >= 10)
			{
				_cursorContext.SendCursorPosition(cursorPosition);
				_lastCursorPosition = cursorPosition;
				_cursorSyncCounter = 0;
			}
			if (flag)
			{
				_lastPickType = cursorPickState.PickType;
				_lastPickName = cursorPickState.PickName;
				_cursorContext.SendCursorPick(cursorPickState);
			}
		}
	}

	public void ApplyCursorState(Dictionary data)
	{
		if (_cursorContext != null && data != null)
		{
			string text = data.GetValueOrDefault("user_id", "").AsString();
			if (!(text == "") && !(text == _cursorContext.LocalPeerId))
			{
				float x = (float)data.GetValueOrDefault("x", -100.0).AsDouble();
				float y = (float)data.GetValueOrDefault("y", -100.0).AsDouble();
				_cursorContext.ApplyRemoteCursorPosition(text, new Vector2(x, y));
			}
		}
	}

	public void ApplyCursorPickState(Dictionary data)
	{
		if (_cursorContext != null && data != null)
		{
			string text = data.GetValueOrDefault("user_id", "").AsString();
			if (!(text == "") && !(text == _cursorContext.LocalPeerId))
			{
				CursorPickState pickState = new CursorPickState(data.GetValueOrDefault("pickType", "").AsString(), data.GetValueOrDefault("pickName", "").AsString());
				_cursorContext.ApplyRemoteCursorPick(text, pickState);
			}
		}
	}

	public void RemoveRemoteCursor(string userId)
	{
		if (_cursorContext != null && !(userId == ""))
		{
			_cursorContext.RemoveRemoteCursor(userId);
		}
	}
}
