using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMagnetMine : TowerDefensePlant
{
	private AttackComponent attackComponent;

	private MagnetComponent magnetComponent;

	private PotatoComponent potatoComponent;

	private bool _isProcessing;

	private StateHandle _fireState;

	private bool _roleStateSignalsConnected;

	private double _readyTime = 5.0;

	public List<TowerDefenseCharacter> drawCharacter = new List<TowerDefenseCharacter>();

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
			if (!IsNodeReady())
			{
				return;
			}
			PotatoComponent obj = potatoComponent;
			if (obj != null && !obj.IsReleased)
			{
				PotatoComponent obj2 = potatoComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					potatoComponent.readyTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			if (magnetComponent == null)
			{
				GD.PushError("Magnet Mine is missing its Magnet resource runtime.");
			}
			potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			PotatoComponent obj = potatoComponent;
			if (obj != null && !obj.IsReleased)
			{
				potatoComponent.readyTime = (float)readyTime;
			}
			_fireState = base.StateMachine?.GetStateById("plant.magnet_mine.fire");
			ConnectRoleStateSignals();
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
		if (!Engine.IsEditorHint() && potatoComponent.isCharge && base.sprite.clip != "Fire" && !_isProcessing)
		{
			_isProcessing = true;
			ProcessDrawAsync();
		}
	}

	private async void ProcessDrawAsync()
	{
		try
		{
			drawCharacter = await magnetComponent.GetCanArmorDrawCharacterList();
			if (GodotObject.IsInstanceValid(this) && drawCharacter.Count > 0)
			{
				SendStateEvent("ToFire");
			}
		}
		finally
		{
			_isProcessing = false;
		}
	}

	public void FireEntered()
	{
		instance.invincible = true;
		base.sprite.SetAnimation("Fire", loop: false);
		potatoComponent.SetAlive(alive: false);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in drawCharacter)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.gridPos = new Vector2I(item.gridPos.X, base.gridPos.Y);
				Tween tween = item.CreateTween();
				tween.SetParallel();
				tween.SetEase(Tween.EaseType.Out);
				tween.SetTrans(Tween.TransitionType.Quart);
				Vector2 logicalGlobalPosition2 = item.GetLogicalGlobalPosition();
				tween.TweenMethod(Callable.From<Vector2>(item.SetLogicalGlobalPosition), logicalGlobalPosition2, logicalGlobalPosition, 0.5);
				ShadowComponent obj = item.shadowComponent;
				if (obj != null && !obj.IsReleased)
				{
					item.shadowComponent.TweenSaveShadowPositionY(tween, item.shadowComponent.saveShadowPosition.Y + logicalGlobalPosition.Y - logicalGlobalPosition2.Y, 0.5);
				}
			}
		}
	}

	public void FireProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void FireExited()
	{
	}

	public void ReadyRise()
	{
		potatoComponent.ReadyRise();
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Fire" && !over)
		{
			over = true;
			potatoComponent.Explode();
			Destroy();
		}
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
		readyTime = data.GetValueOrDefault("readyTime", 5.0).AsDouble();
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
