using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieDancer : TowerDefenseZombie, IDancer
{
	private StateHandle _danceStateHandle;

	private bool _stateSignalsConnected;

	public int walkTime = 2;

	public int danceTime = 3;

	public TowerDefenseCharacter jackson;

	public string _pendingJacksonName = "";

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_danceStateHandle = base.StateMachine?.GetStateById("zombie.dancer.dance");
			StateHandle danceStateHandle = _danceStateHandle;
			if (danceStateHandle != null && danceStateHandle.IsValid)
			{
				_danceStateHandle.Entered += DanceEntered;
				_danceStateHandle.Exited += DanceExited;
				_danceStateHandle.PhysicsProcessing += DanceProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_danceStateHandle != null)
			{
				_danceStateHandle.Entered -= DanceEntered;
				_danceStateHandle.Exited -= DanceExited;
				_danceStateHandle.PhysicsProcessing -= DanceProcessing;
			}
			_danceStateHandle = null;
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
			base.sprite.SetFliter("_ground", open: false);
			ConnectStateSignals();
		}
	}

	public TowerDefenseCharacter GetJackson()
	{
		return jackson;
	}

	public void SetJackson(TowerDefenseCharacter value)
	{
		jackson = value;
	}

	public virtual void DanceEntered()
	{
		danceTime = 3;
		base.sprite.Scale = new Vector2(-1f, base.sprite.Scale.Y);
		base.sprite.SetAnimation("ArmRise", loop: true, 0.2);
	}

	public virtual void DanceProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (!base.sprite.pause && attackComponent.CanAttack())
		{
			Attack();
		}
	}

	public virtual void DanceExited()
	{
		base.sprite.Scale = new Vector2(1f, base.sprite.Scale.Y);
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		base.sprite.Scale = new Vector2(1f, base.sprite.Scale.Y);
		walkTime = 2;
	}

	public override void WalkProcessing(double delta)
	{
		if (GodotObject.IsInstanceValid(jackson) && jackson is TowerDefenseZombie towerDefenseZombie)
		{
			groundMoveComponent.SetAlive(towerDefenseZombie.groundMoveComponent?.Alive ?? false);
		}
		else
		{
			groundMoveComponent.SetAlive(true);
		}
		base.WalkProcessing(delta);
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if ((clip == "Walk" || clip == "ArmRise") && clip != base.sprite.clip)
		{
			return;
		}
		if (!(clip == "Walk"))
		{
			if (!(clip == "ArmRise"))
			{
				return;
			}
			base.sprite.Scale = new Vector2(0f - base.sprite.Scale.X, base.sprite.Scale.Y);
			danceTime--;
			if (!die && !nearDie)
			{
				if (danceTime > 0)
				{
					return;
				}
				if (GodotObject.IsInstanceValid(jackson))
				{
					if (!GodotObject.IsInstanceValid(jackson) || !(jackson is TowerDefenseZombie towerDefenseZombie))
					{
						return;
					}
					GroundMoveComponent obj = towerDefenseZombie.groundMoveComponent;
					if (obj != null && obj.Alive)
					{
						return;
					}
				}
				Walk();
			}
			else
			{
				OutJackson();
				Die();
			}
		}
		else
		{
			if (TowerDefenseManager.CurrentControl != null && !TowerDefenseManager.CurrentControl.isGameRunning)
			{
				return;
			}
			walkTime--;
			if (!die && !nearDie)
			{
				if (walkTime > 0)
				{
					return;
				}
				if (GodotObject.IsInstanceValid(jackson))
				{
					if (!GodotObject.IsInstanceValid(jackson) || !(jackson is TowerDefenseZombie towerDefenseZombie2))
					{
						return;
					}
					GroundMoveComponent obj2 = towerDefenseZombie2.groundMoveComponent;
					if (obj2 != null && obj2.Alive)
					{
						return;
					}
				}
				SendStateEvent("ToDance");
			}
			else
			{
				OutJackson();
				Die();
			}
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		OutJackson();
	}

	public void OutJackson()
	{
		if (GodotObject.IsInstanceValid(this.jackson))
		{
			if (this.jackson is TowerDefenseZombie towerDefenseZombie && !towerDefenseZombie.instance.hypnoses && this.jackson is IJackson jackson)
			{
				jackson.RemoveDancer(this);
			}
			this.jackson = null;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary();
		if (GodotObject.IsInstanceValid(jackson))
		{
			dictionary["jacksonNodeName"] = jackson.Name;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		if (data.ContainsKey("jacksonNodeName"))
		{
			_pendingJacksonName = (string)data["jacksonNodeName"];
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!(_pendingJacksonName != ""))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			Node nodeOrNull = node2D.GetNodeOrNull(_pendingJacksonName);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				jackson = (TowerDefenseCharacter)nodeOrNull;
			}
		}
		_pendingJacksonName = "";
	}
}
