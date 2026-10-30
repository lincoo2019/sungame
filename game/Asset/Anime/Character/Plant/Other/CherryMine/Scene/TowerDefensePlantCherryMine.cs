using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCherryMine : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private PotatoComponent _potatoComponent;

	private ExplodeComponent _explodeComponent;

	private StateHandle _fireState;

	private bool _roleStateSignalsConnected;

	private double _readyTime = 15.0;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventlist = new Array<TowerDefenseCharacterEventBase>();

	public bool over;

	[Export(PropertyHint.None, "")]
	public double readyTime
	{
		get
		{
			return _readyTime;
		}
		set
		{
			_readyTime = value;
			if (IsNodeReady())
			{
				PotatoComponent potatoComponent = _potatoComponent;
				if (potatoComponent != null && !potatoComponent.IsReleased)
				{
					_potatoComponent.readyTime = (float)_readyTime;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.readyTime = (float)readyTime;
				_potatoComponent.autoExplodeOnCharge = false;
				_potatoComponent.smashExplodeHandler = SmashExplode;
			}
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			_fireState = base.StateMachine?.GetStateById("plant.cherry_mine.fire");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
		DisconnectRoleStateSignals();
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (!_roleStateSignalsConnected)
		{
			StateHandle fireState = _fireState;
			if (fireState != null && fireState.IsValid)
			{
				_fireState.Entered += FireEntered;
				_fireState.Exited += FireExited;
				_fireState.PhysicsProcessing += FireProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_fireState.Entered -= FireEntered;
			_fireState.Exited -= FireExited;
			_fireState.PhysicsProcessing -= FireProcessing;
			_fireState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && _potatoComponent.isCharge && base.sprite.clip != "Fire" && _attackComponent.CanAttack())
		{
			SendStateEvent("ToFire");
		}
	}

	public void FireEntered()
	{
		instance.invincible = true;
		base.sprite.SetAnimation("Fire", loop: false);
		_potatoComponent.SetAlive(alive: false);
		TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(1.5f, 1.5f), eventlist, new Array<TowerDefenseCharacter>(), base.camp, -1);
		CompleteFireAsync();
	}

	private async Task CompleteFireAsync()
	{
		if (await WaitForStateDelayAsync(_fireState, 0.75))
		{
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.Explode();
			}
		}
	}

	public void FireProcessing(double _delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void FireExited()
	{
	}

	private bool SmashExplode()
	{
		if (!over)
		{
			StateHandle fireState = _fireState;
			if (fireState != null && fireState.IsValid)
			{
				if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.clip == "Fire")
				{
					return true;
				}
				SendStateEvent("ToFire");
				return true;
			}
		}
		return false;
	}

	public void ReadyRise()
	{
		_potatoComponent.ReadyRise();
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Fire" && !over)
		{
			over = true;
			Destroy();
		}
	}

	public void Explode()
	{
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "readyTime", readyTime },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		readyTime = (double)data.GetValueOrDefault("readyTime", 15.0);
		over = (bool)data.GetValueOrDefault("over", false);
	}
}
