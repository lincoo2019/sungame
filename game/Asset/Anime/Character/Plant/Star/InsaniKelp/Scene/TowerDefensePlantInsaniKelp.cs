using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantInsaniKelp : TowerDefensePlant
{
	private CharacterTimerComponent _timerComponent;

	private MousePressComponent _mousePressComponent;

	private StateHandle _openState;

	private StateHandle _openIdleState;

	private StateHandle _closeState;

	private bool _roleStateSignalsConnected;

	public bool isTanglekelp;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_mousePressComponent = componentManager.GetRuntime<MousePressComponent>();
			_timerComponent.OnTimeout += Timeout;
			_mousePressComponent.OnPressed += Pressed;
			_openState = base.StateMachine?.GetStateById("plant.insani_kelp.open");
			_openIdleState = base.StateMachine?.GetStateById("plant.insani_kelp.open_idle");
			_closeState = base.StateMachine?.GetStateById("plant.insani_kelp.close");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		if (_mousePressComponent != null)
		{
			_mousePressComponent.OnPressed -= Pressed;
		}
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		StateHandle openState = _openState;
		if (openState == null || !openState.IsValid)
		{
			return;
		}
		StateHandle openIdleState = _openIdleState;
		if (openIdleState != null && openIdleState.IsValid)
		{
			StateHandle closeState = _closeState;
			if (closeState != null && closeState.IsValid)
			{
				_openState.Entered += OpenEntered;
				_openState.Exited += OpenExited;
				_openState.PhysicsProcessing += OpenProcessing;
				_openIdleState.Entered += OpenIdleEntered;
				_openIdleState.Exited += OpenIdleExited;
				_openIdleState.PhysicsProcessing += OpenIdleProcessing;
				_closeState.Entered += CloseEntered;
				_closeState.Exited += CloseExited;
				_closeState.PhysicsProcessing += CloseProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_openState.Entered -= OpenEntered;
			_openState.Exited -= OpenExited;
			_openState.PhysicsProcessing -= OpenProcessing;
			_openIdleState.Entered -= OpenIdleEntered;
			_openIdleState.Exited -= OpenIdleExited;
			_openIdleState.PhysicsProcessing -= OpenIdleProcessing;
			_closeState.Entered -= CloseEntered;
			_closeState.Exited -= CloseExited;
			_closeState.PhysicsProcessing -= CloseProcessing;
			_openState = null;
			_openIdleState = null;
			_closeState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Open"))
		{
			_timerComponent.Run("Open");
		}
	}

	public virtual void OpenEntered()
	{
		isTanglekelp = (double)GD.Randf() < 0.1;
		base.sprite.SetAnimation(isTanglekelp ? "OpenB" : "OpenA", loop: false, 0.2);
	}

	public virtual void OpenProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public virtual void OpenExited()
	{
	}

	public virtual void OpenIdleEntered()
	{
		_mousePressComponent.SetAlive(alive: true);
		if (instance.hypnoses)
		{
			Pressed(GetLogicalGlobalPosition());
			_mousePressComponent.SetAlive(alive: false);
		}
		base.sprite.SetAnimation(isTanglekelp ? "IdleB" : "IdleA", loop: true, 0.2);
	}

	public virtual void OpenIdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public virtual void OpenIdleExited()
	{
		_mousePressComponent.SetAlive(alive: false);
	}

	public virtual void CloseEntered()
	{
		base.sprite.SetAnimation("Close", loop: true, 0.2);
	}

	public virtual void CloseProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public virtual void CloseExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		switch (clip)
		{
		case "OpenA":
		case "OpenB":
			SendStateEvent("ToOpenIdle");
			break;
		case "Close":
			Idle();
			break;
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Open")
		{
			SendStateEvent("ToOpen");
		}
	}

	public void Pressed(Vector2 pos)
	{
		Vector2 pos2 = (instance.hypnoses ? pos : GetLogicalGlobalPosition());
		long num = (isTanglekelp ? 50 : 25);
		EconomyAccountId accountId = (base.HasEconomyOwner ? base.EconomyOwnerAccountId : EconomyAccountId.Local);
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			if (!instance.hypnoses && !TowerDefenseManager.Instance.TrySpendSun(accountId, num))
			{
				return;
			}
		}
		else
		{
			BrainSunCreate(pos2, num);
		}
		SendStateEvent("ToClose");
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		string packetName = (isTanglekelp ? "ZombieSnorkleTanglekelp" : "ZombieSnorkle");
		TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? TowerDefenseCharacter.CreateCharacter(base.EconomyOwnerAccountId, packetName, pos2, base.gridPos, base.groundHeight) : TowerDefenseCharacter.CreateCharacter(packetName, pos2, base.gridPos, base.groundHeight));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		towerDefenseCharacter.CallDeferred("Walk");
		if (!instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 0.0, useCreate: true, pos2.X, pos2.Y, walkAfterSpawn: true, base.groundHeight);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "isTanglekelp", isTanglekelp } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isTanglekelp = data.GetValueOrDefault("isTanglekelp", Variant.From<bool>(false)).AsBool();
	}
}
