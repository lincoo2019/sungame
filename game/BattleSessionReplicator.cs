using Godot.Collections;

public sealed class BattleSessionReplicator : NetworkReplicatorBase
{
	private readonly IBattleSessionNetworkContext _sessionContext;

	public BattleSessionReplicator()
	{
	}

	public BattleSessionReplicator(IBattleSessionNetworkContext sessionContext)
	{
		_sessionContext = sessionContext;
	}

	public void ApplyChooseReady(Dictionary data)
	{
		_sessionContext?.ApplyChooseReady(data);
	}

	public void ApplyChooseOver()
	{
		_sessionContext?.ApplyChooseOver();
	}

	public void ApplyPause(bool paused)
	{
		_sessionContext?.ApplyPause(paused);
	}

	public void ApplyClientReady(UserIdDto dto)
	{
		if (dto != null && dto.user_id != null && MultiPlayerManager.Instance != null)
		{
			string user_id = dto.user_id;
			Array clientsReady = MultiPlayerManager.Instance.ClientsReady;
			if (!clientsReady.Contains(user_id))
			{
				clientsReady.Add(user_id);
			}
			if (MultiPlayerManager.Instance.CheckAllClientsReady())
			{
				MultiPlayerManager.Instance.EmitAllClientsReady();
			}
		}
	}

	public void ApplyGameEntry(GameEntryDto dto)
	{
		_sessionContext?.ApplyGameEntry(dto?.round_num ?? 0);
		_sessionContext?.SendGameEntryAck();
	}

	public void ApplyGameEntryAck(UserIdDto dto)
	{
		if (dto != null && dto.user_id != null && MultiPlayerManager.Instance != null)
		{
			string user_id = dto.user_id;
			Array gameEntryAcked = MultiPlayerManager.Instance.GameEntryAcked;
			if (!gameEntryAcked.Contains(user_id))
			{
				gameEntryAcked.Add(user_id);
			}
			if (MultiPlayerManager.Instance.CheckAllGameEntryAcked())
			{
				MultiPlayerManager.Instance.EmitAllGameEntryAcked();
			}
		}
	}

	public void ApplyTips(TipsPlayDto dto)
	{
		if (dto != null && dto.text != "")
		{
			_sessionContext?.ApplyTips(dto.text, dto.duration);
		}
	}

	public void ApplyGameResult(GameResultDto dto)
	{
		if (dto != null)
		{
			_sessionContext?.ApplyGameResult(dto.victory, dto.leave);
		}
	}
}
