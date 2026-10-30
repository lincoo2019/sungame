using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieLadder : TowerDefenseZombie
{
	private StateHandle _ladderStateHandle;

	private bool _stateSignalsConnected;

	private bool _ladderOver;

	public bool ladderOver
	{
		get
		{
			return _ladderOver;
		}
		set
		{
			_ladderOver = value;
			if (_ladderOver)
			{
				base.swimAnimeClip = "Walk";
				base.walkAnimeClip = "Walk";
				base.attackAnimeClip = "Eat";
			}
			else
			{
				base.swimAnimeClip = "LadderWalk";
				base.walkAnimeClip = "LadderWalk";
				base.attackAnimeClip = "LadderEat";
			}
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
			_ladderStateHandle = base.StateMachine?.GetStateById("zombie.ladder.ladder");
			StateHandle ladderStateHandle = _ladderStateHandle;
			if (ladderStateHandle != null && ladderStateHandle.IsValid)
			{
				_ladderStateHandle.Entered += LadderEntered;
				_ladderStateHandle.Exited += LadderExited;
				_ladderStateHandle.PhysicsProcessing += LadderProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_ladderStateHandle != null)
			{
				_ladderStateHandle.Entered -= LadderEntered;
				_ladderStateHandle.Exited -= LadderExited;
				_ladderStateHandle.PhysicsProcessing -= LadderProcessing;
			}
			_ladderStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConnectStateSignals();
		}
	}

	public virtual void LadderEntered()
	{
		base.sprite.SetAnimation("Ladder", loop: false, 0.2);
	}

	public virtual void LadderProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * base.walkSpeedScale * 1.0;
	}

	public virtual void LadderExited()
	{
	}

	public override void AttackProcessing(double delta)
	{
		if (!ladderOver && !base.sprite.pause && attackComponent.CanAttack() && GodotObject.IsInstanceValid(attackComponent.target))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(attackComponent.target.gridPos);
			if (GodotObject.IsInstanceValid(mapCell) && mapCell.HasWallnut())
			{
				SendStateEvent("ToLadder");
				return;
			}
		}
		base.AttackProcessing(delta);
		if (!ladderOver)
		{
			base.sprite.timeScale = timeScale * 4.0;
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (!ladderOver && !base.sprite.pause && attackComponent.CanAttack() && GodotObject.IsInstanceValid(attackComponent.target))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(attackComponent.target.gridPos);
			if (GodotObject.IsInstanceValid(mapCell) && mapCell.HasWallnut())
			{
				SendStateEvent("ToLadder");
				return;
			}
		}
		base.WalkProcessing(delta);
		if (!ladderOver)
		{
			base.sprite.timeScale = timeScale * 2.0;
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Ladder")
		{
			ladderOver = true;
			Walk();
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "place_ladder")
		{
			LadderPlace();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Ladder")
		{
			ladderOver = true;
			Walk();
		}
	}

	public void LadderPlace()
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemLadder");
		if (GodotObject.IsInstanceValid(attackComponent.target))
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(attackComponent.target.gridPos);
			if (GodotObject.IsInstanceValid(mapCell) && mapCell.HasWallnut() && mapCell.CanPacketPlant(packetConfig))
			{
				packetConfig.Plant(attackComponent.target.gridPos);
				ladderOver = true;
				instance.ArmorDelete("Ladder", createDamagePart: false);
				attackComponent.target = null;
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "ladderOver", ladderOver } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		ladderOver = data.GetValueOrDefault("ladderOver", false).AsBool();
	}
}
