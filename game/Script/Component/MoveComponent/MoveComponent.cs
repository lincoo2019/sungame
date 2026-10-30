using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MoveComponent : ComponentBase
{
	public delegate void MovementActivityChangedEventHandler(bool active);

	private Vector2 _velocity = Vector2.Zero;

	private double _gravity;

	private bool _movementActive;

	[Export(PropertyHint.None, "")]
	public double moveScale = 1.0;

	public CanvasItem parent;

	protected override bool UseSharedPhysicsBatch => true;

	[Export(PropertyHint.None, "")]
	public Vector2 velocity
	{
		get
		{
			return _velocity;
		}
		set
		{
			if (!(_velocity == value))
			{
				_velocity = value;
				RefreshMovementProcessing();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double gravity
	{
		get
		{
			return _gravity;
		}
		set
		{
			if (_gravity != value)
			{
				_gravity = value;
				RefreshMovementProcessing();
			}
		}
	}

	public bool HasActiveMovement
	{
		get
		{
			if (!(_velocity != Vector2.Zero))
			{
				return _gravity != 0.0;
			}
			return true;
		}
	}

	public event MovementActivityChangedEventHandler MovementActivityChanged;

	public override string _GetName()
	{
		return "MoveComponent";
	}

	public override void _Ready()
	{
		parent = GetParent() as CanvasItem;
		RefreshMovementProcessing();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!base.alive || parent == null)
		{
			return;
		}
		if (!HasActiveMovement)
		{
			SetSharedPhysicsBatchWorkEnabled(enabled: false);
			return;
		}
		if (_gravity != 0.0)
		{
			_velocity = new Vector2(_velocity.X, _velocity.Y + (float)(_gravity * delta * moveScale));
		}
		Vector2 globalPosition = GetGlobalPosition(parent);
		SetGlobalPosition(parent, globalPosition + _velocity * (float)(delta * moveScale));
	}

	private void RefreshMovementProcessing()
	{
		bool hasActiveMovement = HasActiveMovement;
		SetSharedPhysicsBatchWorkEnabled(hasActiveMovement);
		if (_movementActive != hasActiveMovement)
		{
			bool num = _movementActive && !hasActiveMovement;
			_movementActive = hasActiveMovement;
			if (num)
			{
				ClearStoppedMovementRenderInterpolation();
			}
			this.MovementActivityChanged?.Invoke(hasActiveMovement);
		}
	}

	private void ClearStoppedMovementRenderInterpolation()
	{
		Node node = parent;
		if (node != null)
		{
			ClearStoppedMovementRenderInterpolation(node);
		}
	}

	private static void ClearStoppedMovementRenderInterpolation(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.ClearRetainedRootMotionForStoppedMovement();
		}
		foreach (Node child in node.GetChildren())
		{
			ClearStoppedMovementRenderInterpolation(child);
		}
	}

	private static Vector2 GetGlobalPosition(CanvasItem node)
	{
		if (node is Node2D node2D)
		{
			return node2D.GlobalPosition;
		}
		if (node is Control control)
		{
			return control.GlobalPosition;
		}
		return Vector2.Zero;
	}

	private static void SetGlobalPosition(CanvasItem node, Vector2 pos)
	{
		if (node is Node2D node2D)
		{
			node2D.GlobalPosition = pos;
		}
		else if (node is Control control)
		{
			control.GlobalPosition = pos;
		}
	}

	public void SetVelocity(Vector2 _velocity)
	{
		velocity = _velocity;
	}

	public void SetGravity(double _gravity)
	{
		gravity = _gravity;
	}

	public void MoveClear()
	{
		_velocity = Vector2.Zero;
		_gravity = 0.0;
		RefreshMovementProcessing();
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "velocityX", _velocity.X },
			{ "velocityY", _velocity.Y },
			{ "gravity", _gravity },
			{ "moveScale", moveScale }
		};
	}

	public override void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		_velocity = new Vector2((float)_data.GetValueOrDefault("velocityX", 0.0).AsDouble(), (float)_data.GetValueOrDefault("velocityY", 0.0).AsDouble());
		_gravity = _data.GetValueOrDefault("gravity", 0.0).AsDouble();
		moveScale = _data.GetValueOrDefault("moveScale", 1.0).AsDouble();
		RefreshMovementProcessing();
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary
		{
			{ "velocityX", _velocity.X },
			{ "velocityY", _velocity.Y },
			{ "gravity", _gravity },
			{ "moveScale", moveScale }
		};
	}

	public override void SyncDeserialize(Dictionary data)
	{
		_velocity = new Vector2((float)data.GetValueOrDefault("velocityX", 0.0).AsDouble(), (float)data.GetValueOrDefault("velocityY", 0.0).AsDouble());
		_gravity = data.GetValueOrDefault("gravity", 0.0).AsDouble();
		moveScale = data.GetValueOrDefault("moveScale", 1.0).AsDouble();
		RefreshMovementProcessing();
	}
}
