using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseTimeNuke : TowerDefenseGravestone
{
	[Export(PropertyHint.None, "")]
	public double countdownTime = 120.0;

	[Export(PropertyHint.None, "")]
	public double readyDuration = 1.5;

	private CharacterTimerComponent _timerComponent;

	private ExplodeComponent _explodeComponent;

	private StateHandle _readyState;

	private bool _stateSignalsConnected;

	private bool _readyStarted;

	private bool _exploded;

	private int _lastDisplayedSecond = -1;

	private const string NumberTexturePath = "res://Asset/AtlasSource/DamagePoint/Anime/Character/GraveStone/TimeBomb/DamagePoint/Config/TimeBombNum_";

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode)
		{
			return;
		}
		_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout += OnTimeout;
		}
		_explodeComponent = componentManager.GetRuntime<ExplodeComponent>("character.explode");
		_readyState = base.StateMachine?.GetStateById("timenuke.ready");
		ConnectStateSignals();
		RemoveFromGroup("Gravestone");
		if (_readyStarted)
		{
			SendStateEvent("ToReady");
			return;
		}
		UpdateNumberDisplay(countdownTime);
		CharacterTimerComponent timerComponent2 = _timerComponent;
		if (timerComponent2 != null && !timerComponent2.IsReleased && !_timerComponent.IsRunning("Countdown"))
		{
			_timerComponent.Run("Countdown", countdownTime);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= OnTimeout;
		}
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			StateHandle readyState = _readyState;
			if (readyState != null && readyState.IsValid)
			{
				_readyState.Entered += ReadyEntered;
				_readyState.PhysicsProcessing += ReadyProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!_readyStarted)
		{
			UpdateRedFilter();
			double countdownRemaining = GetCountdownRemaining();
			int num = (int)Math.Ceiling(countdownRemaining);
			if (num != _lastDisplayedSecond)
			{
				UpdateNumberDisplay(countdownRemaining);
				_lastDisplayedSecond = num;
			}
		}
	}

	private double GetCountdownRemaining()
	{
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent == null || timerComponent.IsReleased)
		{
			return countdownTime;
		}
		double value;
		double num = (_timerComponent.timerWaitTime.TryGetValue("Countdown", out value) ? value : countdownTime);
		double value2;
		double num2 = (_timerComponent.timerCurrent.TryGetValue("Countdown", out value2) ? value2 : 0.0);
		return num - num2;
	}

	private void OnTimeout(string timerName)
	{
		if (!(timerName == "Countdown"))
		{
			if (timerName == "Ready")
			{
				Explode();
			}
		}
		else
		{
			_readyStarted = true;
			SendStateEvent("ToReady");
		}
	}

	private void ReadyEntered()
	{
		if (base.sprite != null && GodotObject.IsInstanceValid(base.sprite))
		{
			if (base.sprite.HasClip("Ready"))
			{
				base.sprite.SetAnimation("Ready");
				base.sprite.timeScale = 1.0;
			}
			base.sprite.SetRenderColorMultiplier(new Color(1f, 0.4f, 0.4f));
		}
		instance.invincible = true;
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased && !_timerComponent.IsRunning("Ready"))
		{
			_timerComponent.Run("Ready", readyDuration);
		}
	}

	private void ReadyProcessing(double delta)
	{
		if (base.sprite != null && GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.timeScale = Math.Min(base.sprite.timeScale + delta * 2.0, 4.0);
		}
	}

	private void UpdateRedFilter()
	{
		if (base.sprite != null && GodotObject.IsInstanceValid(base.sprite))
		{
			float num = Mathf.Clamp((Mathf.Clamp(1f - (float)(GetCountdownRemaining() / countdownTime), 0f, 1f) - 0.5f) * 2f, 0f, 1f);
			float num2 = 1f - num * 0.6f;
			base.sprite.SetRenderColorMultiplier(new Color(1f, num2, num2));
		}
	}

	private void UpdateNumberDisplay(double remaining)
	{
		if (base.sprite != null && GodotObject.IsInstanceValid(base.sprite))
		{
			int num = ((!(remaining <= 0.0)) ? ((int)Math.Ceiling(remaining)) : 0);
			int num2 = num / 60;
			int num3 = num % 60;
			int[] array = new int[4]
			{
				Math.Min(num2 / 10, 9),
				num2 % 10,
				num3 / 10 % 10,
				num3 % 10
			};
			for (int i = 0; i < 4; i++)
			{
				base.sprite.SetAtlasReplace("TimeBombNum_" + i + ".png", "res://Asset/AtlasSource/DamagePoint/Anime/Character/GraveStone/TimeBomb/DamagePoint/Config/TimeBombNum_" + array[i] + ".png");
			}
		}
	}

	public override void DestroySet()
	{
		if (!_exploded)
		{
			if (base.sprite != null && GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetRenderColorMultiplier(Colors.White);
			}
			base.DestroySet();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["readyStarted"] = _readyStarted };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_readyStarted = data.GetValueOrDefault("readyStarted", false).AsBool();
	}

	private void Explode()
	{
		if (!_exploded)
		{
			_exploded = true;
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.Explode();
			}
			Destroy();
		}
	}
}
