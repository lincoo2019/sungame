using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieGargantuarPaper : TowerDefenseZombieGargantuarBase
{
	private const string GaspClip = "Gasp";

	private const string AngryWalkClip = "WalkNoPaper";

	private const string AngryAttackClip = "SmashNoPaper";

	private const string AngryDeathClip = "DeathNoPaper";

	private const double AngryTimeScale = 7.5;

	private const double AngryBonusHitpoints = 3000.0;

	private StateHandle _gaspStateHandle;

	private bool _stateSignalsConnected;

	private bool _angry;

	private bool _gasping;

	private bool _keepAliveBeforeGasp;

	private double _damageHitpointsFloorBeforeGasp;

	private bool _angryBonusApplied;

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			impThrowFlag = false;
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
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_gaspStateHandle = base.StateMachine?.GetStateById("zombie.gargantuar_paper.gasp");
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

	public virtual void GaspEntered()
	{
		if (GodotObject.IsInstanceValid(instance))
		{
			if (!_gasping)
			{
				_keepAliveBeforeGasp = instance.keepAlive;
				_damageHitpointsFloorBeforeGasp = instance.damageHitpointsFloor;
			}
			instance.keepAlive = true;
			instance.damageHitpointsFloor = Math.Max(1.0, _damageHitpointsFloorBeforeGasp);
		}
		_gasping = true;
		base.sprite.SetAnimation("Gasp", loop: false);
		AudioManager.Instance.AudioPlay("NewspaperRip");
	}

	public virtual void GaspProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void GaspExited()
	{
		_gasping = false;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.keepAlive = _keepAliveBeforeGasp;
			instance.damageHitpointsFloor = _damageHitpointsFloorBeforeGasp;
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Paper" && !_angry && !_gasping && !die && !isDestroy && GodotObject.IsInstanceValid(instance) && !instance.die && instance.hitpoints > 0.0)
		{
			SendStateEvent("ToGasp");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Gasp" && _gasping)
		{
			StateHandle gaspStateHandle = _gaspStateHandle;
			if (gaspStateHandle != null && gaspStateHandle.IsActive && !die && !isDestroy && GodotObject.IsInstanceValid(instance) && !instance.die)
			{
				AudioManager.Instance.AudioPlay("NewspaperRarrgh");
				timeScaleInit = 7.5;
				angry = true;
				ApplyAngryHitpointBonus();
				Walk();
			}
		}
	}

	private void ApplyAngryPresentation()
	{
		if (_angry)
		{
			base.walkAnimeClip = "WalkNoPaper";
			base.swimAnimeClip = "WalkNoPaper";
			base.attackAnimeClip = "SmashNoPaper";
			base.attackWaterAnimeClip = "SmashNoPaper";
			base.dieAnimeClip = "DeathNoPaper";
		}
	}

	private void ApplyAngryHitpointBonus()
	{
		if (!_angryBonusApplied && GodotObject.IsInstanceValid(instance) && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			_angryBonusApplied = true;
			instance.hitpointsSave += 3000.0;
			instance.hitpoints += 3000.0;
			showHealthComponent?.MarkDirty();
			instance.RefreshDamagePoint();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["angry"] = _angry;
		dictionary["angryBonusApplied"] = _angryBonusApplied;
		dictionary["gasping"] = _gasping;
		dictionary["keepAliveBeforeGasp"] = _keepAliveBeforeGasp;
		dictionary["damageHitpointsFloorBeforeGasp"] = _damageHitpointsFloorBeforeGasp;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		angry = data.GetValueOrDefault("angry", false).AsBool();
		_angryBonusApplied = data.GetValueOrDefault("angryBonusApplied", _angry).AsBool();
		_gasping = data.GetValueOrDefault("gasping", !_angry && instance.keepAlive).AsBool();
		_keepAliveBeforeGasp = data.GetValueOrDefault("keepAliveBeforeGasp", false).AsBool();
		_damageHitpointsFloorBeforeGasp = data.GetValueOrDefault("damageHitpointsFloorBeforeGasp", 0.0).AsDouble();
		if (_gasping)
		{
			instance.keepAlive = true;
			instance.damageHitpointsFloor = Math.Max(1.0, _damageHitpointsFloorBeforeGasp);
		}
	}
}
