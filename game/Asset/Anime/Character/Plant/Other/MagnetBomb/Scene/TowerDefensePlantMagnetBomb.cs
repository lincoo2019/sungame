using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMagnetBomb : TowerDefensePlant
{
	private MagnetComponent _magnetComponent;

	private ExplodeComponent _explodeComponent;

	private StateHandle _fireState;

	private bool _roleStateSignalsConnected;

	private readonly List<TowerDefenseCharacter> _scanTargets = new List<TowerDefenseCharacter>();

	public Array<TowerDefenseCharacter> drawCharacter = new Array<TowerDefenseCharacter>();

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			MagnetComponent magnetComponent = _magnetComponent;
			if (magnetComponent != null && !magnetComponent.IsReleased)
			{
				_magnetComponent.SetAlive(false);
			}
			else
			{
				GD.PushError("Magnet Bomb is missing its Magnet resource runtime.");
			}
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.SetAlive(false);
			}
			else
			{
				GD.PushError("Magnet Bomb is missing its Explode resource runtime.");
			}
			_fireState = base.StateMachine?.GetStateById("plant.magnet_bomb.fire");
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
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_fireState.Entered -= FireEntered;
			_fireState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !TowerDefenseManager.Instance.IsGameRunning() || !inGame || !base.componentAlive || componentRunning || over)
		{
			return;
		}
		_magnetComponent.FillCanArmorDrawCharacterList(_scanTargets);
		drawCharacter.Clear();
		foreach (TowerDefenseCharacter scanTarget in _scanTargets)
		{
			drawCharacter.Add(scanTarget);
		}
		if (drawCharacter.Count > 0)
		{
			SendStateEvent("ToFire");
			return;
		}
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.SetAlive(true);
		}
	}

	public void FireEntered()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.SetAlive(true);
		}
		instance.invincible = true;
		base.sprite.SetAnimation("Explode", loop: false);
		Vector2I[] array = new Vector2I[9]
		{
			new Vector2I(-1, -1),
			new Vector2I(0, -1),
			new Vector2I(1, -1),
			new Vector2I(-1, 0),
			new Vector2I(0, 0),
			new Vector2I(1, 0),
			new Vector2I(-1, 1),
			new Vector2I(0, 1),
			new Vector2I(1, 1)
		};
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		foreach (TowerDefenseCharacter item in drawCharacter)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				Vector2I vector2I = base.gridPos + array[(int)(GD.Randi() % array.Length)];
				vector2I = new Vector2I(Mathf.Clamp(vector2I.X, 1, mapGridNum.X), Mathf.Clamp(vector2I.Y, 1, mapGridNum.Y));
				Vector2 mapCellPosCenter = TowerDefenseManager.Instance.GetMapCellPosCenter(vector2I);
				item.gridPos = vector2I;
				Vector2 logicalGlobalPosition = item.GetLogicalGlobalPosition();
				Tween tween = item.CreateTween();
				tween.SetParallel();
				tween.SetEase(Tween.EaseType.Out);
				tween.SetTrans(Tween.TransitionType.Quart);
				tween.TweenMethod(Callable.From<Vector2>(item.SetLogicalGlobalPosition), logicalGlobalPosition, mapCellPosCenter, 0.5);
				ShadowComponent obj = item.shadowComponent;
				if (obj != null && !obj.IsReleased)
				{
					item.shadowComponent.TweenSaveShadowPositionY(tween, item.shadowComponent.saveShadowPosition.Y + mapCellPosCenter.Y - logicalGlobalPosition.Y, 0.5);
				}
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = (bool)data.GetValueOrDefault("over", false);
	}
}
