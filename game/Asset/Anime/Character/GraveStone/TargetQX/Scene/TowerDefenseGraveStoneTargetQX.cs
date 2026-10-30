using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseGraveStoneTargetQX : TowerDefenseGravestone, INetworkSpawnStateReceiver
{
	private const double ReviveTime = 30.0;

	private StateHandle _downState;

	private StateHandle _downIdleState;

	private StateHandle _upState;

	private bool _stateSignalsConnected;

	public bool isDowned;

	private double _reviveTimer;

	private bool _hpDepleted;

	private bool _baseSetupDone;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_downState = base.StateMachine?.GetStateById("gravestone.qx.down");
			_downIdleState = base.StateMachine?.GetStateById("gravestone.qx.down_idle");
			_upState = base.StateMachine?.GetStateById("gravestone.qx.up");
			ConnectStateSignals();
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		base._ExitTree();
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			StateHandle downState = _downState;
			if (downState != null && downState.IsValid)
			{
				_downState.Entered += DownEntered;
			}
			StateHandle downIdleState = _downIdleState;
			if (downIdleState != null && downIdleState.IsValid)
			{
				_downIdleState.Entered += DownIdleEntered;
				_downIdleState.PhysicsProcessing += DownIdleProcessing;
			}
			StateHandle upState = _upState;
			if (upState != null && upState.IsValid)
			{
				_upState.Entered += UpEntered;
			}
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			StateHandle downState = _downState;
			if (downState != null && downState.IsValid)
			{
				_downState.Entered -= DownEntered;
			}
			StateHandle downIdleState = _downIdleState;
			if (downIdleState != null && downIdleState.IsValid)
			{
				_downIdleState.Entered -= DownIdleEntered;
				_downIdleState.PhysicsProcessing -= DownIdleProcessing;
			}
			StateHandle upState = _upState;
			if (upState != null && upState.IsValid)
			{
				_upState.Entered -= UpEntered;
			}
			_stateSignalsConnected = false;
		}
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		_hpDepleted = true;
	}

	public void SetupBaseState()
	{
		if (!_baseSetupDone)
		{
			_baseSetupDone = true;
			isDowned = true;
			SetHitBoxEnabled(enabled: false);
			instance.canBeCollection = false;
			SendStateEvent("ToDown");
		}
	}

	private void RestoreNormalState()
	{
		instance.canBeCollection = true;
		SetHitBoxEnabled(enabled: true);
		isDowned = false;
		_hpDepleted = false;
		_baseSetupDone = false;
		RestoreFullHealthAfterRevive();
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (isDowned)
		{
			SetupBaseState();
		}
	}

	public override void Destroy(bool freeInstance = true)
	{
		if (!isDowned && _hpDepleted)
		{
			SetupBaseState();
		}
		else
		{
			base.Destroy(freeInstance);
		}
	}

	private void DownEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("Down"))
		{
			base.sprite.SetAnimation("Down", loop: false);
		}
	}

	private void DownIdleEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("DownIdle"))
		{
			base.sprite.SetAnimation("DownIdle");
		}
		if (_reviveTimer <= 0.0 || _reviveTimer > 30.0)
		{
			_reviveTimer = 30.0;
		}
	}

	private void DownIdleProcessing(double delta)
	{
		if (base.sprite != null)
		{
			base.sprite.timeScale = timeScale;
		}
		_reviveTimer -= delta;
		if (_reviveTimer <= 0.0)
		{
			SendStateEvent("ToUp");
		}
	}

	private void UpEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("Up"))
		{
			base.sprite.SetAnimation("Up", loop: false);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Down")
		{
			SendStateEvent("ToDownIdle");
		}
		else if (clip == "Up")
		{
			RestoreNormalState();
			SendStateEvent("ToRevived");
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		bool flag = isDowned;
		isDowned = data?.GetValueOrDefault("isDowned", false).AsBool() ?? false;
		if (isDowned && !flag && IsNodeReady())
		{
			SetupBaseState();
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary { ["isDowned"] = isDowned };
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		isDowned = data?.GetValueOrDefault("isDowned", false).AsBool() ?? false;
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return isDowned ? 1 : 0;
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["isDowned"] = isDowned;
		dictionary["reviveTimer"] = _reviveTimer;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		isDowned = data.GetValueOrDefault("isDowned", false).AsBool();
		_reviveTimer = data.GetValueOrDefault("reviveTimer", 30.0).AsDouble();
		if (isDowned && IsNodeReady())
		{
			SetupBaseState();
		}
	}
}
