using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantDiamondseed : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private StateHandle _prepareState;

	private bool _roleStateSignalsConnected;

	public bool over;

	public double timer;

	public double time = 30.0;

	private double _fireInterval = 5.0;

	private int _fireNum = 1;

	private string _projectileName = "CoinDiamond";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (!Engine.IsEditorHint())
			{
				_prepareState = base.StateMachine?.GetStateById("plant.diamondseed.prepare");
				ConnectRoleStateSignals();
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (!_roleStateSignalsConnected)
		{
			StateHandle prepareState = _prepareState;
			if (prepareState != null && prepareState.IsValid)
			{
				_prepareState.Entered += PrepareEntered;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_prepareState.Entered -= PrepareEntered;
			_prepareState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public void PrepareEntered()
	{
		_fireComponent.alive = false;
		base.sprite.SetAnimation("Prepare", loop: true, 0.2);
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.isGameRunning && !over && !base.sprite.pause)
		{
			if (timer < time)
			{
				timer += delta * timeScale;
				return;
			}
			over = true;
			base.sprite.SetAnimation("Grow", loop: true, 0.2);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Grow")
		{
			Idle();
		}
	}

	public override void Idle()
	{
		if (over)
		{
			SendStateEvent("ToIdle");
		}
		else
		{
			SendStateEvent("ToPrepare");
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		_fireComponent.alive = true;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["over"] = over,
			["timer"] = timer,
			["time"] = time,
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
		timer = (data.ContainsKey("timer") ? data["timer"].AsDouble() : 0.0);
		time = (data.ContainsKey("time") ? data["time"].AsDouble() : 30.0);
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "CoinDiamond");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 5.0);
	}
}
