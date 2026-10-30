using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantKelpMine : TowerDefensePlant
{
	private const string KelpStateId = "plant.kelp_mine.kelp";

	private const string MineStateId = "plant.kelp_mine.mine";

	private const string ToKelpEvent = "ToKelp";

	private const string ToMineEvent = "ToMine";

	private const string ArmedTimer = "Armed";

	private const string BackClip = "Back";

	private const string KelpIdleClip = "Idle";

	private TanglekelpComponent _tanglekelpComponent;

	private PotatoComponent _potatoComponent;

	private CharacterTimerComponent _timerComponent;

	private StateHandle _kelpState;

	private StateHandle _mineState;

	private bool _stateSignalsConnected;

	public bool mineForm { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_tanglekelpComponent = componentManager.GetRuntime<TanglekelpComponent>();
			_potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			ApplyKelpForm();
			ConnectStateSignals();
			TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
			if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
			{
				_tanglekelpComponent.OnDrag += DragFinished;
			}
			CharacterTimerComponent timerComponent = _timerComponent;
			if (timerComponent != null && !timerComponent.IsReleased)
			{
				_timerComponent.OnTimeout += Timeout;
			}
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
		if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
		{
			_tanglekelpComponent.OnDrag -= DragFinished;
		}
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		base._ExitTree();
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		_kelpState = base.StateMachine?.GetStateById("plant.kelp_mine.kelp");
		_mineState = base.StateMachine?.GetStateById("plant.kelp_mine.mine");
		StateHandle mineState = _mineState;
		if (mineState != null && mineState.IsValid)
		{
			StateHandle kelpState = _kelpState;
			if (kelpState != null && kelpState.IsValid)
			{
				_mineState.Entered += MineEntered;
				_kelpState.Entered += KelpEntered;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_mineState != null)
			{
				_mineState.Entered -= MineEntered;
			}
			if (_kelpState != null)
			{
				_kelpState.Entered -= KelpEntered;
			}
			_kelpState = null;
			_mineState = null;
			_stateSignalsConnected = false;
		}
	}

	public virtual void DragFinished(TowerDefenseCharacter target, bool success)
	{
		if (success)
		{
			SendStateEvent("ToMine");
		}
	}

	public void Timeout(string timerName)
	{
		if (!(timerName != "Armed") && mineForm)
		{
			if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("Back"))
			{
				base.sprite.SetAnimation("Back", loop: false);
			}
			else
			{
				SendStateEvent("ToKelp");
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Back")
		{
			SendStateEvent("ToKelp");
		}
	}

	public void MineEntered()
	{
		mineForm = true;
		TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
		if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
		{
			_tanglekelpComponent.SetAlive(alive: false);
		}
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.SetAlive(alive: true);
			_potatoComponent.ReadyRise();
		}
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased && !_timerComponent.IsRunning("Armed"))
		{
			_timerComponent.Run("Armed");
		}
	}

	public void KelpEntered()
	{
		ApplyKelpForm();
		Idle();
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.invincible = false;
			instance.invincibleHurt = false;
			instance.invincibleSmash = false;
		}
		base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.PLANT;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.canBeCollection = true;
			instance.canCollection = true;
			instance.hologram = false;
		}
		TargetRegistrationComponent obj = targetRegistrationComponent;
		if (obj != null && !obj.IsReleased)
		{
			targetRegistrationComponent.canProjectileCheck = true;
			targetRegistrationComponent.RegisterTarget();
		}
		SetHitBoxEnabled(enabled: true);
		SetHitBoxMonitorable(monitorable: true);
		SetHitBoxSuppressed(HitBoxSuppressionReason.Tanglekelp, suppressed: false);
		SetHitBoxMonitorSuppressed(HitBoxSuppressionReason.Tanglekelp, suppressed: false);
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.timeScale = timeScale;
			if (base.sprite.HasClip("Idle"))
			{
				base.sprite.SetAnimation("Idle", loop: true, 0.2);
			}
		}
	}

	private void ApplyKelpForm()
	{
		mineForm = false;
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.SetAlive(alive: false);
		}
		TanglekelpComponent tanglekelpComponent = _tanglekelpComponent;
		if (tanglekelpComponent != null && !tanglekelpComponent.IsReleased)
		{
			_tanglekelpComponent.SetAlive(alive: true);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["mineForm"] = mineForm;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		if (data.GetValueOrDefault("mineForm", false).AsBool() && !mineForm)
		{
			CallDeferred("RestoreMineForm");
		}
	}

	private void RestoreMineForm()
	{
		if (!mineForm)
		{
			SendStateEvent("ToMine");
		}
	}
}
