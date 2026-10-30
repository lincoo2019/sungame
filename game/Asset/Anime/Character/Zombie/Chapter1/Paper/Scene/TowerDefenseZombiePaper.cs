using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombiePaper : TowerDefenseZombie
{
	private const int GaspHeadFollowSlotId = 18;

	private const double AngryTimeScale = 3.0;

	private const string AngryWalkClip = "AngryWalk";

	private const string AngryAttackClip = "AngryEat";

	private const string ZOMBIE_PAPER_MADHEAD = "uid://s4hnj2igecma";

	private int _headFollowSlotIdBeforeGasp = -1;

	private StateHandle _gaspStateHandle;

	private bool _stateSignalsConnected;

	private bool _angry;

	public bool angry
	{
		get
		{
			return _angry;
		}
		set
		{
			_angry = value;
			ApplyAngryPresentation();
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
			_gaspStateHandle = base.StateMachine?.GetStateById("zombie.paper.gasp");
			StateHandle gaspStateHandle = _gaspStateHandle;
			if (gaspStateHandle != null && gaspStateHandle.IsValid)
			{
				_gaspStateHandle.Entered += GaspEntered;
				_gaspStateHandle.Exited += GaspExited;
				_gaspStateHandle.PhysicsProcessing += GaspProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_gaspStateHandle != null)
			{
				_gaspStateHandle.Entered -= GaspEntered;
				_gaspStateHandle.Exited -= GaspExited;
				_gaspStateHandle.PhysicsProcessing -= GaspProcessing;
			}
			_gaspStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConnectStateSignals();
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void GaspEntered()
	{
		if (GodotObject.IsInstanceValid(base.headSlot))
		{
			if (_headFollowSlotIdBeforeGasp < 0)
			{
				_headFollowSlotIdBeforeGasp = base.headSlot.followSlotId;
			}
			base.headSlot.followSlotId = 18;
		}
		base.sprite.SetAnimation("Gasp", loop: false);
	}

	public virtual void GaspProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void GaspExited()
	{
		if (GodotObject.IsInstanceValid(base.headSlot) && _headFollowSlotIdBeforeGasp >= 0)
		{
			base.headSlot.followSlotId = _headFollowSlotIdBeforeGasp;
		}
		_headFollowSlotIdBeforeGasp = -1;
	}

	private void ApplyAngryPresentation()
	{
		if (_angry)
		{
			base.walkAnimeClip = "AngryWalk";
			base.swimAnimeClip = "AngryWalk";
			base.attackAnimeClip = "AngryEat";
			base.attackWaterAnimeClip = "AngryEat";
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAtlasReplace("Zombie_head.png", "uid://s4hnj2igecma");
			}
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Paper")
		{
			SendStateEvent("ToGasp");
			AudioManager.Instance.AudioPlay("NewspaperRip");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Gasp")
		{
			AudioManager.Instance.AudioPlay("NewspaperRarrgh");
			timeScaleInit = 3.0;
			angry = true;
			Walk();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "angry", angry } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		angry = (bool)data.GetValueOrDefault("angry", false);
	}
}
