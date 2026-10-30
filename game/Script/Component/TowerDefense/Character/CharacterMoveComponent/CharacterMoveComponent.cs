using Godot;
using Godot.Collections;

public sealed class CharacterMoveComponent : CharacterComponentRuntime
{
	private Vector2 _velocity = Vector2.Zero;

	private double _gravity;

	private double _moveScale = 1.0;

	private bool _configured;

	private readonly Dictionary _syncPayload = new Dictionary();

	public TowerDefenseCharacter parent;

	protected override bool AllowPhysicsOutsideComponentBattlefield => true;

	private CharacterMoveComponentDefinition Definition => base.ComponentDefinition as CharacterMoveComponentDefinition;

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
				RefreshPhysicsProcessEligibility();
			}
		}
	}

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
				RefreshPhysicsProcessEligibility();
			}
		}
	}

	public double moveScale
	{
		get
		{
			return _moveScale;
		}
		set
		{
			if (_moveScale != value)
			{
				_moveScale = value;
				RefreshPhysicsProcessEligibility();
			}
		}
	}

	private bool HasMotion
	{
		get
		{
			if (_moveScale != 0.0)
			{
				if (!(_velocity != Vector2.Zero))
				{
					return _gravity != 0.0;
				}
				return true;
			}
			return false;
		}
	}

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (GodotObject.IsInstanceValid(parent))
			{
				return HasMotion;
			}
			return false;
		}
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			CharacterMoveComponentDefinition definition = Definition;
			_velocity = definition?.velocity ?? Vector2.Zero;
			_gravity = definition?.gravity ?? 0.0;
			_moveScale = definition?.moveScale ?? 1.0;
			_configured = true;
		}
		RefreshPhysicsProcessEligibility();
	}

	protected override void OnActivated()
	{
		RefreshPhysicsProcessEligibility();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		parent = null;
	}

	protected override void OnReleased()
	{
		parent = null;
		_syncPayload.Clear();
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (!base.Alive || !GodotObject.IsInstanceValid(parent) || !HasMotion)
		{
			RefreshPhysicsProcessEligibility();
			return;
		}
		if (gravity != 0.0)
		{
			_velocity = new Vector2(_velocity.X, _velocity.Y + (float)(gravity * delta * moveScale));
		}
		if (velocity == Vector2.Zero || moveScale == 0.0)
		{
			RefreshPhysicsProcessEligibility();
			return;
		}
		Vector2 globalPositionForPhysicsFrame = parent.GetGlobalPositionForPhysicsFrame(physicsFrame);
		parent.SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame + velocity * (float)(delta * moveScale), physicsFrame);
	}

	public void SetVelocity(Vector2 value)
	{
		velocity = value;
	}

	public void SetGravity(double value)
	{
		gravity = value;
	}

	public void MoveClear()
	{
		_velocity = Vector2.Zero;
		_gravity = 0.0;
		RefreshPhysicsProcessEligibility();
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "velocityX", velocity.X },
			{ "velocityY", velocity.Y },
			{ "gravity", gravity },
			{ "moveScale", moveScale }
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		ApplySerializedState(data);
	}

	public override Dictionary SyncSerialize()
	{
		_syncPayload["velocityX"] = velocity.X;
		_syncPayload["velocityY"] = velocity.Y;
		_syncPayload["gravity"] = gravity;
		_syncPayload["moveScale"] = moveScale;
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		ApplySerializedState(data);
	}

	private void ApplySerializedState(Dictionary data)
	{
		if (data != null)
		{
			velocity = new Vector2((float)data.GetValueOrDefault("velocityX", velocity.X).AsDouble(), (float)data.GetValueOrDefault("velocityY", velocity.Y).AsDouble());
			gravity = data.GetValueOrDefault("gravity", gravity).AsDouble();
			moveScale = data.GetValueOrDefault("moveScale", moveScale).AsDouble();
		}
	}
}
