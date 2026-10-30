using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombiePaperYeti : TowerDefenseZombie
{
	private const int GaspHeadFollowSlotId = 18;

	private const double FleeTimeScale = 4.0;

	private const string FleeWalkClip = "AngryWalk";

	private const string FleeAttackClip = "AngryEat";

	private int _headFollowSlotIdBeforeGasp = -1;

	private StateHandle _gaspStateHandle;

	private bool _stateSignalsConnected;

	private bool _fleeing;

	private bool _respawned;

	private const double InitialTimeScale = 0.6;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_gaspStateHandle = base.StateMachine?.GetStateById("zombie.paper_yeti.gasp");
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
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			timeScaleInit = 0.6;
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
			StartFleeing();
		}
	}

	private void StartFleeing()
	{
		_fleeing = true;
		timeScaleInit = 4.0;
		base.walkAnimeClip = "AngryWalk";
		base.swimAnimeClip = "AngryWalk";
		base.attackAnimeClip = "AngryEat";
		base.attackWaterAnimeClip = "AngryEat";
		RefreshFleeDirection();
		Walk();
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		if (_fleeing)
		{
			RefreshFleeDirection();
		}
	}

	private void RefreshFleeDirection()
	{
		float num = (instance.hypnoses ? 1f : (-1f));
		base.Scale = new Vector2(num * Mathf.Abs(base.Scale.X), base.Scale.Y);
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.NotifyAncestorTransformChangedForRender();
		}
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.RefreshDirectionCache();
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (_fleeing)
		{
			SwimComponent swimComponent = base.swimComponent;
			if (swimComponent != null && !swimComponent.IsReleased)
			{
				swimComponent.WalkProcessing((float)delta);
			}
			base.sprite.timeScale = timeScale;
			if (HasEscapedScreen())
			{
				RespawnNewspaperYeti();
				Destroy();
			}
		}
		else
		{
			base.WalkProcessing(delta);
		}
	}

	public override async void DestroySet()
	{
		if (_fleeing && !_respawned && HasEscapedScreen())
		{
			RespawnNewspaperYeti();
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private bool HasEscapedScreen()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return false;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (!instance.hypnoses)
		{
			return (double)logicalGlobalPosition.X > TowerDefenseManager.Instance.GetMapGroundRight() + 150.0;
		}
		return (double)logicalGlobalPosition.X < TowerDefenseManager.Instance.GetMapGroundLeft() - 150.0;
	}

	private void RespawnNewspaperYeti()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		_respawned = true;
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombiePaperYeti");
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(transformPoint))
		{
			return;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		int num = base.gridPos.Y;
		if (mapGridNum.Y > 1)
		{
			int num2 = GD.RandRange(1, mapGridNum.Y);
			if (num2 == base.gridPos.Y)
			{
				num2 = ((num2 >= mapGridNum.Y) ? (mapGridNum.Y - 1) : (num2 + 1));
			}
			num = Mathf.Clamp(num2, 1, mapGridNum.Y);
		}
		bool hypnoses = instance.hypnoses;
		float x = (float)(hypnoses ? (TowerDefenseManager.Instance.GetMapGroundLeft() - 100.0) : (TowerDefenseManager.Instance.GetMapGroundRight() + 100.0));
		float y = (float)TowerDefenseManager.GetMapLineY(num);
		Vector2 pos = new Vector2(x, y);
		Vector2I vector2I = new Vector2I((!hypnoses) ? (mapGridNum.X + 1) : 0, num);
		TowerDefenseCharacter zombie = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, pos, vector2I) : packetConfig.Create(pos, vector2I));
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", zombie);
		double hitpointScale = instance.hitpointScale;
		double remainingHitpoints = instance.hitpoints;
		Vector2 scale = transformPoint.Scale;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				if (GodotObject.IsInstanceValid(zombie.instance))
				{
					zombie.instance.hitpointScale = hitpointScale;
					zombie.instance.hitpoints = Math.Min(remainingHitpoints, zombie.instance.hitpointsSave);
					zombie.instance.RefreshDamagePoint();
					zombie.showHealthComponent?.MarkDirty();
				}
				if (GodotObject.IsInstanceValid(zombie.transformPoint))
				{
					zombie.transformPoint.Scale = scale;
				}
			}
		}).CallDeferred();
		zombie.SetDeferred("invisible", base.invisible);
		if (hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.Hypnoses();
				}
			}).CallDeferred();
		}
		zombie.CallDeferred("WalkReady");
		Dictionary spawnState = new Dictionary
		{
			["hypnoses"] = hypnoses,
			["spawn_invisible"] = base.invisible,
			["walk_ready_after_spawn"] = true
		};
		TowerDefenseManager.PublishSpawnedCharacter("ZombiePaperYeti", zombie, useCreate: true, 0.0, walkAfterSpawn: false, "", spawnState);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fleeing", _fleeing },
			{ "respawned", _respawned }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_fleeing = data.GetValueOrDefault("fleeing", false).AsBool();
		_respawned = data.GetValueOrDefault("respawned", false).AsBool();
	}
}
