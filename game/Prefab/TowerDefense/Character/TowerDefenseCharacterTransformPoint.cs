using Godot;

public sealed partial class TowerDefenseCharacterTransformPoint : Marker2D
{
	private Vector2 _cachedScale = Vector2.One;

	private ulong _scaleRevision = 1uL;

	private Transform2D _cachedLocalTransform = Transform2D.Identity;

	private ulong _localTransformRevision = 1uL;

	private TowerDefenseCharacter _owner;

	internal Vector2 CachedScale => _cachedScale;

	internal ulong ScaleRevision => _scaleRevision;

	internal Transform2D CachedLocalTransform => _cachedLocalTransform;

	internal ulong LocalTransformRevision => _localTransformRevision;

	public override void _EnterTree()
	{
		base._EnterTree();
		_owner = GetParent()?.GetParent() as TowerDefenseCharacter;
		_cachedLocalTransform = base.Transform;
		_cachedScale = _cachedLocalTransform.Scale;
		AdvanceScaleRevision();
		AdvanceLocalTransformRevision();
		SetNotifyLocalTransform(enable: true);
	}

	public override void _ExitTree()
	{
		SetNotifyLocalTransform(enable: false);
		_owner = null;
		base._ExitTree();
	}

	public override void _Notification(int what)
	{
		if ((long)what != 35)
		{
			return;
		}
		Transform2D transform = base.Transform;
		if (!transform.IsEqualApprox(_cachedLocalTransform))
		{
			Vector2 scale = transform.Scale;
			_cachedLocalTransform = transform;
			AdvanceLocalTransformRevision();
			if (GodotObject.IsInstanceValid(_owner?.sprite))
			{
				_owner.sprite.NotifyAncestorTransformChangedForRender();
			}
			if (!scale.IsEqualApprox(_cachedScale))
			{
				_cachedScale = scale;
				AdvanceScaleRevision();
			}
		}
	}

	private void AdvanceScaleRevision()
	{
		_scaleRevision++;
		if (_scaleRevision == 0L)
		{
			_scaleRevision = 1uL;
		}
	}

	private void AdvanceLocalTransformRevision()
	{
		_localTransformRevision++;
		if (_localTransformRevision == 0L)
		{
			_localTransformRevision = 1uL;
		}
	}
}
