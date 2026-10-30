using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieJackGene : TowerDefenseZombie
{
	private StateHandle _bombStateHandle;

	private bool _stateSignalsConnected;

	private static PackedScene _JACKBOX_EXPLOSION;

	public static AudioStreamPlayerMember JackInTheBoxPlayer;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool hasJackBox = true;

	public bool over;

	public double timer;

	public double time;

	public bool explode;

	private static PackedScene JACKBOX_EXPLOSION => _JACKBOX_EXPLOSION ?? (_JACKBOX_EXPLOSION = GD.Load<PackedScene>("uid://cxnt2jbnk48fp"));

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_bombStateHandle = base.StateMachine?.GetStateById("zombie.jackgene.bomb");
			StateHandle bombStateHandle = _bombStateHandle;
			if (bombStateHandle != null && bombStateHandle.IsValid)
			{
				_bombStateHandle.Entered += BombEntered;
				_bombStateHandle.Exited += BombExited;
				_bombStateHandle.PhysicsProcessing += BombProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_bombStateHandle != null)
			{
				_bombStateHandle.Entered -= BombEntered;
				_bombStateHandle.Exited -= BombExited;
				_bombStateHandle.PhysicsProcessing -= BombProcessing;
			}
			_bombStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override async void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		ConnectStateSignals();
		AddToGroup("JackBox");
		time = GD.RandRange(9.0, 20.0);
		if (!TowerDefenseManager.Instance.IsGameRunning() || !inGame)
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(JackInTheBoxPlayer))
		{
			JackInTheBoxPlayer = AudioManager.Instance.MemberFind("JackInTheBox");
			JackInTheBoxPlayer.MaxPolyphony = 1;
			JackInTheBoxPlayer.ProcessMode = ProcessModeEnum.Pausable;
		}
		JackInTheBoxPlayer.Play();
		await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
		if (TowerDefenseManager.GetGameMethod() == TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE)
		{
			explode = true;
			instance.unUseBuffFlags += 128;
			if (hasJackBox && !nearDie && !die)
			{
				SendStateEvent("ToBomb");
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		RemoveFromGroup("JackBox");
		if (GodotObject.IsInstanceValid(JackInTheBoxPlayer) && GetTree().GetNodeCountInGroup("JackBox") <= 0)
		{
			JackInTheBoxPlayer.Stop();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !TowerDefenseManager.Instance.IsGameRunning() || !inGame || !base.IsInsideComponentBattlefield || explode || base.sprite.pause)
		{
			return;
		}
		if (timer < time)
		{
			timer += delta * timeScale;
			return;
		}
		explode = true;
		instance.unUseBuffFlags += 128;
		if (hasJackBox && !nearDie && !die)
		{
			SendStateEvent("ToBomb");
		}
	}

	public virtual void BombEntered()
	{
		AudioManager.Instance.AudioPlay("JackSurprise");
		base.sprite.SetAnimation("Bomb", loop: false, 0.2);
	}

	public virtual void BombProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void BombExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Bomb" && !over)
		{
			over = true;
			if (hasJackBox)
			{
				CreateEffect();
				Destroy();
			}
			else
			{
				base.Walk();
			}
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "JackGenebox")
		{
			hasJackBox = false;
		}
	}

	public override void Walk()
	{
		if (!explode)
		{
			base.Walk();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["hasJackBox"] = hasJackBox,
			["over"] = over,
			["timer"] = timer,
			["time"] = time,
			["explode"] = explode
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hasJackBox = data.GetValueOrDefault("hasJackBox", hasJackBox).AsBool();
		over = data.GetValueOrDefault("over", over).AsBool();
		timer = Math.Max(0.0, data.GetValueOrDefault("timer", timer).AsDouble());
		time = Math.Max(0.0, data.GetValueOrDefault("time", time).AsDouble());
		explode = data.GetValueOrDefault("explode", explode).AsBool();
	}

	public void CreateEffect()
	{
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(JACKBOX_EXPLOSION, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(1.25f, 1.25f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1, suppressDeathrattles: true);
		AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
	}
}
